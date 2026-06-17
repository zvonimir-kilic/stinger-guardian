using Unity.Netcode;
using UnityEngine;
using System.Collections;

// Mrežna verzija skripte za pucanje iz aviona
public class NetworkJetShootingScript : NetworkBehaviour
{
    [Header("Camera Reference")]
    public Camera jetCamera;

    [Header("Missile Settings")]
    public GameObject missilePrefab; // NetworkMissile prefab
    public float shootForce = 350f; // Ista brzina kao Stinger
    public float timeBetweenShooting = 1f; // Vrijeme između pucanja (reload)
    public float spread = 0.05f; // Manji spread za preciznije pucanje
    public float missileLifeTime = 10f;

    [Header("Launch Points")]
    public Transform leftLaunchPoint; // Lijevi launch point
    public Transform rightLaunchPoint; // Desni launch point

    [Header("Audio")]
    private AudioSource leftAudioSource;
    private AudioSource rightAudioSource;

    [Header("Visual Effects")]
    public GameObject muzzleFlash; // Opcionalni bljesak pri pucanju

    // Tipka za pucanje
    public KeyCode fireKey = KeyCode.Mouse0;

    // Je li spremno za pucanje
    private bool readyToShoot = true;

    // Indikator koji launch point koristiti
    private bool useLeftLaunchPoint = true;

    private void Start()
    {
        // Ako kamera nije postavljena, pokušaj pronaći
        if (jetCamera == null)
        {
            FindJetCamera();
        }
        
        // Pronađi AudioSource komponente na launch pointovima
        FindLaunchPointAudioSources();
    }
    
    /// Pronađi AudioSource komponente na launch pointovima
    private void FindLaunchPointAudioSources()
    {
        if (leftLaunchPoint != null)
        {
            leftAudioSource = leftLaunchPoint.GetComponent<AudioSource>();
        }
        
        if (rightLaunchPoint != null)
        {
            rightAudioSource = rightLaunchPoint.GetComponent<AudioSource>();
        }
    }

    /// Pronađi JetCamera
    private void FindJetCamera()
    {
        // Traži kameru s tagom "JetCamera"
        GameObject jetCamObj = GameObject.FindGameObjectWithTag("JetCamera");
        if (jetCamObj != null)
        {
            Camera cam = jetCamObj.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                jetCamera = cam;
                return;
            }
        }

        // Ako nije pronađena po tagu, pokušaj pronaći unutar parent objekta
        if (transform.parent != null)
        {
            Camera cam = transform.parent.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                jetCamera = cam;
            }
        }
    }

    /// Vraća aktivnu kameru za pucanje
    private Camera GetActiveCamera()
    {
        if (jetCamera != null && jetCamera.enabled)
            return jetCamera;
        
        // Pokušaj ponovo pronaći kameru
        FindJetCamera();
        return jetCamera;
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
    }

    private void Shoot()
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

        // Odaberi launch point
        Transform currentLaunchPoint = useLeftLaunchPoint ? leftLaunchPoint : rightLaunchPoint;
        
        if (currentLaunchPoint == null)
        {
            Debug.LogError("Launch point is null! Cannot shoot.");
            readyToShoot = true;
            return;
        }

        // Raycast iz centra kamere
        Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, 100000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            // Ako nema kolizije, koristi točku daleko ispred
            targetPoint = ray.GetPoint(1000f);
        }

        // Dodaj nasumično odstupanje meti
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        Vector3 targetWithSpread = targetPoint + new Vector3(x, y, 0);

        // Pozovi ServerRpc da spawna raketu na mreži
        ShootMissileServerRpc(
            currentLaunchPoint.position, 
            currentLaunchPoint.rotation, 
            targetWithSpread
        );

        // Bljesak iz cijevi
        if (muzzleFlash != null)
        {
            Instantiate(muzzleFlash, currentLaunchPoint.position, currentLaunchPoint.rotation);
        }

        // Zvuk pucanja koristi AudioSource na launch pointu ako postoji
        AudioSource sourceToUse = null;
        if (useLeftLaunchPoint && leftAudioSource != null)
        {
            sourceToUse = leftAudioSource;
        }
        else if (!useLeftLaunchPoint && rightAudioSource != null)
        {
            sourceToUse = rightAudioSource;
        }
        
        if (sourceToUse != null)
        {
            // Koristi PlayOneShot za preklapanje zvukova
            if (sourceToUse.clip != null)
            {
                sourceToUse.PlayOneShot(sourceToUse.clip);
            }
            else
            {
                sourceToUse.Play();
            }
        }

        // Alterniraj launch point za sljedeći put
        useLeftLaunchPoint = !useLeftLaunchPoint;

        // Resetiraj vrijeme čekanja između pucnjeva
        Invoke("ResetShot", timeBetweenShooting);
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
}