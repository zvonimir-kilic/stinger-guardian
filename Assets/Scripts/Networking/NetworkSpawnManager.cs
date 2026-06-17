using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// Eksplicitno spawna NetworkPlayer i NetworkJet ovisno o tome je li Host ili Client
public class NetworkSpawnManager : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject networkPlayerPrefab;
    [SerializeField] private GameObject networkJetPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(5605f, 36f, -80f);
    [SerializeField] private Vector3 jetSpawnPosition = new Vector3(6155f, 35f, -300f);

    private bool playerSpawned = false; // Provjera je li NetworkPlayer spawnan
    private bool isSpawning = false; // Sprječava višestruko spawnanje
    private bool hasCheckedJets = false; // Sprječava višestruko provjeravanje

    // Perzistentne statistike po ownerId
    private Dictionary<ulong, PlayerStatsData> persistentStats = new Dictionary<ulong, PlayerStatsData>();

    // Struktura za čuvanje statistika
    private struct PlayerStatsData
    {
        public int kills;
        public int deaths;
    }

    private void Start()
    {
        // Pretplati se na NetworkManager callback-e
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Ako je već spojen (Host), spawnaj odmah
            if (NetworkManager.Singleton.IsHost)
            {
                // Mali delay da se scena učita
                Invoke(nameof(SpawnPlayer), 0.5f);
                
                // Provjeri već spojene klijente i spawnaj NetworkJet za njih
                // Povećaj delay da se osigura da je scena potpuno učitana
                Invoke(nameof(CheckAndSpawnJetsForExistingClients), 1.5f);
            }
        }
    }

    /// Provjeri i spawnaj NetworkJet za već spojene klijente (ako postoje)
    private void CheckAndSpawnJetsForExistingClients()
    {
        if (!NetworkManager.Singleton.IsHost || !NetworkManager.Singleton.IsServer) return;
        
        var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
        Debug.Log($"CheckAndSpawnJetsForExistingClients: Found {connectedClients.Count} connected clients");
        
        foreach (ulong clientId in connectedClients)
        {
            // Preskoči Host-a (već ima NetworkPlayer)
            if (clientId == NetworkManager.Singleton.LocalClientId)
                continue;
            
            // Provjeri je li već spawnan NetworkJet za ovog klijenta
            bool alreadySpawned = false;
            NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
            foreach (var obj in allNetworkObjects)
            {
                if (obj.OwnerClientId == clientId && obj.name.Contains("NetworkJet"))
                {
                    alreadySpawned = true;
                    Debug.Log($"NetworkJet already spawned for client {clientId}");
                    break;
                }
            }
            
            if (!alreadySpawned)
            {
                Debug.Log($"Spawning NetworkJet for client {clientId}");
                SpawnJetForSpecificClient(clientId);
            }
        }
    }

    private void Update()
    {
        // Provjeri je li potrebno spawnati NetworkJet za već spojene klijente
        // Ovo osigurava da se NetworkJet spawna i kada se scena učitava iz lobby scene
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsServer)
        {
            // Provjeri samo jednom nakon što se scena učita
            if (Time.time > 2f && playerSpawned && !hasCheckedJets)
            {
                hasCheckedJets = true;
                Debug.Log("Update: Checking for existing clients to spawn NetworkJet");
                CheckAndSpawnJetsForExistingClients();
            }
        }
    }

    private void OnDestroy()
    {
        // Odjavi se od callback-a
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Host spawna NetworkJet za Clienta koji se upravo spojio
        if (NetworkManager.Singleton.IsHost)
        {
            // Spawnaj NetworkJet za Clienta koji se upravo spojio
            // Mali delay da se scena učita
            StartCoroutine(SpawnJetDelayed(clientId));
        }
    }

    /// Korutina za delay prije spawnanja NetworkJet-a
    private System.Collections.IEnumerator SpawnJetDelayed(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);
        SpawnJetForSpecificClient(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Otkaži sve invoke pozive kada se klijent odspoji
        CancelInvoke(nameof(SpawnPlayer));
        CancelInvoke(nameof(CheckAndSpawnJetsForExistingClients));
    }

    private void SpawnPlayer()
    {
        if (playerSpawned || isSpawning || networkPlayerPrefab == null) return;

        // Postavi flag da je u procesu spawnanja
        isSpawning = true;

        // Instanciraj prefab
        GameObject playerInstance = Instantiate(networkPlayerPrefab, playerSpawnPosition, Quaternion.identity);
        
        // Spawnaj na mreži
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
            playerInstance.transform.position = playerSpawnPosition;
            playerSpawned = true;
            isSpawning = false;
        }
        else
        {
            Destroy(playerInstance);
            isSpawning = false;
        }
    }

    /// Spawnaj NetworkJet za specifičnog klijenta
    private void SpawnJetForSpecificClient(ulong clientId)
    {
        if (networkJetPrefab == null) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsServer) return;

        // Preskoči Host-a (već ima NetworkPlayer)
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        // Provjeri je li već spawnan NetworkJet za ovog klijenta
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in allNetworkObjects)
        {
            if (obj.OwnerClientId == clientId && obj.name.Contains("NetworkJet"))
            {
                return; // Već postoji
            }
        }

        // Instanciraj prefab
        GameObject jetInstance = Instantiate(networkJetPrefab, jetSpawnPosition, Quaternion.identity);

        // Spawnaj na mreži s ownership-om za Clienta
        NetworkObject networkObject = jetInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(clientId);
            jetInstance.transform.position = jetSpawnPosition;
        }
        else
        {
            Destroy(jetInstance);
        }
    }

    /// Respawnaj NetworkPlayer na početnoj poziciji
    public void RespawnPlayer(ulong ownerId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Sačuvaj statistike prije respawna
        SaveStatsBeforeRespawn(ownerId);

        // Resetiraj flag da se može respawnati
        playerSpawned = false;

        // Instanciraj prefab
        GameObject playerInstance = Instantiate(networkPlayerPrefab, playerSpawnPosition, Quaternion.identity);
        
        // Spawnaj na mreži
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(ownerId);
            playerInstance.transform.position = playerSpawnPosition;
            playerSpawned = true;

            // Vrati statistike nakon respawna
            RestoreStatsAfterRespawn(ownerId, playerInstance);
        }
        else
        {
            Destroy(playerInstance);
        }
    }

    /// Respawnaj NetworkJet na početnoj poziciji
    public void RespawnJet(ulong ownerId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Sačuvaj statistike prije respawna
        SaveStatsBeforeRespawn(ownerId);

        // Instanciraj prefab
        GameObject jetInstance = Instantiate(networkJetPrefab, jetSpawnPosition, Quaternion.identity);
        
        // Spawnaj na mreži s ownership-om
        NetworkObject networkObject = jetInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.SpawnWithOwnership(ownerId);
            jetInstance.transform.position = jetSpawnPosition;

            // Vrati statistike nakon respawna
            RestoreStatsAfterRespawn(ownerId, jetInstance);
        }
        else
        {
            Destroy(jetInstance);
        }
    }

    /// Sačuvaj statistike prije respawna
    private void SaveStatsBeforeRespawn(ulong ownerId)
    {
        // Pronađi postojeći objekt i sačuvaj statistike
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in allNetworkObjects)
        {
            if (obj.OwnerClientId == ownerId)
            {
                NetworkPlayerStats stats = obj.GetComponent<NetworkPlayerStats>();
                if (stats != null)
                {
                    persistentStats[ownerId] = new PlayerStatsData
                    {
                        kills = stats.Kills,
                        deaths = stats.Deaths
                    };
                    break;
                }
            }
        }
    }

    /// Vrati statistike nakon respawna
    private void RestoreStatsAfterRespawn(ulong ownerId, GameObject instance)
    {
        // Sačekaj malo da se objekt spawna
        StartCoroutine(RestoreStatsDelayed(ownerId, instance));
    }

    private System.Collections.IEnumerator RestoreStatsDelayed(ulong ownerId, GameObject instance)
    {
        yield return new WaitForSeconds(0.1f);

        if (instance == null) yield break;

        // Resetiraj health na maxHealth
        NetworkHealth health = instance.GetComponent<NetworkHealth>();
        if (health != null && NetworkManager.Singleton.IsServer)
        {
            health.ResetHealth();
        }

        NetworkPlayerStats stats = instance.GetComponent<NetworkPlayerStats>();
        if (stats != null && persistentStats.ContainsKey(ownerId))
        {
            PlayerStatsData data = persistentStats[ownerId];
            // Postavi statistike direktno
            if (NetworkManager.Singleton.IsServer)
            {
                stats.SetStats(data.kills, data.deaths);
            }
        }
    }

    /// Ispiši sve statistike svih igrača
    public static void LogAllPlayerStats()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        NetworkPlayerStats[] allStats = Object.FindObjectsOfType<NetworkPlayerStats>();
        foreach (var stats in allStats)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned)
            {
                string playerType = "Unknown";
                ulong playerId = obj.OwnerClientId;

                // Odredi tip igrača
                if (stats.GetComponent<NetworkPlayerController>() != null)
                {
                    playerType = "NetworkPlayer";
                }
                else if (stats.GetComponent<NetworkJetController>() != null)
                {
                    playerType = "NetworkJet";
                }

                // Ispiši statistike
                Debug.Log($"{playerType} ({playerId}) KILL: {stats.Kills} DEATH: {stats.Deaths} K/D: {stats.KillDeathRatio:F2}");
            }
        }
    }
}


