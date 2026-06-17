using Unity.Netcode;
using UnityEngine;
using TMPro;

/// Prikazuje IP adresu hosta u lobby sceni
public class LobbyIPDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text ipText;

    private void Start()
    {
        UpdateIPDisplay();
    }

    private void UpdateIPDisplay()
    {
        if (ipText == null)
        {
            Debug.LogWarning("LobbyIPDisplay: IP Text is not assigned!");
            return;
        }

        // Samo host prikazuje svoju IP adresu
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            string localIP = NetworkIPHelper.GetLocalIPAddress();
            ipText.text = $"Host IP address: {localIP}";
            ipText.gameObject.SetActive(true);
        }
        else
        {
            // Klijent ne prikazuje IP adresu
            ipText.text = "";
            ipText.gameObject.SetActive(false);
        }
    }
}

