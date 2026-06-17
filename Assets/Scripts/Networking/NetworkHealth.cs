using Unity.Netcode;
using UnityEngine;

// Skripta za upravljanje health-om igrača i aviona
public class NetworkHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    
    // Event koji se poziva kada se health promijeni
    public System.Action<float, float> OnHealthChangedEvent; // (oldValue, newValue)
    public System.Action OnDeathEvent; // Poziva se kada health padne na 0
    
    // NetworkVariable za sinkronizaciju health-a preko mreže
    private NetworkVariable<float> health = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    
    // Praćenje zadnjeg napadača
    private ulong lastAttackerId = ulong.MaxValue; // MaxValue znači nema napadača

    // Javne metode za pristup health-u
    public float Health => health.Value;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? health.Value / maxHealth : 0f;
    public bool IsDead => health.Value <= 0f;
    
    // Javna metoda za dohvat lastAttackerId
    public ulong GetLastAttackerId()
    {
        return lastAttackerId;
    }
    
    // Resetiraj lastAttacker nakon respawna
    public void ResetLastAttacker()
    {
        lastAttackerId = ulong.MaxValue;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Inicijaliziraj health na maxHealth kada se spawna
        if (IsServer)
        {
            health.Value = maxHealth;
        }
        
        // Pretplati se na promjene health-a
        health.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        // Odjavi se od promjena health-a
        health.OnValueChanged -= OnHealthChanged;
    }

    // Metoda za primanje damage-a
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, ulong.MaxValue);
    }
    
    // Metoda za primanje damage-a s napadačem
    public void TakeDamage(float damage, ulong attackerId)
    {
        if (IsServer && !IsDead)
        {
            health.Value = Mathf.Max(0f, health.Value - damage);
            
            // Ako je napadač različit od vlasnika ovog objekta, zapamti ga
            if (NetworkObject != null && attackerId != ulong.MaxValue && attackerId != NetworkObject.OwnerClientId)
            {
                lastAttackerId = attackerId;
            }
            
            // Ako je health <= 0, pozovi metodu za smrt
            if (health.Value <= 0f)
            {
                OnDeath();
            }
        }
    }

    // Metoda za liječenje
    public void Heal(float healAmount)
    {
        if (IsServer && !IsDead)
        {
            health.Value = Mathf.Min(maxHealth, health.Value + healAmount);
        }
    }

    // Metoda za resetiranje health-a na maxHealth
    public void ResetHealth()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            ResetLastAttacker();
        }
    }

    // Metoda za postavljanje maxHealth-a
    public void SetMaxHealth(float newMaxHealth)
    {
        if (IsServer && newMaxHealth > 0)
        {
            maxHealth = newMaxHealth;
            // Ako je trenutni health veći od novog maxHealth, smanji ga
            if (health.Value > maxHealth)
            {
                health.Value = maxHealth;
            }
        }
    }

    // Metoda koja se poziva kada health padne na 0
    private void OnDeath()
    {
        // Pozovi event za smrt
        OnDeathEvent?.Invoke();
        
        // Ovdje ćemo kasnije dodati logiku za respawn ili game over
        Debug.Log($"{gameObject.name} has died!");
    }

    // Callback kada se health promijeni
    private void OnHealthChanged(float oldValue, float newValue)
    {
        // Pozovi event za promjenu health-a
        OnHealthChangedEvent?.Invoke(oldValue, newValue);
    }
}

