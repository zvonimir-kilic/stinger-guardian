using Unity.Netcode;
using UnityEngine;

// Mrežna verzija Aiming skripte
public class NetworkAiming : NetworkBehaviour
{
    [Header("Zoom Settings")]
    public float zoomFOV = 20f;
    public float normalFOV = 60f;
    public float zoomSpeed = 5f;

    private Canvas canvas;
    private Camera playerCamera;
    private GameObject reticle;
    private GameObject lightbulb;
    private GameObject gun;
    private bool isAiming = false;
    private NetworkStingerScript networkStingerScript;
    private bool uiElementsFound = false;

    private void Start()
    {
        // Samo vlasnik koristi ovu skriptu
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // Dohvati kameru i traži po tagu PlayerCamera
        GameObject playerCamObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamObj != null)
        {
            playerCamera = playerCamObj.GetComponent<Camera>();
        }
        
        // Fallback dohvati kameru iz player-a
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        // Pronađi gun model u player-u
        if (playerCamera != null)
        {
            Transform gunTransform = playerCamera.transform.Find("FIM92-Stinger");
            if (gunTransform != null)
                gun = gunTransform.gameObject;
        }

        // Pronađi NetworkStingerScript umjesto StingerScript
        networkStingerScript = GetComponentInChildren<NetworkStingerScript>();
    }

    private void TryFindUIElements()
    {
        // Pronađi sve Canvas-e u sceni
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        
        foreach (Canvas c in allCanvases)
        {
            // Traži Target i LightBulb u svakom Canvas-u
            Transform targetTransform = c.transform.Find("Target");
            Transform lightbulbTransform = c.transform.Find("LightBulb");
            
            if (targetTransform != null)
            {
                reticle = targetTransform.gameObject;
            }
            
            if (lightbulbTransform != null)
            {
                lightbulb = lightbulbTransform.gameObject;
            }
            
            // Ako smo pronašli oba, izađi iz petlje
            if (reticle != null && lightbulb != null)
            {
                canvas = c;
                break;
            }
        }
        
        // Označi kao pronađeno čak i ako nisu svi elementi pronađeni
        // da se ciljanje može koristiti bez UI elemenata
        uiElementsFound = true;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Pokušaj pronaći UI elemente ako još nisu pronađeni
        if (!uiElementsFound)
        {
            TryFindUIElements();
        }

        HandleAiming();
    }

    private void HandleAiming()
    {
        // Provjera je li pritisnut desni klik
        if (Input.GetMouseButtonDown(1)) 
        {
            isAiming = true;
            ToggleReticle(true);  // Aktiviraj ciljnik

            // Sakrij samo djecu objekta gun
            if (gun != null)
                SetChildrenActive(gun, false);
        }

        // Provjera je li pušten desni klik
        if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            ToggleReticle(false);  // Sakrij ciljnik

            // Prikaži samo djecu objekta gun
            if (gun != null)
                SetChildrenActive(gun, true);
        }

        // Ako igrač cilja, zumiraj kameru
        if (isAiming && playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, Time.deltaTime * zoomSpeed);

            // Omogući pucanje dok cilja
            if (networkStingerScript != null)
            {
                networkStingerScript.enabled = true;
            }
        }
        else if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * zoomSpeed);

            // Omogući pucanje kada ne cilja
            if (networkStingerScript != null)
            {
                networkStingerScript.enabled = true;
            }
        }
    }

    // Funkcija za uključivanje/isključivanje ciljnika
    private void ToggleReticle(bool show)
    {
        if (reticle != null)
        {
            reticle.SetActive(show);
        }

        if (lightbulb != null)
        {
            lightbulb.SetActive(show);
        }
    }

    // Funkcija za sakrivanje ili prikazivanje samo djece objekta
    private void SetChildrenActive(GameObject parent, bool isActive)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }

    private void OnDestroy()
    {
        // Vrati UI u isključeno stanje
        if (reticle != null)
            reticle.SetActive(false);

        if (lightbulb != null)
            lightbulb.SetActive(false);
    }
}