using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    public Transform waterSurface;

    private Vector3 playerVelocity;
    private bool isGrounded;
    public float speed = 6f;
    public float gravity = -9.8f;
    public float jumpHeight = 1f;

    private bool lerpCrouch = false;
    private float crouchTimer = 0f;
    private bool crouching = false;
    private bool sprinting = false;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller == null || !controller.enabled) return;
        
        isGrounded = controller.isGrounded;
        if (lerpCrouch)
        {
            crouchTimer += Time.deltaTime;
            float p = crouchTimer / 1;
            p *= p;
            if (crouching)
                controller.height = Mathf.Lerp(controller.height, 1, p);
            else
                controller.height = Mathf.Lerp(controller.height, 2, p);

            if (p > 1)
            {
                lerpCrouch = false;
                crouchTimer = 0f;
            }
        }
    }

    // Primamo ulazne podatke iz InputManager.cs te ih primjenjujemo na kontroler
    public void ProcessMove(Vector2 input)
    {
        if (controller == null || !controller.enabled) return;
        
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;
        controller.Move(transform.TransformDirection(moveDirection) * speed * Time.deltaTime);
        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

    public void Crouch()
    {
        crouching = !crouching;
        crouchTimer = 0;
        lerpCrouch = true;
    }

    public void Sprint()
    {
        sprinting = !sprinting;
        if (sprinting)
            speed = 12;
        else
            speed = 6;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            // Provjeri je li u multiplayer modu
            // Ako jest, NetworkPlayerController će se pobrinuti za respawn
            if (GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                KillPlayer();
            }
        }
    }

    private void KillPlayer()
    {
        SceneManager.LoadScene("GameOver");
    }
}