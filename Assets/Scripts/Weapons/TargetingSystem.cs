using UnityEngine;
using UnityEngine.UI;  // Za rad sa UI Image

public class TargetingSystem : MonoBehaviour
{
    public GameObject crosshair;
    public Image targetIndicator;  // UI Image za žarulju
    public float lockOnTime = 1f;  // Vrijeme za lockanje mete
    private float timer = 0f;
    private bool isLockingOn = false;

    private Color defaultColor = Color.yellow;  // Početna boja žarulje (žuta)
    private Color lockOnColor = Color.red;      // Boja kada je meta detektirana (crvena)
    private Color lockedColor = Color.green;    // Boja nakon lockanja (zelena)

    public Transform currentTarget;  // Trenutna naciljana meta

    private Camera activeCamera;  // Aktivna kamera za raycast

    void Start()
    {
        // Postavi početnu boju žarulje na žutu
        targetIndicator.color = defaultColor;
        FindActiveCamera();
    }

    /// Pronađi aktivnu kameru
    private void FindActiveCamera()
    {
        // Prvo pokušaj pronaći PlayerCamera
        GameObject playerCamObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamObj != null)
        {
            Camera cam = playerCamObj.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                activeCamera = cam;
                return;
            }
        }

        // Fallback na Camera.main
        if (Camera.main != null)
        {
            activeCamera = Camera.main;
        }
    }

    void Update()
    {
        // Ako nema aktivne kamere, pokušaj je pronaći
        if (activeCamera == null || !activeCamera.enabled)
        {
            FindActiveCamera();
        }

        // Ako još uvijek nema kamere ili crosshair-a, izađi
        if (activeCamera == null || crosshair == null || targetIndicator == null)
            return;
        
        RaycastHit hit;
        Ray ray = activeCamera.ScreenPointToRay(crosshair.transform.position);  // Crosshair ostaje isti

        if (Physics.Raycast(ray, out hit))
        {
            // Provjeri i "Enemy" (Mission mode) i "Jet" (Multiplayer mode) tagove
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Jet"))
            {
                currentTarget = hit.transform;  // Postavi metu
                if (isLockingOn)
                {
                    timer += Time.deltaTime;
                    if (timer >= lockOnTime)
                    {
                        targetIndicator.color = lockedColor;  // Postavi boju na zelenu nakon 2 sekunde
                    }
                }
                else
                {
                    isLockingOn = true;
                    timer = 0f;
                    targetIndicator.color = lockOnColor;  // Postavi boju na crvenu kada je meta detektirana
                }
            }
            else
            {
                ResetLockOn();
            }
        }
        else
        {
            ResetLockOn();
        }
    }

    void ResetLockOn()
    {
        isLockingOn = false;
        timer = 0f;
        targetIndicator.color = defaultColor;  // Vraća boju na žutu kada nema mete
        currentTarget = null;  // Očisti metu
    }
}