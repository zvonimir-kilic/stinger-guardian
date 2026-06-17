using UnityEngine;

public class PositionQuadInFrontOfCamera : MonoBehaviour
{
    public Camera mainCamera;  // Referenca na glavnu kameru
    public float distanceFromCamera = 1f;  // Udaljenost na kojoj postaviti Stinger ispred kamere
    public float quadSize = 2f;  // Veličina Stingera

    private bool isAiming = false;  // Prati cilja li igrač

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;  // Postavi glavnu kameru ukoliko nije
        }

        SetQuadSize();
    }

    void Update()
    {
        // Pazi da Stinger ostane ispred kamere
        PositionQuad();

        // Prilagodi veličinu ako je zumirano
        AdjustQuadSizeBasedOnZoom();
    }

    private void PositionQuad()
    {
        // Smjer kamere prema naprijed
        Vector3 cameraForward = mainCamera.transform.forward;

        // Postavi Stinger točno ispred kamere
        Vector3 positionInFront = mainCamera.transform.position + cameraForward * distanceFromCamera;

        // Postavi poziciju Stingera
        transform.position = positionInFront;

        // Provjeri je li Stinger okrenut prema kameri
        transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        transform.Rotate(0, 180, 0);
    }

    private void AdjustQuadSizeBasedOnZoom()
    {
        // Ako igrač cilja, smanji veličinu Stingera
        if (isAiming)
        {
            transform.localScale = new Vector3(quadSize * 0.5f, quadSize * 0.5f, 1);
        }
        else
        {
            transform.localScale = new Vector3(quadSize, quadSize, 1);
        }
    }

    // Funkcija za ažuriranje stanja ciljanja
    public void SetAimingState(bool aiming)
    {
        isAiming = aiming;
    }

    private void SetQuadSize()
    {
        transform.localScale = new Vector3(quadSize, quadSize, 1);
    }
}
