using System.Collections;
using UnityEngine;
using System.Reflection;

public class EventManager : MonoBehaviour
{
    public GameObject audioManagerObject; // Referenca na AudioManager objekt
    private AudioSource[] audioSources; // Niz AudioSource komponenti

    public GameObject[] airplanes; // Niz aviona
    public float[] airplaneDelays = { 5f, 5f, 5f }; // Kašnjenja za pokretanje aviona

    void Start()
    {
        // Dohvati AudioSource komponente s AudioManager objekta
        if (audioManagerObject != null)
        {
            audioSources = audioManagerObject.GetComponents<AudioSource>();
            if (audioSources.Length < 3)
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

        // 2. Alarm (9 sekundi)
        if (audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].Play(); // Pokreni alarm
            yield return new WaitForSeconds(9f); // Čekaj 9 sekundi
            audioSources[1].Stop(); // Zaustavi alarm
        }

        // 3. Druga glazba (do kraja)
        if (audioSources.Length > 2 && audioSources[2] != null)
        {
            audioSources[2].Play(); // Pokreni drugu glazbu
        }

        // 4. Pokretanje aviona s odgovarajućim kašnjenjima
        for (int i = 0; i < airplanes.Length; i++)
        {
            yield return new WaitForSeconds(airplaneDelays[i]);
            StartAirplaneMovement(airplanes[i]);
        }
    }

    void StartAirplaneMovement(GameObject airplane)
    {
        // Dohvati sve komponente na avionu
        MonoBehaviour[] scripts = airplane.GetComponents<MonoBehaviour>();

        foreach (var script in scripts)
        {
            // Provjeri ima li skripta metodu "UpdatePositionAndRotation"
            System.Type scriptType = script.GetType();
            MethodInfo method = scriptType.GetMethod("UpdatePositionAndRotation");

            if (method != null)
            {
                // Pozovi metodu s novom pozicijom
                Vector3 newPosition = CalculateNewPosition(airplane); // Ovdje izračunaj novu poziciju
                method.Invoke(script, new object[] { newPosition });
                break; // Prekini petlju nakon pronalaska metode
            }
        }
    }

    Vector3 CalculateNewPosition(GameObject airplane)
    {
        // Pomakni avion za neki offset
        return airplane.transform.position + new Vector3(100f, 0f, 0f);
    }
}