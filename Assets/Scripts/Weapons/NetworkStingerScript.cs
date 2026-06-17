using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// Mrežna verzija StingerScript-a koja omogućava mrežno pucanje
public class NetworkStingerScript : NetworkBehaviour
{
    public TargetingSystem targetingSystem;  // Referenca na TargetingSystem
    public AudioSource audioSource;

    // Referenca na kameru
    [Header("Camera Reference")]
    public Camera playerCamera;

    // Referenca na prefab rakete
    public GameObject missilePrefab;

    // Sila ispaljivanja rakete
    public float shootForce;
    public float guidedMissileForce = 298f; // 15% sporija od shootForce za auto-lock
    public float upwardForce = 0f;

    // Tipka za pucanje
    public KeyCode fireKey = KeyCode.Mouse0;

    // Pozicija iz koje će raketa biti ispaljena
    public Transform missileLaunchPoint;

    // Vrijeme između pucanja
    public float timeBetweenShooting = 0.5f;

    // Je li spremno za pucanje
    private bool readyToShoot = true;

    // Spread za preciznost rakete
    public float spread = 0.1f;

    // Grafički elementi
    public GameObject muzzleFlash;

    public float missileLifeTime = 10f;

    // UI Crosshair
    public Image crosshairDot;

    private void Start()
    {
        if (crosshairDot != null)
        {
            crosshairDot.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairDot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairDot.rectTransform.anchoredPosition = Vector2.zero;
        }
        
        // Ako kamera nije postavljena, pokušaj pronaći
        if (playerCamera == null)
        {
            FindPlayerCamera();
        }
        
        // Ako targetingSystem nije postavljen, pokušaj pronaći
        if (targetingSystem == null)
        {
            FindTargetingSystem();
        }
    }
    
    /// Pronađi TargetingSystem u sceni
    private void FindTargetingSystem()
    {
        targetingSystem = FindObjectOfType<TargetingSystem>();
        if (targetingSystem != null)
        {
            Debug.Log("NetworkStingerScript: Found TargetingSystem");
        }
        else
        {
            Debug.LogWarning("NetworkStingerScript: TargetingSystem not found!");
        }
    }

    /// Pronađi aktivnu kameru za igrača
    private void FindPlayerCamera()
    {
        // Traži kameru s tagom "PlayerCamera"
        GameObject playerCamObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamObj != null)
        {
            Camera cam = playerCamObj.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                playerCamera = cam;
                return;
            }
        }
    }

    /// Vraća aktivnu kameru za pucanje
    private Camera GetActiveCamera()
    {
        if (playerCamera != null && playerCamera.enabled)
            return playerCamera;
        
        // Pokušaj ponovo pronaći kameru
        FindPlayerCamera();
        return playerCamera;
    }

    private void Update()
    {
        // Samo vlasnik može pucati
        if (!IsOwner) return;

        // Provjera tipke za pucanje
        if (Input.GetKeyDown(fireKey) && readyToShoot)
        {
            Shoot();
        }

        UpdateCrosshairPosition();
    }

    private void Shoot()
    {   
        // Provjeri je li target zaključan
        bool isTargetLocked = targetingSystem != null && 
                              targetingSystem.targetIndicator != null &&
                              IsColorApproximatelyEqual(targetingSystem.targetIndicator.color, Color.green);
        
        if (isTargetLocked)
        {
            Transform target = targetingSystem.currentTarget;
            if (target != null)
            {
                readyToShoot = false;

                // Pronađi NetworkObject targeta ako postoji
                ulong targetNetworkObjectId = 0;
                NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();
                if (targetNetworkObject != null && targetNetworkObject.IsSpawned)
                {
                    targetNetworkObjectId = targetNetworkObject.NetworkObjectId;
                }

                // Pozovi ServerRpc da spawna raketu na mreži
                Vector3 targetPosition = target.position;
                ShootMissileWithTargetServerRpc(missileLaunchPoint.position, missileLaunchPoint.rotation, targetPosition, targetNetworkObjectId);

                if (muzzleFlash != null)
                {
                    Instantiate(muzzleFlash, missileLaunchPoint.position, Quaternion.identity);
                }

                // Resetiraj cooldown za pucanje
                Invoke("ResetShot", timeBetweenShooting);
            }
        }
        else
        {
            readyToShoot = false;

            // Provjeri da li postoji kamera
            Camera activeCamera = GetActiveCamera();
            if (activeCamera == null)
            {
                Debug.LogWarning("No active camera found! Cannot shoot.");
                readyToShoot = true;
                return;
            }

            Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out hit, 100000f))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(75); 
            }

            // Dodaj nasumično odstupanje meti
            float x = Random.Range(-spread, spread);
            float y = Random.Range(-spread, spread);
            Vector3 targetWithSpread = targetPoint + new Vector3(x, y, 0);

            // Izračunaj smjer s odstupanjem
            Vector3 directionWithSpread = (targetWithSpread - missileLaunchPoint.position).normalized;

            // Pozovi ServerRpc da spawna raketu na mreži
            ShootMissileServerRpc(missileLaunchPoint.position, missileLaunchPoint.rotation, targetWithSpread);

            // Bljesak iz cijevi
            if (muzzleFlash != null)
            {
                Instantiate(muzzleFlash, missileLaunchPoint.position, Quaternion.identity);
            }

            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            // Resetiraj vrijeme čekanja između pucnjeva
            Invoke("ResetShot", timeBetweenShooting);
        }
    }

    [ServerRpc]
    private void ShootMissileServerRpc(Vector3 launchPosition, Quaternion launchRotation, Vector3 targetPoint)
    {
        // Server spawna raketu
        GameObject missileInstance = Instantiate(missilePrefab, launchPosition, launchRotation);
        
        // Spawnaj na mreži
        NetworkObject missileNetworkObject = missileInstance.GetComponent<NetworkObject>();
        if (missileNetworkObject != null)
        {
            // Postavi ownership rakete na vlasnika koji ju je ispalio
            missileNetworkObject.SpawnWithOwnership(OwnerClientId);
            
            // Postavi smjer i silu rakete
            Vector3 direction = (targetPoint - launchPosition).normalized;
            Rigidbody missileRb = missileInstance.GetComponentInChildren<Rigidbody>();
            if (missileRb != null)
            {
                missileRb.AddForce(direction * shootForce, ForceMode.Impulse);
            }
            
            // Uništi raketu nakon određenog vremena
            StartCoroutine(DestroyMissileAfterTime(missileInstance, missileLifeTime));
        }
        else
        {
            Debug.LogError("NetworkMissile prefab nema NetworkObject komponentu!");
            Destroy(missileInstance);
        }
    }

    [ServerRpc]
    private void ShootMissileWithTargetServerRpc(Vector3 launchPosition, Quaternion launchRotation, Vector3 targetPoint, ulong targetNetworkObjectId)
    {
        // Server spawna raketu
        GameObject missileInstance = Instantiate(missilePrefab, launchPosition, launchRotation);
        
        // Spawnaj na mreži
        NetworkObject missileNetworkObject = missileInstance.GetComponent<NetworkObject>();
        if (missileNetworkObject != null)
        {
            // Postavi ownership rakete na vlasnika koji ju je ispalio
            missileNetworkObject.SpawnWithOwnership(OwnerClientId);
            
            // Ako postoji target NetworkObject, postavi target na NetworkGuidedMissile
            if (targetNetworkObjectId != 0)
            {
                // Pronađi target NetworkObject po ID-u
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetNetworkObject))
                {
                    if (targetNetworkObject != null)
                    {
                        // Pronađi NetworkGuidedMissile komponentu na raketi
                        NetworkGuidedMissile guidedMissile = missileInstance.GetComponent<NetworkGuidedMissile>();
                        
                        // Postavi target na NetworkGuidedMissile
                        if (guidedMissile != null && targetNetworkObject.gameObject != null)
                        {
                            guidedMissile.SetTarget(targetNetworkObject.gameObject);
                        }
                        else if (guidedMissile == null)
                        {
                            Debug.LogWarning("NetworkGuidedMissile component not found on missile prefab! Add it in Unity Editor.");
                        }
                    }
                }
            }
            
            // Postavi smjer i silu rakete
            Vector3 direction = (targetPoint - launchPosition).normalized;
            Rigidbody missileRb = missileInstance.GetComponentInChildren<Rigidbody>();
            if (missileRb != null)
            {
                missileRb.AddForce(direction * guidedMissileForce, ForceMode.Impulse);
            }
            
            // Uništi raketu nakon određenog vremena
            StartCoroutine(DestroyMissileAfterTime(missileInstance, missileLifeTime));
        }
        else
        {
            Debug.LogError("NetworkMissile prefab nema NetworkObject komponentu!");
            Destroy(missileInstance);
        }
    }

    private IEnumerator DestroyMissileAfterTime(GameObject missile, float missileLifeTime)
    {
        yield return new WaitForSeconds(missileLifeTime);
        if (missile != null)
        {
            NetworkObject missileNetworkObject = missile.GetComponent<NetworkObject>();
            if (missileNetworkObject != null && missileNetworkObject.IsSpawned)
            {
                missileNetworkObject.Despawn();
            }
            Destroy(missile);
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void UpdateCrosshairPosition()
    {
        // Samo vlasnik vidi crosshair
        if (!IsOwner) return;

        Camera activeCamera = GetActiveCamera();
        if (crosshairDot != null && activeCamera != null)
        {
            Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Promijeni izgled ciljnika ako pokazuje na neprijatelja
                crosshairDot.color = Color.red; // Meta detektirana
            }
            else
            {
                crosshairDot.color = Color.white; // Zadana boja
            }
        }
    }
    
    /// Usporedi dvije boje s tolerancijom za float nepreciznost
    private bool IsColorApproximatelyEqual(Color a, Color b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }
}

