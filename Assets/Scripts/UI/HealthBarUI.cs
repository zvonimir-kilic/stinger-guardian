using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

// Skripta za upravljanje health bar UI-om
public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthBarFill; // Progress bar koji se mijenja
    [SerializeField] private Image healthBarBackground; // Pozadina health bara
    
    [Header("Color Settings")]
    [SerializeField] private Color fullHealthColor = Color.green; // Zelena kada je pun health
    [SerializeField] private Color mediumHealthColor = Color.yellow; // Žuta na srednjem health-u
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.5f, 0f); // Narančasta na niskom health-u
    [SerializeField] private Color criticalHealthColor = Color.red; // Crvena na kritičnom health-u
    
    [Header("Health Thresholds")]
    [SerializeField] private float mediumHealthThreshold = 0.6f; // 60% health = žuta
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30% health = narančasta
    [SerializeField] private float criticalHealthThreshold = 0.15f; // 15% health = crvena
    
    [Header("Health Bar Size")]
    [SerializeField] private float maxHealthBarWidth = 400f; // Maksimalna širina health bara
    
    private NetworkHealth networkHealth;
    private bool isInitialized = false;
    private float lastCheckTime = 0f;
    private const float checkInterval = 0.5f; // Provjeravaj svakih 0.5 sekundi dok se ne inicijalizira
    
    private RectTransform healthBarFillRect;
    private float lastHealthValue = -1f; // Za praćenje promjena health-a

    private void Start()
    {
        // Ako nije postavljeno u Inspectoru, pokušaj pronaći automatski
        if (healthBarFill == null)
        {
            healthBarFill = GetComponentInChildren<Image>();
        }
        
        // Dobavi RectTransform
        if (healthBarFill != null)
        {
            healthBarFillRect = healthBarFill.GetComponent<RectTransform>();
            
            // Postavi početnu širinu na maxHealthBarWidth
            if (healthBarFillRect != null)
            {
                healthBarFillRect.sizeDelta = new Vector2(maxHealthBarWidth, healthBarFillRect.sizeDelta.y);
            }
        }
        
        // Pokušaj pronaći NetworkHealth komponentu
        InitializeHealthReference();
    }

    private void Update()
    {
        // Provjeri je li networkHealth uništen
        if (isInitialized && networkHealth == null)
        {
            // Objekt je uništen, resetiraj i traži novi
            isInitialized = false;
            lastHealthValue = -1f;
        }
        
        // Ako još nije inicijalizirano ili je objekt uništen, pokušaj ponovo
        if (!isInitialized && Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            InitializeHealthReference();
        }
        
        // Ažuriraj health bar ako je inicijalizirano
        if (isInitialized && networkHealth != null)
        {
            UpdateHealthBar();
        }
    }

    /// Pronađi NetworkHealth komponentu na igraču ili avionu
    private void InitializeHealthReference()
    {
        // Provjeri je li NetworkManager aktivan
        if (NetworkManager.Singleton == null) return;

        // Pronađi NetworkPlayer ili NetworkJet koji pripada ovom klijentu
        NetworkHealth foundHealth = null;

        // Pokušaj pronaći NetworkPlayer
        GameObject player = GameObject.Find("NetworkPlayer(Clone)");
        if (player != null)
        {
            NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.IsSpawned && playerNetworkObject.IsOwner)
            {
                foundHealth = player.GetComponent<NetworkHealth>();
            }
        }

        // Ako nije NetworkPlayer, pokušaj NetworkJet
        if (foundHealth == null)
        {
            GameObject[] jets = GameObject.FindGameObjectsWithTag("Jet");
            foreach (GameObject jet in jets)
            {
                NetworkObject jetNetworkObject = jet.GetComponent<NetworkObject>();
                if (jetNetworkObject != null && jetNetworkObject.IsSpawned && jetNetworkObject.IsOwner)
                {
                    foundHealth = jet.GetComponent<NetworkHealth>();
                    break;
                }
            }
        }

        // Ako je pronađen, pretplati se na promjene
        if (foundHealth != null && networkHealth != foundHealth)
        {
            // Odjavi se od starog health-a ako postoji
            if (networkHealth != null)
            {
                networkHealth.OnHealthChangedEvent -= OnHealthChanged;
                networkHealth.OnDeathEvent -= OnDeath;
            }

            networkHealth = foundHealth;
            networkHealth.OnHealthChangedEvent += OnHealthChanged;
            networkHealth.OnDeathEvent += OnDeath;
            isInitialized = true;
            lastHealthValue = -1f; // Resetiraj za novi health objekt
            
            // Resetiraj UI na pun health
            ResetHealthBarToFull();
            
            // Zatim ažuriraj na stvarnu vrijednost
            UpdateHealthBar();
        }
    }
    
    /// Resetiraj health bar na punu vrijednost
    private void ResetHealthBarToFull()
    {
        if (healthBarFill == null) return;
        
        // Postavi punu širinu
        if (healthBarFillRect != null && maxHealthBarWidth > 0)
        {
            healthBarFillRect.sizeDelta = new Vector2(maxHealthBarWidth, healthBarFillRect.sizeDelta.y);
        }
        else
        {
            healthBarFill.fillAmount = 1f;
        }
        
        // Postavi zelenu boju
        healthBarFill.color = fullHealthColor;
    }

    /// Ažuriraj health bar
    private void UpdateHealthBar()
    {
        if (networkHealth == null || healthBarFill == null) return;

        float currentHealth = networkHealth.Health;
        float maxHealth = networkHealth.MaxHealth;
        float healthPercentage = networkHealth.HealthPercentage;
        
        // Provjeri je li health resetiran na maxHealth
        if (lastHealthValue >= 0f && currentHealth >= maxHealth && lastHealthValue < maxHealth)
        {
            // Health je resetiran na maxHealth - osiguraj da je boja zelena
            healthBarFill.color = fullHealthColor;
        }
        
        lastHealthValue = currentHealth;
        
        // Ažuriraj širinu health bara
        if (healthBarFillRect != null && maxHealthBarWidth > 0)
        {
            float newWidth = maxHealthBarWidth * healthPercentage;
            healthBarFillRect.sizeDelta = new Vector2(newWidth, healthBarFillRect.sizeDelta.y);
        }
        else
        {
            // Fallback na Fill Amount ako RectTransform nije dostupan
            healthBarFill.fillAmount = healthPercentage;
        }
        
        // Ažuriraj boju ovisno o health-u
        Color targetColor = GetHealthColor(healthPercentage);
        healthBarFill.color = targetColor;
    }

    /// Odredi boju ovisno o postotku health-a
    private Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage <= criticalHealthThreshold)
        {
            // Kritičan health - crvena
            return criticalHealthColor;
        }
        else if (healthPercentage <= lowHealthThreshold)
        {
            // Nizak health - narančasta
            float t = healthPercentage / lowHealthThreshold;
            return Color.Lerp(criticalHealthColor, lowHealthColor, t);
        }
        else if (healthPercentage <= mediumHealthThreshold)
        {
            // Srednji health - žuta
            float t = (healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            return Color.Lerp(lowHealthColor, mediumHealthColor, t);
        }
        else
        {
            // Visok health - zelena
            float t = (healthPercentage - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            return Color.Lerp(mediumHealthColor, fullHealthColor, t);
        }
    }

    /// Callback kada se health promijeni
    private void OnHealthChanged(float oldValue, float newValue)
    {
        UpdateHealthBar();
    }

    /// Callback kada igrač/avion umre
    private void OnDeath()
    {
        // Health bar će se automatski ažurirati na 0
        UpdateHealthBar();
    }

    private void OnDestroy()
    {
        // Odjavi se od eventa kada se objekt uništi
        if (networkHealth != null)
        {
            networkHealth.OnHealthChangedEvent -= OnHealthChanged;
            networkHealth.OnDeathEvent -= OnDeath;
        }
    }
}

