using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NetworkReadyManager : NetworkBehaviour
{
    // Dictionary za praćenje ready statusa po clientId
    private Dictionary<ulong, bool> playerReadyStatus = new Dictionary<ulong, bool>();
    
    // NetworkVariable za sinkronizaciju ready count-a sa svim klijentima
    private NetworkVariable<int> readyCount = new NetworkVariable<int>(0);
    
    // Broj igrača potreban za start
    private int requiredPlayers = 2;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Pretplati se na callback-e za spajanje/odspajanje igrača
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // Dodaj Host-a u ready status
            ulong hostId = NetworkManager.Singleton.LocalClientId;
            playerReadyStatus[hostId] = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Dodaj novog igrača u dictionary
        if (IsServer)
        {
            playerReadyStatus[clientId] = false;
            UpdateReadyCount(); // Ažuriraj ready count
            Debug.Log($"Player {clientId} connected. Ready status: {playerReadyStatus.Count}/{requiredPlayers}");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Ukloni igrača iz dictionary
        if (IsServer)
        {
            playerReadyStatus.Remove(clientId);
            Debug.Log($"Player {clientId} disconnected. Remaining: {playerReadyStatus.Count}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
    {
        if (!IsServer) return;

        // Ažuriraj ready status
        if (playerReadyStatus.ContainsKey(clientId))
        {
            playerReadyStatus[clientId] = isReady;
            Debug.Log($"Player {clientId} ready status: {isReady}");
            
            // Ažuriraj ready count
            UpdateReadyCount();
            
            // Obavijesti sve klijente o promjeni
            UpdateReadyStatusClientRpc();
            
            // Provjeri jesu li svi ready
            CheckAllPlayersReady();
        }
    }
    
    private void UpdateReadyCount()
    {
        if (!IsServer) return;
        
        int count = 0;
        foreach (var status in playerReadyStatus.Values)
        {
            if (status) count++;
        }
        readyCount.Value = count;
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc()
    {
        // UI će se ažurirati automatski u LobbyReadyUI.Update()
        // Ova metoda se poziva da se osigura da svi klijenti znaju da je status promijenjen
    }

    private void CheckAllPlayersReady()
    {
        if (!IsServer) return;

        // Provjeri ima li dovoljno igrača
        if (playerReadyStatus.Count < requiredPlayers)
        {
            return;
        }

        // Provjeri jesu li svi ready
        bool allReady = true;
        foreach (var status in playerReadyStatus.Values)
        {
            if (!status)
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            Debug.Log("All players ready! Starting game in 2 seconds...");
            // Delay prije ulaska u igru da igrači vide Ready 2/2
            StartCoroutine(StartGameDelayed());
        }
    }
    
    private System.Collections.IEnumerator StartGameDelayed()
    {
        // Čekaj 2 sekunde prije ulaska u igru
        yield return new WaitForSeconds(2f);
        
        // Despawnaj sve NetworkPlayer i NetworkJet objekte iz lobby scene
        if (IsServer)
        {
            DespawnLobbyObjects();
        }
        
        // Čekaj kratko da se objekti despawnaju
        yield return new WaitForSeconds(0.5f);
        
        // Učitaj glavnu scenu
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
        }
    }
    
    private void DespawnLobbyObjects()
    {
        // Pronađi i despawnaj sve Lobby objekte
        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in allNetworkObjects)
        {
            if (obj.name.Contains("NetworkPlayerLobby") || obj.name.Contains("NetworkJetLobby"))
            {
                if (obj.IsSpawned)
                {
                    Debug.Log($"Despawning lobby object: {obj.name}");
                    obj.Despawn();
                }
            }
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // Ova metoda se više ne koristi za učitavanje scene
        // Scene se učitava direktno u StartGameDelayed() na serveru
    }

    // Public metoda za provjeru je li igrač ready
    public bool IsPlayerReady(ulong clientId)
    {
        if (!IsServer) return false;
        
        if (playerReadyStatus.ContainsKey(clientId))
        {
            return playerReadyStatus[clientId];
        }
        return false;
    }
    
    // Public metoda za dohvat ready count-a
    public int GetReadyCount()
    {
        return readyCount.Value;
    }

    // Public metoda za provjeru jesu li svi ready
    public bool AreAllPlayersReady()
    {
        if (playerReadyStatus.Count < requiredPlayers) return false;
        
        foreach (var status in playerReadyStatus.Values)
        {
            if (!status) return false;
        }
        return true;
    }
}

