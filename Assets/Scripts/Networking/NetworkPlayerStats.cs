using Unity.Netcode;
using UnityEngine;

// Skripta za praćenje statistika igrača (KILL, DEATH, K/D)
public class NetworkPlayerStats : NetworkBehaviour
{
    // Event koji se poziva kada se killovi promijene
    public static System.Action<NetworkPlayerStats, int> OnKillsChangedStatic;
    
    // Event koji se poziva kada se deathovi promijene
    public static System.Action<NetworkPlayerStats, int> OnDeathsChangedStatic;

    // NetworkVariable za sinkronizaciju statistika preko mreže
    private NetworkVariable<int> kills = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> deaths = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Javne metode za pristup statistikama
    public int Kills => kills.Value;
    public int Deaths => deaths.Value;
    public float KillDeathRatio => deaths.Value > 0 ? (float)kills.Value / deaths.Value : kills.Value;

    // Metoda za dodavanje killa
    public void AddKill()
    {
        if (IsServer)
        {
            kills.Value++;
        }
    }

    // Metoda za dodavanje smrti
    public void AddDeath()
    {
        if (IsServer)
        {
            deaths.Value++;
        }
    }

    // Metoda za postavljanje statistika
    public void SetStats(int killsValue, int deathsValue)
    {
        if (IsServer)
        {
            kills.Value = killsValue;
            deaths.Value = deathsValue;
        }
    }

    // Poziva se kada se statistike promijene
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Pretplati se na promjene statistika
        kills.OnValueChanged += OnKillsChanged;
        deaths.OnValueChanged += OnDeathsChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Odjavi se od promjena statistika
        kills.OnValueChanged -= OnKillsChanged;
        deaths.OnValueChanged -= OnDeathsChanged;
    }

    private void OnKillsChanged(int oldValue, int newValue)
    {
        // Pozovi static event ako smo na serveru
        if (IsServer)
        {
            OnKillsChangedStatic?.Invoke(this, newValue);
        }
    }

    private void OnDeathsChanged(int oldValue, int newValue)
    {
        // Pozovi static event ako smo na serveru
        if (IsServer)
        {
            OnDeathsChangedStatic?.Invoke(this, newValue);
        }
    }
}
