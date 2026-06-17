using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Upravlja UI-jem i omogućava pokretanje Host-a ili spajanje kao Client
public class MultiplayerLobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_InputField ipInputField;

    private const string DEFAULT_IP = "127.0.0.1";
    private const ushort DEFAULT_PORT = 7777;

    private void Start()
    {
        // Osigurava da je kursor vidljiv
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Input field ostaje prazan - placeholder tekst ce se prikazati
        // Ako korisnik ne unese nista, koristit ce se DEFAULT_IP

        UpdateStatus("Ready to connect...");
    }

    // Pokreće igru kao Host (server + client)
    public void HostGame()
    {
        UpdateStatus("Starting Host...");

        // Resetiraj flag za namjerni izlazak
        NetworkDisconnectHandler.ResetQuitting();

        if (NetworkManager.Singleton != null)
        {
            // Postavi port i ServerListenAddress na Unity Transport
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // Postavi port
                transport.ConnectionData.Port = DEFAULT_PORT;
                
                // Postavi ServerListenAddress na "0.0.0.0" da sluša na svim mrežnim interfejsima
                // Ovo omogućava klijentima s drugih računala da se spoje
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            // Pokreće Host
            NetworkManager.Singleton.StartHost();

            // Učitava MultiplayerGameLobby scenu
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameLobby", LoadSceneMode.Single);
        }
        else
        {
            UpdateStatus("Unable to start host!");
        }
    }

    // Spaja se kao Client na postojećeg Hosta
    public void JoinGame()
    {
        // Dohvati IP adresu iz input fielda, ako je prazan koristi localhost
        string targetIP = DEFAULT_IP;
        if (ipInputField != null && !string.IsNullOrWhiteSpace(ipInputField.text))
        {
            targetIP = ipInputField.text.Trim();
            
            // Validiraj IP adresu samo ako je korisnik nešto unio
            if (!NetworkIPHelper.IsValidIPAddress(targetIP))
            {
                UpdateStatus("Invalid IP address!");
                return;
            }
        }

        UpdateStatus($"Connecting to {targetIP}...");

        // Resetiraj flag za namjerni izlazak
        NetworkDisconnectHandler.ResetQuitting();

        if (NetworkManager.Singleton != null)
        {
            // Postavi IP adresu na Unity Transport
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = targetIP;
                transport.ConnectionData.Port = DEFAULT_PORT;
            }

            // Pokreće Client
            bool success = NetworkManager.Singleton.StartClient();

            if (success)
            {
                UpdateStatus("Connected! Loading game...");
                // Scena će se automatski učitati kada Host pozove LoadScene
            }
            else
            {
                UpdateStatus("Failed to connect!");
            }
        }
        else
        {
            UpdateStatus("Unable to connect!");
        }
    }

    // Vraća se na MainMenu
    public void BackToMenu()
    {
        // Označi da mi namjerno napuštamo
        NetworkDisconnectHandler.SetQuitting(true);

        // Ako je mrežna veza aktivna, shutdown prvo
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Učitava MainMenu scenu
        SceneManager.LoadScene("MainMenu");
    }

    // Ažurira tekst statusa
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}