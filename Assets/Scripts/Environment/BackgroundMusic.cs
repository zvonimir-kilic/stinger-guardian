using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;
    
    // Scene gdje glazba TREBA svirati
    private string[] allowedScenes = { "MainMenu", "MultiplayerLobby", "MultiplayerGameLobby" };
    
    void Awake()
    {
        // Osiguraj da postoji samo jedna instanca
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Dohvati AudioSource komponentu
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Postavi glazbu da svira u loopu
        audioSource.loop = true;
        
        // Pretplati se na promjene scena
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Provjeri trenutnu scenu
        CheckCurrentScene();
    }
    
    void OnDestroy()
    {
        // Odjavi se od eventa
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckCurrentScene();
    }
    
    private void CheckCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool shouldPlay = IsSceneAllowed(currentScene);
        
        if (shouldPlay)
        {
            // Ako glazba treba svirati i nije već svirala, pokreni je
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // Ako glazba ne treba svirati, zaustavi je
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
    
    private bool IsSceneAllowed(string sceneName)
    {
        foreach (string allowedScene in allowedScenes)
        {
            if (sceneName == allowedScene)
            {
                return true;
            }
        }
        return false;
    }
}

