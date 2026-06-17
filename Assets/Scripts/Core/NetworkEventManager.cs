using System.Collections;
using UnityEngine;
using System.Reflection;

public class NetworkEventManager : MonoBehaviour
{
    public GameObject audioManagerObject; // Referenca na AudioManager objekt
    private AudioSource[] audioSources; // Niz AudioSource komponenti

    void Start()
    {
        // Dohvati AudioSource komponente s AudioManager objekta
        if (audioManagerObject != null)
        {
            audioSources = audioManagerObject.GetComponents<AudioSource>();
            if (audioSources.Length < 2)
            {
                Debug.LogError("AudioManager nema dovoljno AudioSource komponenti!");
            }
        }
        else
        {
            Debug.LogError("AudioManager objekt nije postavljen!");
        }

        // Pokreće tijek događaja
        StartCoroutine(EventSequence());
    }

    IEnumerator EventSequence()
    {
        // 1. Prva glazba (15 sekundi)
        if (audioSources.Length > 0 && audioSources[0] != null)
        {
            audioSources[0].Play(); // Pokreni prvu glazbu
            yield return new WaitForSeconds(15f); // Čekaj 15 sekundi
            audioSources[0].Stop(); // Zaustavi prvu glazbu
        }

        // 2. Druga glazba (do kraja)
        if (audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play(); // Pokreni drugu glazbu
        }
    }
}