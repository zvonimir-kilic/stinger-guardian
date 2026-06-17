using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyCounter : MonoBehaviour
{
    public static EnemyCounter Instance; // Globalni pristup

    private int enemiesDestroyed = 0;

    void Awake()
    {
        // Osiguraj da postoji samo jedna instanca
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Trajni objekt između scena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Pozovi ovu metodu kad se avion uništi
    public void RegisterEnemyDestroyed()
    {
        enemiesDestroyed++;
        
        if (enemiesDestroyed >= 3)
        {
            LoadWellDoneScene();
        }
    }

    private void LoadWellDoneScene()
    {
        SceneManager.LoadScene("WellDone");
        enemiesDestroyed = 0; // Resetiraj brojač (opcionalno)
    }
}