using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Linq;

// Spawna NetworkPlayerLobby i NetworkJetLobby u lobby sceni
public class NetworkLobbySpawnManager : MonoBehaviour
{
    [Header("Lobby Prefab References")]
    [SerializeField] private GameObject networkPlayerLobbyPrefab;
    [SerializeField] private GameObject networkJetLobbyPrefab;

    [Header("Lobby Spawn Positions")]
    [SerializeField] private Vector3 playerLobbySpawnPosition = new Vector3(335f, 36f, 580f);
    [SerializeField] private Vector3 jetLobbySpawnPosition = new Vector3(320f, 36f, 560f);
    
    // Javno polje za dohvat pozicije aviona
    public Vector3 JetLobbySpawnPosition => jetLobbySpawnPosition;

    private bool playerSpawned = false;
    private bool isSpawning = false;

    private bool isInitialized = false;

    private void Start()
    {
        // Osiguraj da je kursor vidljiv i otključan za UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Osiguraj da postoji EventSystem za UI
        if (EventSystem.current == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        
        // Inicijaliziraj s delay-om da se osigura da je sve spremno
        StartCoroutine(InitializeDelayed());
    }

    private System.Collections.IEnumerator InitializeDelayed()
    {
        // Čekaj da se scena potpuno učita
        yield return new WaitForSeconds(0.1f);
        
        // Čekaj da je NetworkManager spreman
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Pretplati se na NetworkManager callback-e
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            
            // Ako je već spojen, spawnaj odmah
            if (NetworkManager.Singleton.IsHost)
            {
                yield return new WaitForSeconds(0.5f);
                SpawnPlayer();
                yield return new WaitForSeconds(0.5f);
                CheckAndSpawnJetsForExistingClients();
            }
        }
        
        isInitialized = true;
    }

    private void Update()
    {
        // Osiguraj da je kursor vidljiv i otključan za UI
        // Ovo sprječava da se cursor zaključa negdje drugdje
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MultiplayerGameLobby")
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Host spawna NetworkJet za Clienta koji se upravo spojio
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && isInitialized)
        {
            StartCoroutine(SpawnJetDelayedCoroutine(clientId));
        }
    }

    private System.Collections.IEnumerator SpawnJetDelayedCoroutine(ulong clientId)
    {
        // Čekaj da se scena potpuno učita i NetworkManager je spreman
        yield return new WaitForSeconds(0.5f);
        
        // Provjeri je li sve još uvijek spremno
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && isInitialized)
        {
            // Provjeri je li client još uvijek spojen
            if (NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
            {
                SpawnJetForSpecificClient(clientId);
            }
        }
    }

    private void CheckAndSpawnJetsForExistingClients()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost || !isInitialized) return;
        
        var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
        foreach (ulong clientId in connectedClients)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId) continue;
            
            bool alreadySpawned = false;
            NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
            foreach (var obj in allNetworkObjects)
            {
                if (obj.OwnerClientId == clientId && obj.name.Contains("NetworkJetLobby"))
                {
                    alreadySpawned = true;
                    break;
                }
            }
            
            if (!alreadySpawned)
            {
                SpawnJetForSpecificClient(clientId);
            }
        }
    }

    private void SpawnPlayer()
    {
        if (playerSpawned || isSpawning || networkPlayerLobbyPrefab == null) return;

        isSpawning = true;

        GameObject playerInstance = Instantiate(networkPlayerLobbyPrefab, playerLobbySpawnPosition, Quaternion.identity);
        
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
            
            // Postavi poziciju nakon spawnanja 
            StartCoroutine(SetPlayerPositionAfterSpawn(playerInstance, playerLobbySpawnPosition));
            
            playerSpawned = true;
            isSpawning = false;
            
            // Onemogući komponente za lobby
            DisablePlayerComponentsForLobby(playerInstance);
        }
        else
        {
            Destroy(playerInstance);
            isSpawning = false;
        }
    }

    private void SpawnJetForSpecificClient(ulong clientId)
    {
        // Provjeri je li sve spremno
        if (networkJetLobbyPrefab == null) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsServer) return;
        if (!isInitialized) return;
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        
        // Provjeri je li client još uvijek spojen
        if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
        {
            return; // Client se već odspojio
        }

        // Provjeri je li već spawnan
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in allNetworkObjects)
        {
            if (obj.OwnerClientId == clientId && obj.name.Contains("NetworkJetLobby"))
            {
                return; // Već postoji
            }
        }

        // Instantiraj na lobby poziciji
        GameObject jetInstance = Instantiate(networkJetLobbyPrefab, jetLobbySpawnPosition, Quaternion.identity);
        
        NetworkObject networkObject = jetInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            // Spawnaj s ownership-om za klijenta
            // Pozicija će se postaviti na klijentu (vlasniku) jer koristi ClientNetworkTransform
            networkObject.SpawnWithOwnership(clientId);
            
            // Onemogući komponente za lobby
            DisableJetComponentsForLobby(jetInstance);
        }
        else
        {
            Destroy(jetInstance);
        }
    }

    private void DisablePlayerComponentsForLobby(GameObject playerInstance)
    {
        // Onemogući InputManager
        InputManager inputManager = playerInstance.GetComponent<InputManager>();
        if (inputManager != null) inputManager.enabled = false;
        
        // Onemogući NetworkAiming
        NetworkAiming networkAiming = playerInstance.GetComponent<NetworkAiming>();
        if (networkAiming != null) networkAiming.enabled = false;
        
        // Onemogući pucanje
        StingerScript stingerScript = playerInstance.GetComponentInChildren<StingerScript>();
        if (stingerScript != null) stingerScript.enabled = false;
        
        NetworkStingerScript networkStingerScript = playerInstance.GetComponentInChildren<NetworkStingerScript>();
        if (networkStingerScript != null) networkStingerScript.enabled = false;
        
        // Onemogući PlayerCamera
        Camera playerCamera = playerInstance.GetComponentInChildren<Camera>();
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
            if (audioListener != null) audioListener.enabled = false;
        }
        
        // Onemogući CharacterController
        CharacterController characterController = playerInstance.GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false;
        
        // Osiguraj da je Main Camera aktivna
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
        else
        {
            // Pronađi Main Camera po tagu
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                Camera cam = mainCamObj.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;
            }
        }
        
        // Osiguraj da je kursor vidljiv i otključan za UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void DisableJetComponentsForLobby(GameObject jetInstance)
    {
        // Onemogući PlaneControllerSimple
        PlaneControllerSimple planeController = jetInstance.GetComponent<PlaneControllerSimple>();
        if (planeController != null) planeController.enabled = false;
        
        // Onemogući NetworkJetShootingScript
        NetworkJetShootingScript jetShooting = jetInstance.GetComponent<NetworkJetShootingScript>();
        if (jetShooting != null) jetShooting.enabled = false;
        
        // Onemogući JetCamera
        Camera jetCamera = jetInstance.GetComponentInChildren<Camera>();
        if (jetCamera != null)
        {
            jetCamera.enabled = false;
            AudioListener audioListener = jetCamera.GetComponent<AudioListener>();
            if (audioListener != null) audioListener.enabled = false;
        }
        
        // Osiguraj da je Main Camera aktivna
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
        }
        else
        {
            // Pronađi Main Camera po tagu
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                Camera cam = mainCamObj.GetComponent<Camera>();
                if (cam != null) cam.enabled = true;
            }
        }
        
        // Osiguraj da je kursor vidljiv i otključan za UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private System.Collections.IEnumerator SetPlayerPositionAfterSpawn(GameObject playerInstance, Vector3 position)
    {
        // Čekaj da se objekt spawna na mreži
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (playerInstance != null)
        {
            playerInstance.transform.position = position;
        }
    }

    private System.Collections.IEnumerator SetJetPositionAfterSpawn(GameObject jetInstance, Vector3 position)
    {
        // Čekaj da se objekt spawna na mreži više frame-ova za sigurnost
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (jetInstance != null)
        {
            jetInstance.transform.position = position;
            Debug.Log($"SetJetPositionAfterSpawn: Position set to {position}");
        }
        
        // Čekaj još malo i ponovo postavi za sigurnost
        yield return new WaitForSeconds(0.1f);
        
        if (jetInstance != null)
        {
            jetInstance.transform.position = position;
        }
    }
}

