using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public static bool isPaused;
    
    void Start()
    {
        pauseMenu.SetActive(false);
    }
    
    // Pronađi player komponente
    private void FindPlayerComponents()
    {
        // Pokušaj naći Multiplayer NetworkPlayer
        GameObject networkPlayer = GameObject.Find("NetworkPlayer(Clone)");
        if (networkPlayer != null)
        {
            // Pronađi komponente NetworkPlayer-a
            InputManager inputManager = networkPlayer.GetComponent<InputManager>();
            NetworkAiming networkAiming = networkPlayer.GetComponent<NetworkAiming>();
            StingerScript stingerScript = networkPlayer.GetComponentInChildren<StingerScript>();
            
            // Disable komponente
            if (inputManager != null) inputManager.enabled = false;
            if (networkAiming != null) networkAiming.enabled = false;
            if (stingerScript != null) stingerScript.enabled = false;
        } 
        else 
        {
            // Pokušaj naći Mission Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Pronađi komponente Player-a
                InputManager inputManager = player.GetComponent<InputManager>();
                Aiming aiming = player.GetComponent<Aiming>();
                StingerScript stingerScript = player.GetComponentInChildren<StingerScript>();
                
                // Disable komponente
                if (inputManager != null) inputManager.enabled = false;
                if (aiming != null) aiming.enabled = false;
                if (stingerScript != null) stingerScript.enabled = false;
            }
        }
    }
    
    // Ponovno omogući player komponente
    private void EnablePlayerComponents()
    {
        // Pokušaj naći Multiplayer NetworkPlayer
        GameObject networkPlayer = GameObject.Find("NetworkPlayer(Clone)");
        if (networkPlayer != null)
        {
            InputManager inputManager = networkPlayer.GetComponent<InputManager>();
            NetworkAiming networkAiming = networkPlayer.GetComponent<NetworkAiming>();
            StingerScript stingerScript = networkPlayer.GetComponentInChildren<StingerScript>();
            
            // Enable komponente
            if (inputManager != null) inputManager.enabled = true;
            if (networkAiming != null) networkAiming.enabled = true;
            if (stingerScript != null) stingerScript.enabled = true;
        } 
        else 
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                InputManager inputManager = player.GetComponent<InputManager>();
                Aiming aiming = player.GetComponent<Aiming>();
                StingerScript stingerScript = player.GetComponentInChildren<StingerScript>();
                
                // Enable komponente
                if (inputManager != null) inputManager.enabled = true;
                if (aiming != null) aiming.enabled = true;
                if (stingerScript != null) stingerScript.enabled = true;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        // Provjeri mijenja li se kursor dok je paused
        if (isPaused)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } 
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        isPaused = true;
        
        // Disable player komponente
        FindPlayerComponents();
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        isPaused = false;
        
        // Ponovno omogući player komponente
        EnablePlayerComponents();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGamefromResume()
    {
        Application.Quit();
    }
}
