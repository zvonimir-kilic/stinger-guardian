using Unity.Netcode;
using UnityEngine;
using TMPro;

// Skripta za prikaz preostalog vremena igre
public class GameTimerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text timerText; // Text element za prikaz vremena

    private NetworkGameManager gameManager;
    private float managerFoundTime = 0f;

    private void Start()
    {
        // Pokušaj pronaći NetworkGameManager
        FindGameManager();
    }

    private void Update()
    {
        // Ako još nije pronađen, pokušaj ponovo
        if (gameManager == null)
        {
            FindGameManager();
        }

        // Ažuriraj UI ako je pronađen
        if (gameManager != null && timerText != null)
        {
            UpdateTimerDisplay();
        }
    }

    /// Pronađi NetworkGameManager u sceni
    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<NetworkGameManager>();
            if (gameManager != null)
            {
                managerFoundTime = Time.time;
            }
        }
    }

    /// Ažuriraj prikaz vremena
    private void UpdateTimerDisplay()
    {
        float remainingTime = gameManager.RemainingTime;
        
        // Formatiraj vrijeme kao MM:SS
        string formattedTime = FormatTime(remainingTime);
        
        // Ažuriraj tekst
        timerText.text = formattedTime;
        
        // Promijeni boju ako je vrijeme kritično
        if (remainingTime <= 60f)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    /// Formatiraj vrijeme u MM:SS format
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}

