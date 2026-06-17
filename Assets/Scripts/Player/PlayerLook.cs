using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;

    public float xSensitivity = 15f;
    public float ySensitivity = 15f;
  
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;
        // Izračunaj rotaciju kamere za pogled gore i dolje
        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        // Primijeni ovo na transformaciju kamere
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        // Okreni igrača da gleda lijevo i desno
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
