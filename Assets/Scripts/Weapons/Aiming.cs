using UnityEngine;

public class Aiming : MonoBehaviour
{
    public Camera playerCamera;  // Referenca na kameru
    public GameObject reticle;   // UI element za ciljnik
    public GameObject lightbulb; // Žaruljica
    public GameObject gun;       // Stinger
    public float zoomFOV = 30f;  // FOV pri zumiranju
    public float normalFOV = 60f; // Normalni FOV
    public float zoomSpeed = 5f; // Brzina zumiranja

    private bool isAiming = false; // Je li igrač u fazi ciljanja

    // Dodaj referencu na skriptu za pucanje
    public StingerScript stingerScript;

    void Update()
    {
        HandleAiming();
    }

    void HandleAiming()
    {
        // Provjera je li pritisnut desni klik
        if (Input.GetMouseButtonDown(1)) // 1 je desni klik miša
        {
            isAiming = true;
            ToggleReticle(true);  // Aktiviraj ciljnik

            // Sakrij samo djecu objekta gun
            SetChildrenActive(gun, false);
        }

        // Provjera je li pušten desni klik
        if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            ToggleReticle(false);  // Sakrij ciljnik

            // Prikaži samo djecu objekta gun
            SetChildrenActive(gun, true);
        }

        // Ako igrač cilja, zumiraj kameru
        if (isAiming)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, Time.deltaTime * zoomSpeed);

            // Omogući pucanje dok cilja
            if (stingerScript != null)
            {
                stingerScript.enabled = true;
            }
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.deltaTime * zoomSpeed);

            // Omogući pucanje kada ne cilja
            if (stingerScript != null)
            {
                stingerScript.enabled = true;
            }
        }
    }

    // Funkcija za uključivanje/isključivanje ciljnika
    void ToggleReticle(bool show)
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
    void SetChildrenActive(GameObject parent, bool isActive)
    {
        foreach (Transform child in parent.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }
}
