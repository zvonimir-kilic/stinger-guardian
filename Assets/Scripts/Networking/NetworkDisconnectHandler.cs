using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Upravlja događajima disconnecta i preusmjerava igrače na temelju trenutne scene
public class NetworkDisconnectHandler : MonoBehaviour
{
    private static NetworkDisconnectHandler instance;
    private bool isSubscribed = false;
    
    // Flag koji označava da mi namjerno napuštamo igru
    // Postavlja se kada igrač klikne Exit ili zatvori prozor
    public static bool IsQuitting { get; private set; } = false;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SubscribeToNetworkEvents();
    }

    private void Update()
    {
        // Kontinuirano pokušavaj pretplatiti se ako još nismo
        if (!isSubscribed)
        {
            SubscribeToNetworkEvents();
        }
    }

    private void SubscribeToNetworkEvents()
    {
        if (NetworkManager.Singleton != null && !isSubscribed)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            isSubscribed = true;
            Debug.Log("NetworkDisconnectHandler: Subscribed to disconnect events");
        }
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Singleton != null && isSubscribed)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            isSubscribed = false;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnsubscribeFromNetworkEvents();
    }

    private void OnApplicationQuit()
    {
        // Kada se aplikacija zatvara, postavi flag da mi odlazimo
        IsQuitting = true;
    }

    /// Javna metoda za označavanje namjernog izlaska
    public static void SetQuitting(bool quitting)
    {
        IsQuitting = quitting;
        Debug.Log($"NetworkDisconnectHandler: IsQuitting set to {quitting}");
    }

    /// Reset flag
    public static void ResetQuitting()
    {
        IsQuitting = false;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Ako mi namjerno napuštamo igru, ne reagiraj
        if (IsQuitting)
        {
            Debug.Log($"Client {clientId} disconnected, but we are quitting. Ignoring.");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        Debug.Log($"Client {clientId} disconnected. Scene: {currentScene}, IsServer: {isServer}, IsHost: {isHost}, LocalClientId: {localClientId}");

        // Provjeri je li to lokalni klijent koji se disconnectao
        bool isLocalDisconnect = (clientId == localClientId);

        if (currentScene == "MultiplayerGameLobby")
        {
            HandleLobbyDisconnect(clientId, isServer, isHost, isLocalDisconnect);
        }
        else if (currentScene == "MultiplayerScene")
        {
            HandleGameDisconnect(clientId, isServer, isHost, isLocalDisconnect);
        }
    }

    /// Handle disconnect u MultiplayerGameLobby sceni
    private void HandleLobbyDisconnect(ulong clientId, bool isServer, bool isHost, bool isLocalDisconnect)
    {
        // Ako je klijent disconnectan, ništa se ne događa
        // Host može čekati novog klijenta

        // Ako je ovo klijent i host se disconnectao, vrati na MultiplayerLobby
        if (!isServer && isLocalDisconnect)
        {
            Debug.Log("Host disconnected in lobby. Returning to MultiplayerLobby...");
            ReturnToLobby();
        }
        // Ako smo server i klijent se disconnectao, čekamo novog
        else if (isServer && !isLocalDisconnect)
        {
            Debug.Log("Client disconnected in lobby. Waiting for new client...");
            // Ništa ne radimo, host čeka novog klijenta
        }
    }

    /// Handle disconnect u MultiplayerScene sceni
    private void HandleGameDisconnect(ulong clientId, bool isServer, bool isHost, bool isLocalDisconnect)
    {
        // Ako je ovo klijent i host se disconnectao, klijent pobjeđuje
        if (!isServer && isLocalDisconnect)
        {
            Debug.Log("Host disconnected during game. Client wins!");
            GoToWinScene();
        }
        // Ako smo server i klijent se disconnectao, host pobjeđuje
        else if (isServer && !isLocalDisconnect)
        {
            Debug.Log("Client disconnected during game. Host wins!");
            GoToWinScene();
        }
    }

    /// Vrati igrača na MultiplayerLobby
    private void ReturnToLobby()
    {
        // Zatvori mrežnu vezu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Čekaj kratko pa učitaj scenu
        StartCoroutine(LoadSceneAfterDelay("MultiplayerLobby", 0.1f));
    }

    /// Prebaci igrača na YouWin scenu
    private void GoToWinScene()
    {
        // Zatvori mrežnu vezu
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Čekaj kratko pa učitaj scenu
        StartCoroutine(LoadSceneAfterDelay("YouWin", 0.1f));
    }

    private System.Collections.IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}

