using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject selectedButton;

    public void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PlayGame()
    {
        // Učitaj Mission scenu
        SceneManager.LoadScene("SampleScene");
        if (EventSystem.current != null && selectedButton != null)
        {
            // Postavi gumb kao trenutno odabrani
            EventSystem.current.SetSelectedGameObject(selectedButton);
        }
    }

    public void OpenMultiplayer()
    {
        // Učitaj Multiplayer Lobby scenu
        SceneManager.LoadScene("MultiplayerLobby");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
