using UnityEngine;
using System.Collections;

// Mrežna verzija Shake skripte koja može shakeati PlayerCamera i JetCamera
public class NetworkMultiplayerShake : MonoBehaviour
{
    private Camera playerCamera;
    private Camera jetCamera;
    private Animator playerCamAnim;
    private Animator jetCamAnim;

    void Start()
    {
        FindCameras();
    }

    /// Pronađi kamere (može se pozvati i kasnije ako se kamere spawnaju nakon Start)
    private void FindCameras()
    {
        // Pronađi PlayerCamera
        GameObject playerCamObj = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamObj != null)
        {
            playerCamera = playerCamObj.GetComponent<Camera>();
            if (playerCamera != null)
            {
                playerCamAnim = playerCamera.GetComponent<Animator>();
            }
        }

        // Pronađi JetCamera
        GameObject jetCamObj = GameObject.FindGameObjectWithTag("JetCamera");
        if (jetCamObj != null)
        {
            jetCamera = jetCamObj.GetComponent<Camera>();
            if (jetCamera != null)
            {
                jetCamAnim = jetCamera.GetComponent<Animator>();
            }
        }
    }

    /// Shakea PlayerCamera ako je aktivna
    public void ShakePlayerCamera()
    {
        // Pokušaj ponovo pronaći kamere ako nisu pronađene
        if (playerCamera == null || playerCamAnim == null)
        {
            FindCameras();
        }

        if (playerCamera != null && playerCamera.enabled && playerCamAnim != null)
        {
            playerCamAnim.SetTrigger("shake");
        }
    }

    /// Shakea JetCamera ako je aktivna
    public void ShakeJetCamera()
    {
        // Pokušaj ponovo pronaći kamere ako nisu pronađene
        if (jetCamera == null || jetCamAnim == null)
        {
            FindCameras();
        }

        // Shakeaj kameru samo ako je enabled (samo vlasnik ima enabled kameru)
        if (jetCamera != null && jetCamera.enabled && jetCamAnim != null)
        {
            // Spremi trenutnu poziciju prije shake-a (zaštita od resetiranja)
            Vector3 originalPosition = jetCamera.transform.localPosition;
            
            jetCamAnim.SetTrigger("shake");
            
            // Osiguraj da se pozicija ne resetira nakon shake-a
            StartCoroutine(RestoreCameraPosition(jetCamera.transform, originalPosition));
        }
    }

    /// Shakea obje kamere ako su aktivne
    public void ShakeAllCameras()
    {
        ShakePlayerCamera();
        ShakeJetCamera();
    }

    /// Shakea aktivnu kameru (PlayerCamera ili JetCamera)
    public void ShakeActiveCamera()
    {
        // Provjeri koja je kamera aktivna i shakeaj je
        if (playerCamera != null && playerCamera.enabled && playerCamAnim != null)
        {
            playerCamAnim.SetTrigger("shake");
        }
        
        if (jetCamera != null && jetCamera.enabled && jetCamAnim != null)
        {
            jetCamAnim.SetTrigger("shake");
        }
    }
    
    /// Korutina koja osigurava da se pozicija kamere ne resetira nakon shake-a
    private IEnumerator RestoreCameraPosition(Transform cameraTransform, Vector3 originalPosition)
    {
        yield return new WaitForSeconds(0.1f); // Kratko čekanje
        
        // Ako se pozicija resetirala, vrati je
        if (cameraTransform != null && Vector3.Distance(cameraTransform.localPosition, originalPosition) > 0.1f)
        {
            cameraTransform.localPosition = originalPosition;
        }
    }
}