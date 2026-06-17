using Unity.Netcode;
using UnityEngine;

// Mrežna verzija RocketMovement-a koja sinkronizira eksploziju na svim klijentima
public class NetworkRocketMovement : NetworkBehaviour
{
    public GameObject explosion; // Prefab za BigExplosion
    public LayerMask whatIsEnemies;

    // Statistika
    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    // Šteta
    public int explosionDamage;
    public float explosionRange;
    public float explosionForce;
    
    [Header("Damage Settings")]
    public float maxDamage = 50f; // Maksimalni damage na najbližoj udaljenosti
    public float minDamage = 10f; // Minimalni damage na najdaljoj udaljenosti
    public float damageRadius = 15f; // Radijus u kojem se nanosi damage

    // Životni vijek
    public int maxCollisions;
    public float maxLifetime;
    public bool explodeOnTouch = true;

    private int collisions;
    private PhysicMaterial physics_mat;

    private bool hasExploded = false;
	
	public float explosionRadius = 50f; // Radijus eksplozije
	public float showRedRadius = 15f; // Radijus prikaza crvenog traga
    public float criticalRadius = 10f; // Radijus uništenja (povećan za lakše testiranje)
	private Shake shake;
	private NetworkShake networkShake;
	private NetworkMultiplayerShake networkMultiplayerShake;

	// Vlasnik rakete
	private ulong shooterOwnerId = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Postavi ownership rakete na vlasnika koji ju je ispalio
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            shooterOwnerId = NetworkObject.OwnerClientId;
        }
    }

    private void Start()
    {
        // Pokušaj pronaći shake komponente
        GameObject screenShakeObj = GameObject.FindGameObjectWithTag("ScreenShake");
        if (screenShakeObj != null)
        {
            shake = screenShakeObj.GetComponent<Shake>();
            networkShake = screenShakeObj.GetComponent<NetworkShake>();
            networkMultiplayerShake = screenShakeObj.GetComponent<NetworkMultiplayerShake>();
        }
        Setup();
    }

    private void Update(){
    }

    private void Explode()
    {
        if (hasExploded) return;
        
        // Provjeri je li objekt još spawnan
        if (NetworkObject != null && !NetworkObject.IsSpawned)
        {
            return; // Objekt je već despawnan, ne eksplodiraj
        }
        
        hasExploded = true;

        Vector3 explosionPosition = transform.position;

        // Pozovi ClientRpc da svi klijenti vide eksploziju
        ExplodeClientRpc(explosionPosition);
        
        // Server provjerava damage i ostalo
        if (IsServer)
        {
            // Provjeri neprijatelje unutar radijusa eksplozije
            Collider[] enemies = Physics.OverlapSphere(explosionPosition, explosionRange, whatIsEnemies);

            for (int i = 0; i < enemies.Length; i++)
            {
                // Provjeri ima li objekt oznaku "Enemy"
                if (enemies[i].CompareTag("Enemy"))
                {
                    // Uništi neprijateljski objekt
                    Destroy(enemies[i].gameObject);
                }
            }

            // Provjeri hit detection za NetworkPlayer i NetworkJet
            CheckHitDetection(explosionPosition);

            // Uništi raketu nakon kratkog kašnjenja
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                Invoke("Delay", 0.05f);
            }
        }
    }

    /// Provjeri hit detection i nanesi damage igračima i avionima
    private void CheckHitDetection(Vector3 explosionPosition)
    {
        if (!IsServer) return;

        // Osiguraj da je shooterOwnerId postavljen (ako nije već postavljen u OnNetworkSpawn)
        if (shooterOwnerId == 0 && NetworkObject != null && NetworkObject.IsSpawned)
        {
            shooterOwnerId = NetworkObject.OwnerClientId;
        }

        // Koristi Physics.OverlapSphere da pronađe sve objekte u radijusu eksplozije
        float checkRadius = Mathf.Max(damageRadius, criticalRadius * 2f);
        Collider[] collidersInRadius = Physics.OverlapSphere(explosionPosition, checkRadius);

        GameObject player = null;
        GameObject jet = null;

        // Pronađi NetworkPlayer i NetworkJet među colliderima
        foreach (Collider col in collidersInRadius)
        {
            if (col.CompareTag("Player"))
            {
                player = col.gameObject;
                if (player.name.Contains("NetworkPlayer")) break;
            }
            else if (col.CompareTag("Jet"))
            {
                jet = col.gameObject;
                if (jet.name.Contains("NetworkJet")) break;
            }
        }

        // Fallback: ako nisu pronađeni kroz collidere, koristi Find
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("NetworkPlayer(Clone)");
        }

        if (jet == null)
        {
            jet = GameObject.FindGameObjectWithTag("Jet");
            if (jet == null) jet = GameObject.Find("NetworkJet(Clone)");
        }

        // Provjeri NetworkPlayer i nanesi damage
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(explosionPosition, player.transform.position);
            
            // Ako je igrač u damage radijusu, nanesi damage
            if (distanceToPlayer <= damageRadius)
            {
                NetworkHealth playerHealth = player.GetComponent<NetworkHealth>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    // Izračunaj damage ovisno o udaljenosti
                    float damage = CalculateDamage(distanceToPlayer, damageRadius);
                    
                    // Nanesi damage s attackerId za praćenje last attacker-a
                    playerHealth.TakeDamage(damage, shooterOwnerId);
                    
                    // Ako je health <= 0, respawnaj igrača
                    if (playerHealth.IsDead)
                    {
                        NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
                        if (playerNetworkObject != null && playerNetworkObject.IsSpawned)
                        {
                            ulong ownerId = playerNetworkObject.OwnerClientId;
                            
                            // Ažuriraj statistike
                            UpdateStatsForKill(player, shooterOwnerId, ownerId);
                            
                            // Despawnaj igrača
                            if (playerNetworkObject.IsSpawned)
                            {
                                playerNetworkObject.Despawn();
                            }
                            Destroy(player);

                            // Respawnaj igrača na početnoj poziciji
                            NetworkSpawnManager spawnManager = FindObjectOfType<NetworkSpawnManager>();
                            if (spawnManager != null)
                            {
                                spawnManager.RespawnPlayer(ownerId);
                            }
                        }
                    }
                }
            }
        }

        // Provjeri NetworkJet i nanesi damage
        if (jet != null)
        {
            // Za avion koristimo collider bounds center jer je duguljast
            Vector3 jetPosition = jet.transform.position;
            Collider jetCollider = jet.GetComponent<Collider>();
            if (jetCollider != null)
            {
                jetPosition = jetCollider.bounds.center;
            }
            
            float distanceToJet = Vector3.Distance(explosionPosition, jetPosition);
            
            // Povećaj damage radius za avion jer je duguljast
            float jetDamageRadius = damageRadius * 1.5f;
            
            // Ako je avion u damage radijusu, nanesi damage
            if (distanceToJet <= jetDamageRadius)
            {
                NetworkHealth jetHealth = jet.GetComponent<NetworkHealth>();
                if (jetHealth != null && !jetHealth.IsDead)
                {
                    // Izračunaj damage ovisno o udaljenosti
                    float damage = CalculateDamage(distanceToJet, jetDamageRadius);
                    
                    // Nanesi damage s attackerId za praćenje last attacker-a
                    jetHealth.TakeDamage(damage, shooterOwnerId);
                    
                    // Ako je health <= 0, respawnaj avion
                    if (jetHealth.IsDead)
                    {
                        NetworkObject jetNetworkObject = jet.GetComponent<NetworkObject>();
                        if (jetNetworkObject != null && jetNetworkObject.IsSpawned)
                        {
                            ulong jetOwnerId = jetNetworkObject.OwnerClientId;
                            
                            // Ažuriraj statistike
                            UpdateStatsForKill(jet, shooterOwnerId, jetOwnerId);
                            
                            // Despawnaj avion
                            if (jetNetworkObject.IsSpawned)
                            {
                                jetNetworkObject.Despawn();
                            }
                            Destroy(jet);

                            // Respawnaj avion na početnoj poziciji
                            NetworkSpawnManager spawnManager = FindObjectOfType<NetworkSpawnManager>();
                            if (spawnManager != null)
                            {
                                spawnManager.RespawnJet(jetOwnerId);
                            }
                        }
                    }
                }
            }
        }
    }

    /// Izračunaj damage ovisno o udaljenosti od eksplozije
    private float CalculateDamage(float distance, float radius)
    {
        // Normaliziraj udaljenost
        float normalizedDistance = Mathf.Clamp01(distance / radius);
        
        // Linearno smanji damage s udaljenošću
        // Na udaljenosti 0 = maxDamage, na udaljenosti radius = minDamage
        float damage = Mathf.Lerp(maxDamage, minDamage, normalizedDistance);
        
        return damage;
    }


    [ClientRpc]
    private void ExplodeClientRpc(Vector3 explosionPosition)
    {
        // Svi klijenti spawnaju eksploziju
        if (explosion != null)
        {
            GameObject spawnedExplosion = Instantiate(explosion, explosionPosition, Quaternion.identity);

            // Pronađi AudioSource komponentu na instanciranoj eksploziji
            AudioSource explosionAudio = spawnedExplosion.GetComponent<AudioSource>();
            if (explosionAudio != null && explosionAudio.clip != null)
            {
                explosionAudio.Play(); // Reproduciraj zvuk eksplozije
            }

            Destroy(spawnedExplosion, 2f); // Uništi eksploziju nakon 2 sekunde
        }
		
		// Provjeri udaljenost od NetworkPlayer-a
		GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // Pokušaj pronaći NetworkPlayer
            player = GameObject.Find("NetworkPlayer(Clone)");
        }

        // Provjeri udaljenost od NetworkJet-a
        GameObject jet = GameObject.FindGameObjectWithTag("Jet");
        if (jet == null)
        {
            // Pokušaj pronaći NetworkJet
            jet = GameObject.Find("NetworkJet(Clone)");
        }

        bool playerInRadius = false;
        bool jetInRadius = false;

        // Provjeri NetworkPlayer
        if (player != null)
        {
		    float distanceToPlayer = Vector3.Distance(explosionPosition, player.transform.position);

		    if (distanceToPlayer <= explosionRadius)
            {
                playerInRadius = true;

			    if (distanceToPlayer <= showRedRadius) // Dodaj efekt crvenog traga ako je u radijusu
			        {
                        // Provjeri da li je ovaj igrač vlasnik NetworkPlayer-a
                        NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
                        if (playerNetworkObject != null && playerNetworkObject.IsOwner)
                        {
                            // Pronađi ScreenEffect vezan za PlayerCamera
                            ScreenEffect screenEffect = FindScreenEffectForCamera("PlayerCamera");
                            if (screenEffect != null)
                            {
                                screenEffect.ShowEffect(1f); // Prikaz efekta u trajanju 1 sekunde
                            }
                        }

                        if (distanceToPlayer <= criticalRadius)
                        {
                             // U multiplayer sceni ne učitavamo GameOver scenu
                             // SceneManager.LoadScene("GameOver");
                        }
			        }
		    }
	    }

        // Provjeri NetworkJet
        if (jet != null)
        {
            float distanceToJet = Vector3.Distance(explosionPosition, jet.transform.position);

            if (distanceToJet <= explosionRadius)
            {
                jetInRadius = true;

                // Dodaj crveni efekt i za NetworkJet ako je blizu
                // Povećan radijus za jet jer se rakete ispaljuju daleko od centra aviona
                float jetShowRedRadius = 25f;
                if (distanceToJet <= jetShowRedRadius)
                {
                    // Provjeri da li je ovaj igrač vlasnik NetworkJet-a
                    NetworkObject jetNetworkObject = jet.GetComponent<NetworkObject>();
                    if (jetNetworkObject != null && jetNetworkObject.IsOwner)
                    {
                        // Pronađi ScreenEffect vezan za JetCamera
                        ScreenEffect screenEffect = FindScreenEffectForCamera("JetCamera");
                        if (screenEffect != null)
                        {
                            screenEffect.ShowEffect(1f); // Prikaz efekta u trajanju 1 sekunde
                        }
                    }
                }
            }
        }

        // Shakeaj kamere ako su objekti u radijusu
        if (playerInRadius || jetInRadius)
        {
            // Koristi NetworkMultiplayerShake ako postoji
            if (networkMultiplayerShake != null)
            {
                if (playerInRadius && jetInRadius)
                {
                    // Shakeaj obje kamere ako su oba objekta u radijusu
                    networkMultiplayerShake.ShakeAllCameras();
                }
                else if (playerInRadius)
                {
                    // Shakeaj samo PlayerCamera
                    networkMultiplayerShake.ShakePlayerCamera();
                }
                else if (jetInRadius)
                {
                    // Shakeaj samo JetCamera
                    networkMultiplayerShake.ShakeJetCamera();
                }
            }
            else
            {
                // Fallback na stare shake komponente
                if (shake != null)
                    shake.CamShake();
                
                if (networkShake != null)
                    networkShake.CamShake();
            }
        }
    }

    private void Delay()
    {
        if (IsServer)
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ne broji sudare s drugim raketama
        if (collision.collider.CompareTag("Bullet")) return;

        // Provjeri je li objekt još spawnan
        if (NetworkObject == null || !NetworkObject.IsSpawned)
        {
            return; // Objekt je već despawnan, ne eksplodiraj
        }

        // Ako nije server, obavijesti server da eksplodira
        if (!IsServer)
        {
            RequestExplodeServerRpc(transform.position);
            return;
        }

        // Povećaj broj sudara
        collisions++;

        // Eksplodira pri svakom sudaru
        Explode();
    }

    /// ServerRpc koji klijent poziva kada detektira koliziju
    [ServerRpc(RequireOwnership = false)]
    private void RequestExplodeServerRpc(Vector3 explosionPosition)
    {
        // Postavi poziciju rakete na poziciju eksplozije (ako se razlikuje zbog lag kompenzacije)
        transform.position = explosionPosition;
        
        // Pozovi eksploziju na serveru
        Explode();
    }

    private void Setup()
    {
        // Stvori novi materijal
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;
    }

    /// Pronađi ScreenEffect vezan za određenu kameru
    private ScreenEffect FindScreenEffectForCamera(string cameraTag)
    {
        // Pronađi kameru po tagu
        GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);
        if (cameraObj != null)
        {
            Camera targetCamera = cameraObj.GetComponent<Camera>();
            if (targetCamera != null && targetCamera.enabled)
            {
                // Pronađi sve Canvas-e u sceni
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in allCanvases)
                {
                    // Ako je Canvas u "Screen Space - Overlay" modu (worldCamera je null),
                    // ScreenEffect će se prikazati na svim kamerama, pa ga možemo koristiti
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        ScreenEffect screenEffect = canvas.GetComponentInChildren<ScreenEffect>();
                        if (screenEffect != null)
                        {
                            return screenEffect;
                        }
                    }
                    // Ako je Canvas u "Screen Space - Camera" modu, provjeri da li koristi našu kameru
                    else if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == targetCamera)
                    {
                        ScreenEffect screenEffect = canvas.GetComponentInChildren<ScreenEffect>();
                        if (screenEffect != null)
                        {
                            return screenEffect;
                        }
                    }
                }
            }
        }
        
        // Fallback: ako nije pronađen, koristi FindObjectOfType
        return FindObjectOfType<ScreenEffect>();
    }

    /// Ažuriraj statistike za kill
    private void UpdateStatsForKill(GameObject victim, ulong shooterId, ulong victimId)
    {
        if (!IsServer) return;

        // Pronađi NetworkPlayerStats komponente za shooter i victim
        NetworkPlayerStats shooterStats = null;
        NetworkPlayerStats victimStats = null;

        // Pronađi shooter stats, traži sve NetworkObject-e i NetworkPlayerStats komponente
        // Traži i za shooterId = 0 (igrač) i za druge ID-ove
        NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
        foreach (var stats in allStats)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned && obj.OwnerClientId == shooterId)
            {
                shooterStats = stats;
                break;
            }
        }

        // Pronađi victim stats
        if (victim != null)
        {
            victimStats = victim.GetComponent<NetworkPlayerStats>();
        }

        // Ako je samoubojstvo, provjeri lastAttacker za assist kill
        if (shooterId == victimId)
        {
            if (victimStats != null)
            {
                victimStats.AddDeath();
            }
            
            // Provjeri je li netko ranio žrtvu prije samoubojstva
            if (victim != null)
            {
                NetworkHealth victimHealth = victim.GetComponent<NetworkHealth>();
                if (victimHealth != null)
                {
                    ulong lastAttackerId = victimHealth.GetLastAttackerId();
                    
                    // Ako postoji lastAttacker koji nije žrtva sama, daj mu kill
                    if (lastAttackerId != ulong.MaxValue && lastAttackerId != victimId)
                    {
                        // Pronađi stats napadača
                        foreach (var stats in allStats)
                        {
                            NetworkObject obj = stats.GetComponent<NetworkObject>();
                            if (obj != null && obj.IsSpawned && obj.OwnerClientId == lastAttackerId)
                            {
                                stats.AddKill();
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Ako je kill, dodaj KILL shooteru i DEATH victimu
            if (shooterStats != null)
            {
                shooterStats.AddKill();
            }
            if (victimStats != null)
            {
                victimStats.AddDeath();
            }
        }

        // Ispiši sve statistike svih igrača
        LogAllPlayerStats();
    }

    /// Ispiši sve statistike svih igrača
    private void LogAllPlayerStats()
    {
        NetworkSpawnManager.LogAllPlayerStats();
    }

    /// Vizualizacija dometa eksplozije
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}