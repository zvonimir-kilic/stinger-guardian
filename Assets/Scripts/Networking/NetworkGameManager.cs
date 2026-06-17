using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float timeLimit = 720f; // 12 minuta u sekundama
    [SerializeField] private int killLimit = 20; // Broj killova potreban za pobjedu
    [SerializeField] private float startDelay = 3f; // Delay prije početka odbrojavanja

    // NetworkVariable za sinkronizaciju preostalog vremena
    private NetworkVariable<float> remainingTime = new NetworkVariable<float>(
        1200f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // NetworkVariable za sinkronizaciju je li igra počela
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // NetworkVariable za sinkronizaciju Golden Kill moda
    private NetworkVariable<bool> goldenKillMode = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float gameStartTime;
    private bool gameEnded = false;

    // Javna svojstva za pristup vremenu
    public float RemainingTime => remainingTime.Value;
    public float TimeLimit => timeLimit;
    public bool GameStarted => gameStarted.Value;
    public float StartDelay => startDelay;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            gameStartTime = Time.time;
            remainingTime.Value = timeLimit; // Inicijaliziraj na timeLimit
            gameStarted.Value = false; // Igra još nije počela
            
            // Očisti stare event handlere
            NetworkPlayerStats.OnKillsChangedStatic -= OnPlayerKillsChanged;
            NetworkPlayerStats.OnDeathsChangedStatic -= OnPlayerDeathsChanged;
            
            // Pretplati se na promjene killova i deathova
            NetworkPlayerStats.OnKillsChangedStatic += OnPlayerKillsChanged;
            NetworkPlayerStats.OnDeathsChangedStatic += OnPlayerDeathsChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer)
        {
            // Odjavi se od promjena killova i deathova
            NetworkPlayerStats.OnKillsChangedStatic -= OnPlayerKillsChanged;
            NetworkPlayerStats.OnDeathsChangedStatic -= OnPlayerDeathsChanged;
        }
    }

    private void OnDestroy()
    {
        // Eksplicitni cleanup static evenata
        NetworkPlayerStats.OnKillsChangedStatic -= OnPlayerKillsChanged;
        NetworkPlayerStats.OnDeathsChangedStatic -= OnPlayerDeathsChanged;
    }

    private void Update()
    {
        // Samo server provjerava uvjete za završetak igre
        if (!IsServer || gameEnded) return;

        float elapsedTime = Time.time - gameStartTime;
        
        // Provjeri je li prošao start delay
        if (!gameStarted.Value && elapsedTime >= startDelay)
        {
            gameStarted.Value = true; // Igra je počela
        }

        // Ako igra još nije počela, ne odbrojavaj
        if (!gameStarted.Value)
        {
            remainingTime.Value = timeLimit; // Zadrži puno vrijeme dok ne počne
            return;
        }

        // Ažuriraj preostalo vrijeme
        float gameElapsedTime = elapsedTime - startDelay;
        float newRemainingTime = Mathf.Max(0f, timeLimit - gameElapsedTime);
        remainingTime.Value = newRemainingTime;

        // Provjeri vremenski limit
        if (newRemainingTime <= 0f && !goldenKillMode.Value)
        {
            EndGame("Time limit reached!");
            return;
        }
    }

    private void OnPlayerKillsChanged(NetworkPlayerStats stats, int newKills)
    {
        // Ako je Golden Kill mod aktivan, prvi kill pobjeđuje
        if (goldenKillMode.Value && !gameEnded)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned)
            {
                ulong winnerId = obj.OwnerClientId;
                EndGame("Golden Kill!", winnerId);
                return;
            }
        }

        // Provjeri je li igrač dosegao kill limit
        if (newKills >= killLimit && !gameEnded)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned)
            {
                // Ako netko dosegne kill limit, on je pobjednik
                ulong winnerId = obj.OwnerClientId;
                EndGame($"Player reached {killLimit} kills!", winnerId);
            }
        }
    }

    private void OnPlayerDeathsChanged(NetworkPlayerStats statsOfPlayerWhoDied, int newDeaths)
    {
        // Ako je Golden Kill mod aktivan i netko se sam ubije, drugi igrač pobjeđuje
        if (goldenKillMode.Value && !gameEnded)
        {
            // Pronađi ID igrača koji je umro
            NetworkObject deadPlayerObj = statsOfPlayerWhoDied.GetComponent<NetworkObject>();
            if (deadPlayerObj == null || !deadPlayerObj.IsSpawned) return;
            
            ulong deadPlayerId = deadPlayerObj.OwnerClientId;
            
            // Pronađi drugog igrača, odnosno pobjednika
            NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
            foreach (var stats in allStats)
            {
                NetworkObject obj = stats.GetComponent<NetworkObject>();
                if (obj != null && obj.IsSpawned && obj.OwnerClientId != deadPlayerId)
                {
                    // Ovaj igrač je pobjednik jer je drugi umro
                    ulong winnerId = obj.OwnerClientId;
                    EndGame("Golden Kill - Opponent died!", winnerId);
                    return;
                }
            }
        }
    }

    /// Odredi pobjednika na temelju statistika ili aktiviraj Golden Kill mod
    private ulong? DetermineWinnerOrActivateGoldenKill()
    {
        NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
        NetworkPlayerStats bestPlayer = null;
        int bestKills = -1;
        int bestDeaths = int.MaxValue;
        int playersWithBestStats = 0;

        foreach (var stats in allStats)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned)
            {
                int kills = stats.Kills;
                int deaths = stats.Deaths;
                ulong clientId = obj.OwnerClientId;
                
                // Ako ima više killova, on je bolji
                if (kills > bestKills)
                {
                    bestKills = kills;
                    bestDeaths = deaths;
                    bestPlayer = stats;
                    playersWithBestStats = 1;
                }
                // Ako ima jednako killova, onaj s manje smrti je bolji
                else if (kills == bestKills && deaths < bestDeaths)
                {
                    bestDeaths = deaths;
                    bestPlayer = stats;
                    playersWithBestStats = 1;
                }
                // Ako ima jednako killova i jednako smrti, imamo izjednačenje
                else if (kills == bestKills && deaths == bestDeaths)
                {
                    playersWithBestStats++;
                }
            }
        }

        // Ako imaju istu statistiku, aktiviraj Golden Kill mod
        if (playersWithBestStats > 1)
        {
            goldenKillMode.Value = true;
            Debug.Log("Tie detected! Golden Kill mode activated - first kill wins!");
            ActivateGoldenKillClientRpc();
            return null; // Nema pobjednika još, čeka se Golden Kill
        }

        // Ako postoji jasni pobjednik, vrati njegov ID
        if (bestPlayer != null)
        {
            NetworkObject winnerObj = bestPlayer.GetComponent<NetworkObject>();
            if (winnerObj != null && winnerObj.IsSpawned)
            {
                ulong winnerId = winnerObj.OwnerClientId;
                return winnerId;
            }
        }

        // Nema igrača pronađenih, što se ne bi trebalo dogoditi
        return null;
    }

    [ClientRpc]
    private void ActivateGoldenKillClientRpc()
    {
        // Dodati UI poruku "GOLDEN KILL - First Kill Wins!"
        Debug.Log("GOLDEN KILL MODE: First kill wins!");
    }

    private void EndGame(string reason, ulong? winnerIdFromKillLimit = null)
    {
        if (gameEnded) return;

        // Odredi pobjednika
        ulong? winnerId = null;
        if (winnerIdFromKillLimit.HasValue)
        {
            // Ako je netko dosegao kill limit, on je pobjednik
            winnerId = winnerIdFromKillLimit.Value;
        }
        else
        {
            // Ako je vrijeme isteklo, odredi pobjednika na temelju statistika
            // Ili aktiviraj Golden Kill mod ako imaju istu statistiku
            winnerId = DetermineWinnerOrActivateGoldenKill();
        }

        // Ako je Golden Kill mod aktiviran, ne završavaj igru još
        if (winnerId == null)
        {
            Debug.Log($"Game continues - Golden Kill mode activated!");
            return; // Igra se nastavlja, čeka se prvi kill
        }

        // Završi igru s određenim pobjednikom
        gameEnded = true;
        Debug.Log($"Game ended: {reason}");
        Debug.Log($"Winner determined: ClientId {winnerId.Value}");

        // Server prvo despawna sve NetworkObject-e prije nego što klijenti učitaju scene
        if (IsServer)
        {
            DespawnAllNetworkObjects();
            // Pošalji ClientRpc nakon kratkog delay-a da se objekti despawnaju
            StartCoroutine(EndGameDelayed(winnerId.Value));
        }
    }

    /// Despawnaj sve NetworkObject-e u sceni
    private void DespawnAllNetworkObjects()
    {
        if (!IsServer) return;

        NetworkObject[] allNetworkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in allNetworkObjects)
        {
            if (obj != null && obj.IsSpawned && obj != NetworkObject)
            {
                Debug.Log($"Despawning network object: {obj.name}");
                obj.Despawn();
            }
        }
    }

    /// Korutina za delay prije slanja ClientRpc
    private System.Collections.IEnumerator EndGameDelayed(ulong winnerId)
    {
        // Čekaj da se objekti despawnaju
        yield return new WaitForSeconds(1f);
        
        // Pošalji personalizirani RPC svakom spojenom klijentu
        // Server računa isWinner za svakog klijenta i šalje ispravnu vrijednost
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool isWinner = (clientId == winnerId);
            Debug.Log($"Sending EndGame to ClientId={clientId}, isWinner={isWinner}, winnerId={winnerId}");
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            
            NotifyGameEndClientRpc(isWinner, clientRpcParams);
        }
    }

    [ClientRpc]
    private void NotifyGameEndClientRpc(bool isWinner, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"NotifyGameEndClientRpc received: isWinner={isWinner}");
        
        // ODMAH označi da igra završava, tako da disconnect handler ne reagira
        // Ovo mora biti PRIJE čekanja, inače Host može zatvoriti vezu prije nego postavimo flag
        NetworkDisconnectHandler.SetQuitting(true);
        
        // Pokreni korutinu koja čeka prije učitavanja scene
        StartCoroutine(LoadSceneDelayed(isWinner));
    }

    /// Korutina koja čeka prije učitavanja scene da se osigura da su objekti despawnani
    private System.Collections.IEnumerator LoadSceneDelayed(bool isWinner)
    {
        // Čekaj dodatno da se osigura da su objekti despawnani
        yield return new WaitForSeconds(0.5f);

        // Učitaj odgovarajuću scenu
        string sceneToLoad = isWinner ? "YouWin" : "YouLose";
        Debug.Log($"Loading scene: {sceneToLoad} (IsWinner: {isWinner})");
        
        // Zatvori mrežnu vezu prije učitavanja scene
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // Čekaj kratko da se mreža zatvori
        yield return new WaitForSeconds(0.1f);
        
        // Učitaj scenu lokalno nakon što su objekti despawnani i mreža zatvorena
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}