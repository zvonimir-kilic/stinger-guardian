using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// UI za READY gumb u lobby sceni
public class LobbyReadyUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private TMP_Text statusText;

    private NetworkReadyManager readyManager;
    private bool isReady = false;

    private void Start()
    {
        // Pronađi NetworkReadyManager
        readyManager = FindObjectOfType<NetworkReadyManager>();
        
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(ToggleReady);
        }

        // Ažuriraj UI
        UpdateUI();
    }

    private void Update()
    {
        // Ažuriraj status tekst
        if (statusText != null && readyManager != null && NetworkManager.Singleton != null)
        {
            int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
            
            // Koristi NetworkReadyManager da dobiješ ready count
            int readyCount = readyManager.GetReadyCount();
            
            statusText.text = $"Players: {totalPlayers}/2 | Ready: {readyCount}/2";
        }
    }

    private void ToggleReady()
    {
        if (NetworkManager.Singleton == null) return;
        
        isReady = !isReady;
        
        // Pošalji ServerRpc preko NetworkReadyManager
        if (readyManager != null)
        {
            readyManager.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, isReady);
        }
        
        // Deselektiraj gumb da se odmah vrati u normalno stanje
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Ažuriraj UI odmah
        UpdateUI();
        
        // Osiguraj da se boja ažurira i nakon što se gumb deselektira
        StartCoroutine(UpdateUIAfterFrame());
    }
    
    private System.Collections.IEnumerator UpdateUIAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = "READY";
        }

        if (readyButton != null)
        {
            // Promijeni boju gumba, crveno ako NIJE ready, zeleno ako JE ready
            var colors = readyButton.colors;
            colors.normalColor = isReady ? Color.green : Color.red;
            colors.highlightedColor = isReady ? new Color(0f, 0.8f, 0f) : new Color(0.8f, 0f, 0f); 
            colors.pressedColor = isReady ? new Color(0f, 0.6f, 0f) : new Color(0.6f, 0f, 0f);
            readyButton.colors = colors;
        }
    }
}

