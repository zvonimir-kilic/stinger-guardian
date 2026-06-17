using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;
    public static bool isOpen;

    void Start()
    {
        settingsMenu.SetActive(false);
        isOpen = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    // Pronađi i onemogući player/jet komponente
    private void DisablePlayerComponents()
    {
        // Pokušaj naći Multiplayer NetworkPlayer
        GameObject networkPlayer = GameObject.Find("NetworkPlayer(Clone)");
        if (networkPlayer != null)
        {
            // Pronađi komponente NetworkPlayer-a
            InputManager inputManager = networkPlayer.GetComponent<InputManager>();
            NetworkAiming networkAiming = networkPlayer.GetComponent<NetworkAiming>();
            StingerScript stingerScript = networkPlayer.GetComponentInChildren<StingerScript>();
            NetworkStingerScript networkStingerScript = networkPlayer.GetComponentInChildren<NetworkStingerScript>();
            
            // Disable komponente
            if (inputManager != null) inputManager.enabled = false;
            if (networkAiming != null) networkAiming.enabled = false;
            if (stingerScript != null) stingerScript.enabled = false;
            if (networkStingerScript != null) networkStingerScript.enabled = false;
        }

        // Pokušaj naći NetworkJet
        GameObject[] networkJets = GameObject.FindGameObjectsWithTag("Jet");
        
        foreach (GameObject jet in networkJets)
        {
            // Provjeri je li to NetworkJet
            NetworkObject networkObject = jet.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned && networkObject.IsOwner)
            {
                NetworkJetShootingScript jetShooting = jet.GetComponent<NetworkJetShootingScript>();
                
                // Disable samo pucanje
                if (jetShooting != null) jetShooting.enabled = false;
            }
        }
    }

    // Ponovno omogući player/jet komponente
    private void EnablePlayerComponents()
    {
        // Pokušaj naći Multiplayer NetworkPlayer
        GameObject networkPlayer = GameObject.Find("NetworkPlayer(Clone)");
        if (networkPlayer != null)
        {
            InputManager inputManager = networkPlayer.GetComponent<InputManager>();
            NetworkAiming networkAiming = networkPlayer.GetComponent<NetworkAiming>();
            StingerScript stingerScript = networkPlayer.GetComponentInChildren<StingerScript>();
            NetworkStingerScript networkStingerScript = networkPlayer.GetComponentInChildren<NetworkStingerScript>();
            
            // Enable komponente
            if (inputManager != null) inputManager.enabled = true;
            if (networkAiming != null) networkAiming.enabled = true;
            if (stingerScript != null) stingerScript.enabled = true;
            if (networkStingerScript != null) networkStingerScript.enabled = true;
        }

        // Pokušaj naći NetworkJet
        GameObject[] networkJets = GameObject.FindGameObjectsWithTag("Jet");
        
        foreach (GameObject jet in networkJets)
        {
            // Provjeri je li to NetworkJet
            NetworkObject networkObject = jet.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned && networkObject.IsOwner)
            {
                NetworkJetShootingScript jetShooting = jet.GetComponent<NetworkJetShootingScript>();
                
                // Enable samo pucanje
                if (jetShooting != null) jetShooting.enabled = true;
            }
        }
    }

    public void OpenMenu()
    {
        settingsMenu.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Ne mijenjamo Time.timeScale, igra se nastavlja u pozadini
        isOpen = true;
        
        // Zatvori scoreboard ako je otvoren
        NetworkScoreboard.ForceCloseScoreboard();
        
        // Onemogući kretanje i pucanje
        DisablePlayerComponents();
    }

    public void CloseMenu()
    {
        settingsMenu.SetActive(false);
        
        // Provjeri je li u lobby sceni, ako je, cursor ostaje vidljiv
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "MultiplayerGameLobby")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            // Ponovno omogući kretanje i pucanje samo ako nismo u lobby sceni
            EnablePlayerComponents();
        }
        // U lobby sceni cursor ostaje vidljiv i otključan, ali komponente ostaju onemogućene
        
        isOpen = false;
    }

    public void ExitMatch()
    {
        // Označi da mi namjerno napuštamo igru
        NetworkDisconnectHandler.SetQuitting(true);

        // Zatvori network connection
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Vrati se na MultiplayerLobby
        SceneManager.LoadScene("MultiplayerLobby");
    }

    public void GoToMainMenu()
    {
        // Označi da mi namjerno napuštamo igru
        NetworkDisconnectHandler.SetQuitting(true);

        // Zatvori network connection
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Vrati se na MainMenu
        SceneManager.LoadScene("MainMenu");
    }
}