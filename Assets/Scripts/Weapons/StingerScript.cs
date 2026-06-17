using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class StingerScript : MonoBehaviour
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
    }

    /// Pronađi aktivnu kameru za igrača
    private void FindPlayerCamera()
    {
        // Prvo probaj Camera.main (Mission Mode)
        if (Camera.main != null)
        {
            playerCamera = Camera.main;
            return;
        }

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
        // Provjera tipke za pucanje
        if (Input.GetKeyDown(fireKey) && readyToShoot)
        {
            Shoot();
        }

        UpdateCrosshairPosition();
    }

    private void Shoot()
    {   
        if (targetingSystem != null && targetingSystem.targetIndicator.color == Color.green)
        {
            Transform target = targetingSystem.currentTarget;
            if (target != null)
            {
                readyToShoot = false;

                GameObject currentMissile = Instantiate(missilePrefab, missileLaunchPoint.position, missileLaunchPoint.rotation);

                // Dodaj GuidedMissile i postavi metu
                GuidedMissile guidedMissile = currentMissile.GetComponent<GuidedMissile>();
                if (guidedMissile != null)
                {
                    guidedMissile.SetTarget(target.gameObject);
                }

                if (muzzleFlash != null)
                {
                    Instantiate(muzzleFlash, missileLaunchPoint.position, Quaternion.identity);
                }

            // Resetiraj cooldown za pucanje
            Invoke("ResetShot", timeBetweenShooting);
            StartCoroutine(DestroyMissileAfterTime(currentMissile, missileLifeTime));
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

        // Stvori projektil s dosljednom rotacijom
        GameObject currentMissile = Instantiate(missilePrefab, missileLaunchPoint.position, missileLaunchPoint.rotation);
        GuidedMissile guidedMissile = currentMissile.GetComponent<GuidedMissile>();
        if (guidedMissile != null)
        {
            Destroy(guidedMissile); // Ukloni logiku za vođenje
        }

        // Primijeni silu u smjeru kretanja uz raspršenje
        Rigidbody missileRb = currentMissile.GetComponentInChildren<Rigidbody>();
        if (missileRb != null)
        {
            missileRb.AddForce(directionWithSpread * shootForce, ForceMode.Impulse);
        }

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
        StartCoroutine(DestroyMissileAfterTime(currentMissile, missileLifeTime));
        }
    }

    private IEnumerator DestroyMissileAfterTime(GameObject missile, float missileLifeTime)
    {
        yield return new WaitForSeconds(missileLifeTime);
        Destroy(missile);
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void UpdateCrosshairPosition()
    {
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
}