using UnityEngine;
using Unity.Netcode;

// Osigurava da NetworkManager ostane živ kroz sve scene i da postoji samo jedan instance
public class NetworkManagerDontDestroy : MonoBehaviour
{
    private static NetworkManagerDontDestroy instance;

    private void Awake()
    {
        // Provjeri postoji li već NetworkManager instance
        if (instance != null && instance != this)
        {
            // Ako postoji već NetworkManager, uništi ovaj duplikat
            Debug.LogWarning("NetworkManager already exists! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Postavi ovaj kao instance
        instance = this;
        
        // NetworkManager ostaje aktivan kroz sve scene
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // Ako se ovaj objekt uništava i to je trenutni instance, resetiraj instance
        if (instance == this)
        {
            instance = null;
        }
    }
}

