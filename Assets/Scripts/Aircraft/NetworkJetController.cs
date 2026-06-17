using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Mrežni wrapper za kontrolu Jet-a koji omogućava da samo vlasnik kontrolira svog aviona
public class NetworkJetController : NetworkBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera jetCamera;
    [SerializeField] private AudioListener audioListener;
    
    [Header("Camera Position (lokalna pozicija relativna na avion)")]
    [SerializeField] private Vector3 jetCameraLocalPosition = new Vector3(0f, 67.5f, 345f);
    
    private PlaneControllerSimple planeControllerSimple;
    private Rigidbody jetRigidbody;
    
    private bool freezeYPosition = false;
    private float frozenYPosition;

    private void Awake()
    {
        // Dohvati postojeće komponente
        planeControllerSimple = GetComponent<PlaneControllerSimple>();
        jetRigidbody = GetComponent<Rigidbody>();
        
        // Ako kamera nije postavljena u Inspectoru, pokušaj pronaći unutar ovog objekta
        if (jetCamera == null)
        {
            // Traži kameru samo unutar djece ovog objekta
            foreach (Transform child in transform)
            {
                Camera cam = child.GetComponent<Camera>();
                if (cam != null)
                {
                    jetCamera = cam;
                    audioListener = child.GetComponent<AudioListener>();
                    break;
                }
                
                // Ako nije direktno dijete, traži dublje
                cam = child.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    jetCamera = cam;
                    audioListener = cam.GetComponent<AudioListener>();
                    break;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Provjeri je li u lobby sceni, ako je onda ne omogućavaj komponente
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MultiplayerGameLobby")
        {
            // Pronađi NetworkLobbySpawnManager i postavi poziciju aviona
            // Pozicija se postavlja SAMO na vlasniku jer ClientNetworkTransform daje kontrolu klijentu
            if (IsOwner)
            {
                // Čekaj malo da se scena potpuno učita
                StartCoroutine(SetLobbyPositionAfterSceneLoad());
            }
            
            // Onemogući sve komponente za lobby
            if (planeControllerSimple != null)
                planeControllerSimple.enabled = false;
            
            NetworkJetShootingScript jetShooting = GetComponent<NetworkJetShootingScript>();
            if (jetShooting != null)
                jetShooting.enabled = false;
            
            // Onemogući JetCamera
            if (jetCamera != null)
            {
                jetCamera.enabled = false;
                if (audioListener != null)
                    audioListener.enabled = false;
            }
            
            // Osiguraj da je Main Camera aktivna
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }
            else
            {
                GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCamObj != null)
                {
                    Camera cam = mainCamObj.GetComponent<Camera>();
                    if (cam != null) cam.enabled = true;
                }
            }
            
            // Osiguraj da je kursor vidljiv i otključan za UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        // Isključi fog u sceni
        RenderSettings.fog = false;

        // Postavi boundary centar na spawn poziciju vojnika
        if (planeControllerSimple != null)
        {
            // Pokušaj pronaći NetworkPlayer u sceni
            NetworkPlayerController playerController = FindObjectOfType<NetworkPlayerController>();
            if (playerController != null)
            {
                planeControllerSimple.boundaryCenter = playerController.transform.position;
                Debug.Log($"Boundary center set to NetworkPlayer position: {playerController.transform.position}");
            }
            else
            {
                // Ako nije pronađen, koristi default spawn poziciju vojnika
                planeControllerSimple.boundaryCenter = new Vector3(5605f, 36f, -80f); // playerSpawnPosition iz NetworkSpawnManager
                Debug.Log($"Boundary center set to default player spawn position: {planeControllerSimple.boundaryCenter}");
            }
        }

        // Postavi spawn poziciju, zamrzni Y na kratko i omogući kontrole samo za vlasnika aviona
        if (IsOwner)
        {
            // Onemogući komponente privremeno
            if (planeControllerSimple != null)
                planeControllerSimple.enabled = false;
            
            NetworkJetShootingScript jetShooting = GetComponent<NetworkJetShootingScript>();
            if (jetShooting != null)
                jetShooting.enabled = false;
            
            // Omogući komponente nakon 3 sekunde
            StartCoroutine(EnableJetComponentsDelayed());
            
            // Pozicija se već postavlja u NetworkSpawnManager, ali osiguravamo da je ispravna
            frozenYPosition = transform.position.y;
            freezeYPosition = false; // Ne zamrzavaj poziciju u glavnoj igri
            
            // EKSPLICITNO postavi poziciju JetCamera i postavke kamere
            if (jetCamera != null)
            {
                // Provjeri da je ovo zaista child ovog objekta
                if (jetCamera.transform.IsChildOf(transform))
                {
                    jetCamera.transform.localPosition = jetCameraLocalPosition;
                    Debug.Log($"JetCamera position set to: {jetCameraLocalPosition} (parent: {jetCamera.transform.parent?.name})");
                }
                else
                {
                    Debug.LogWarning($"JetCamera nije child od NetworkJet! Camera parent: {jetCamera.transform.parent?.name}");
                }
                
                // Postavi kameru da koristi Skybox umjesto Solid Color
                jetCamera.clearFlags = CameraClearFlags.Skybox;
                
                // Povećaj far clip plane da vidi dalje
                jetCamera.farClipPlane = 10000f;
                
                // Omogući kameru i audio samo za vlasnika aviona
                jetCamera.enabled = true;
            }
            
            if (audioListener != null)
                audioListener.enabled = true;

            // Zaključaj kursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Onemogući PlaneController komponente za remote avione
            if (planeControllerSimple != null)
                planeControllerSimple.enabled = false;
            
            // Onemogući pucanje za remote avione
            NetworkJetShootingScript jetShooting = GetComponent<NetworkJetShootingScript>();
            if (jetShooting != null)
                jetShooting.enabled = false;
            
            // Onemogući kameru i audio za remote avione
            if (jetCamera != null)
                jetCamera.enabled = false;
            
            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    private IEnumerator UnfreezePositionDelayed()
    {
        // Čekaj 2 sekunde
        yield return new WaitForSeconds(2f);
        
        // Omogući PlaneController
        if (planeControllerSimple != null)
            planeControllerSimple.enabled = true;
        
        // Odmrzni Y poziciju
        freezeYPosition = false;
    }

    private IEnumerator EnableJetComponentsDelayed()
    {
        // Čekaj 3 sekunde
        yield return new WaitForSeconds(3f);
        
        // Omogući komponente za glavnu igru
        if (planeControllerSimple != null)
            planeControllerSimple.enabled = true;
        
        NetworkJetShootingScript jetShooting = GetComponent<NetworkJetShootingScript>();
        if (jetShooting != null)
            jetShooting.enabled = true;
    }

    private System.Collections.IEnumerator SetLobbyPositionAfterSceneLoad()
    {
        // Čekaj da se scena potpuno učita
        yield return new WaitForSeconds(0.1f);
        
        NetworkLobbySpawnManager lobbySpawnManager = FindObjectOfType<NetworkLobbySpawnManager>();
        if (lobbySpawnManager != null && transform != null)
        {
            Vector3 targetPosition = lobbySpawnManager.JetLobbySpawnPosition;
            
            // ODMAH postavi poziciju 
            transform.position = targetPosition;
            
            // Zatim pokreni coroutine koji će ponovo postaviti poziciju nekoliko puta za sigurnost
            StartCoroutine(SetLobbyPositionDelayed(targetPosition));
            StartCoroutine(ForcePositionUntilStable(targetPosition));
        }
    }

    private System.Collections.IEnumerator SetLobbyPositionDelayed(Vector3 position)
    {
        // Čekaj da se objekt spawna na mreži više frame-ova za sigurnost
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (transform != null)
        {
            transform.position = position;
        }
        
        // Čekaj još malo i ponovo postavi za sigurnost
        yield return new WaitForSeconds(0.1f);
        
        if (transform != null)
        {
            transform.position = position;
        }
    }

    // Kontinuirano postavlja poziciju dok se ne stabilizira na ispravnoj poziciji
    private System.Collections.IEnumerator ForcePositionUntilStable(Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        float maxTime = 2f; // Maksimalno 2 sekunde pokušavanja
        float checkInterval = 0.05f; // Provjera svakih 50ms
        float tolerance = 50f; // Tolerancija za provjeru je li pozicija ispravna
        
        while (elapsedTime < maxTime)
        {
            if (transform == null) yield break;
            
            // Provjeri je li pozicija daleko od ciljne
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance > tolerance)
            {
                // Pozicija je kriva, postavi je ponovo
                transform.position = targetPosition;
            }
            else
            {
                // Pozicija je stabilna, izađi iz petlje
                yield break;
            }
            
            yield return new WaitForSeconds(checkInterval);
            elapsedTime += checkInterval;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Provjeri je li voda
        if (other.CompareTag("Water"))
        {
            // Pozovi ServerRpc za respawn
            RequestRespawnJetServerRpc();
        }
    }

    [ServerRpc]
    private void RequestRespawnJetServerRpc()
    {
        // Provjeri je li objekt još spawnan
        if (!IsSpawned) return;

        ulong ownerId = NetworkObject.OwnerClientId;
        
        // Provjeri je li netko ranio ovaj avion prije smrti
        NetworkHealth health = GetComponent<NetworkHealth>();
        ulong lastAttackerId = health != null ? health.GetLastAttackerId() : ulong.MaxValue;
        
        // Ažuriraj statistike
        NetworkPlayerStats victimStats = GetComponent<NetworkPlayerStats>();
        if (victimStats != null)
        {
            victimStats.AddDeath();
        }
        
        // Ako postoji lastAttacker, daj mu kill
        if (lastAttackerId != ulong.MaxValue && lastAttackerId != ownerId)
        {
            // Pronađi stats napadača
            NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
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
        
        // Ispiši sve statistike
        NetworkSpawnManager.LogAllPlayerStats();
        
        // Despawnaj avion
        NetworkObject.Despawn();
        Destroy(gameObject);

        // Respawnaj avion na početnoj poziciji
        NetworkSpawnManager spawnManager = FindObjectOfType<NetworkSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.RespawnJet(ownerId);
        }
    }
}

