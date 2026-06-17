using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NetworkScoreboard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject scoreboardPanel;
    [SerializeField] private Transform contentParent; // Parent gdje će se dodavati redovi igrača
    [SerializeField] private GameObject playerRowPrefab; // Prefab za red igrača

    [Header("Header Text")]
    [SerializeField] private TMP_Text headerText;

    private List<GameObject> playerRows = new List<GameObject>();

    private void Start()
    {
        // Ako scoreboardPanel nije postavljen, pokušaj pronaći
        if (scoreboardPanel == null)
        {
            // Pokušaj pronaći kroz ScoreboardCanvas
            Transform scoreboardCanvas = transform.Find("ScoreboardCanvas");
            if (scoreboardCanvas != null)
            {
                scoreboardPanel = scoreboardCanvas.Find("ScoreboardPanel")?.gameObject;
            }

            if (scoreboardPanel == null)
            {
                scoreboardPanel = transform.Find("ScoreboardPanel")?.gameObject;
            }
        }

        // Ako contentParent nije postavljen, pokušaj pronaći
        if (contentParent == null)
        {
            if (scoreboardPanel != null)
            {
                contentParent = scoreboardPanel.transform.Find("Content");
            }

            if (contentParent == null)
            {
                Transform scoreboardCanvas = transform.Find("ScoreboardCanvas");
                if (scoreboardCanvas != null)
                {
                    contentParent = scoreboardCanvas.Find("ScoreboardPanel/Content");
                }
            }
        }

        // Smanji spacing u VerticalLayoutGroup na Content objektu ako postoji
        if (contentParent != null)
        {
            VerticalLayoutGroup verticalLayout = contentParent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                verticalLayout.spacing = 0;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.padding = new RectOffset(0, 0, 0, 0);
            }
        }

        // Sakrij scoreboard na početku
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Ne prikazuj scoreboard ako je SettingsMenu otvoren
        if (SettingsMenu.isOpen) return;

        // Provjeri je li Tab tipka pritisnuta
        if (Input.GetKey(KeyCode.Tab))
        {
            ShowScoreboard();
        }
        else
        {
            HideScoreboard();
        }
    }

    private void ShowScoreboard()
    {
        if (scoreboardPanel == null) return;

        if (!scoreboardPanel.activeSelf)
        {
            scoreboardPanel.SetActive(true);
        }

        // Ažuriraj podatke
        UpdateScoreboard();
    }

    private void HideScoreboard()
    {
        if (scoreboardPanel != null && scoreboardPanel.activeSelf)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    // Public metoda za zatvaranje scoreboarda iz drugih skripti
    public static void ForceCloseScoreboard()
    {
        NetworkScoreboard scoreboard = FindObjectOfType<NetworkScoreboard>();
        if (scoreboard != null && scoreboard.scoreboardPanel != null && scoreboard.scoreboardPanel.activeSelf)
        {
            scoreboard.scoreboardPanel.SetActive(false);
        }
    }

    private void UpdateScoreboard()
    {
        if (contentParent == null) return;

        // Pronađi sve NetworkPlayerStats komponente
        NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
        
        // Očisti postojeće redove ako postoje
        ClearPlayerRows();

        // Kreiraj red za svakog igrača
        foreach (var stats in allStats)
        {
            NetworkObject obj = stats.GetComponent<NetworkObject>();
            if (obj != null && obj.IsSpawned)
            {
                CreatePlayerRow(stats, obj.OwnerClientId);
            }
        }
    }

    private void CreatePlayerRow(NetworkPlayerStats stats, ulong playerId)
    {
        // Odredi tip igrača
        string playerType = "Unknown";
        if (stats.GetComponent<NetworkPlayerController>() != null)
        {
            playerType = "NetworkPlayer";
        }
        else if (stats.GetComponent<NetworkJetController>() != null)
        {
            playerType = "NetworkJet";
        }

        // Ako postoji prefab, koristi ga, inače kreiraj dinamički
        GameObject row;
        if (playerRowPrefab != null)
        {
            row = Instantiate(playerRowPrefab, contentParent);
        }
        else
        {
            // Kreiraj dinamički red
            row = new GameObject($"PlayerRow_{playerId}");
            row.transform.SetParent(contentParent, false);
            
            // Dodaj RectTransform
            RectTransform rectTransform = row.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, 10);
            rectTransform.anchoredPosition = Vector2.zero;

            // Koristi eksplicitno pozicioniranje umjesto HorizontalLayoutGroup
            // Kreiraj TextMeshPro elemente za svaki stupac s eksplicitnim pozicioniranjem
            float xOffset = 0f; // Početni offset
            CreateTextElementAtPosition(row, playerType, 100, xOffset);
            xOffset += 41f; // 41 + 5 spacing
            CreateTextElementAtPosition(row, playerId.ToString(), 30, xOffset);
            xOffset += 24f; // 19 + 5 spacing
            CreateTextElementAtPosition(row, stats.Kills.ToString(), 30, xOffset);
            xOffset += 21f; // 16 + 5 spacing
            CreateTextElementAtPosition(row, stats.Deaths.ToString(), 30, xOffset);
            xOffset += 23f; // 18 + 5 spacing
            CreateTextElementAtPosition(row, stats.KillDeathRatio.ToString("F2"), 50, xOffset);
        }

        // Ažuriraj tekst ako postoji prefab s TextMeshPro komponentama
        UpdateRowText(row, playerType, playerId, stats);

        playerRows.Add(row);
    }

    private void CreateTextElement(GameObject parent, string text, float width)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        // Koristi left anchor za horizontalno pozicioniranje
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, 10);
        rectTransform.anchoredPosition = Vector2.zero;

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 4;
        tmpText.color = Color.black; 
        tmpText.alignment = TextAlignmentOptions.Left;
        tmpText.enableWordWrapping = false;
        tmpText.overflowMode = TextOverflowModes.Overflow;
        
        // Osiguraj da je tekst vidljiv
        tmpText.raycastTarget = false;
    }

    private void CreateTextElementAtPosition(GameObject parent, string text, float width, float xPosition)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        // Koristi left anchor za horizontalno pozicioniranje
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, 10);
        rectTransform.anchoredPosition = new Vector2(xPosition, 0); // Eksplicitna X pozicija

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 4;
        tmpText.color = Color.black; 
        tmpText.alignment = TextAlignmentOptions.Left;
        tmpText.enableWordWrapping = false;
        tmpText.overflowMode = TextOverflowModes.Overflow;
        
        // Osiguraj da je tekst vidljiv
        tmpText.raycastTarget = false;
    }

    private void UpdateRowText(GameObject row, string playerType, ulong playerId, NetworkPlayerStats stats)
    {
        // Ako prefab već ima TextMeshPro komponente, ažuriraj ih
        TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 5)
        {
            texts[0].text = playerType;
            texts[1].text = playerId.ToString();
            texts[2].text = stats.Kills.ToString();
            texts[3].text = stats.Deaths.ToString();
            texts[4].text = stats.KillDeathRatio.ToString("F2");
            
            // Postavi crnu boju i manji font za sve tekstove
            foreach (var text in texts)
            {
                text.color = Color.black;
                text.fontSize = 4;
            }
        }
    }

    private void ClearPlayerRows()
    {
        foreach (var row in playerRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }
        playerRows.Clear();
    }
}

