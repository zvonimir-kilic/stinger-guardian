using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorHidden : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Ne zaključavaj cursor u lobby sceni
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MultiplayerGameLobby")
        {
            return; // Ne zaključavaj cursor u lobby sceni
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
