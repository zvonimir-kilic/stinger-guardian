using UnityEngine;
using UnityEngine.SceneManagement;

public class DayNightCycle : MonoBehaviour
{
    [Header("Game Duration")]
    public float gameDuration = 720f;
    
    [Header("Sun Angle Limits")]
    public float minAngle = 25f; // Izlazak sunca 
    public float maxAngle = 155f; // Prije zalaska
    
    public Light sun; // Referenca na sunce
    
    private float elapsedTime = 0f;

    void Start()
    {
        // Resetiraj timer na početku scene
        elapsedTime = 0f;
    }

    void Update()
    {
        // Vlastiti timer
        elapsedTime += Time.deltaTime;
        
        // Linearni prolaz od minAngle do maxAngle tijekom gameDuration
        // Clamp na 1 da sunce stane na maxAngle kad igra završi
        float progress = Mathf.Clamp01(elapsedTime / gameDuration);
        float angle = Mathf.Lerp(minAngle, maxAngle, progress);
        
        sun.transform.rotation = Quaternion.Euler(angle, -30f, 0f);

        UpdateLightColor(angle);
    }

    void UpdateLightColor(float angle)
    {
        if (sun == null) return;

        // Normaliziraj kut između minAngle i maxAngle na 0-1
        float normalizedAngle = (angle - minAngle) / (maxAngle - minAngle);
        
        // 0 = jutro, 0.5 = podne, 1 = popodne
        float distanceFromNoon = Mathf.Abs(normalizedAngle - 0.5f) * 2f;
        
        // Boja toplija na jutru/popodnevu, bijelija na podne
        sun.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.8f), distanceFromNoon * 0.3f);
        
        // Intenzitet jači na podne, slabiji na krajevima
        sun.intensity = Mathf.Lerp(1.2f, 0.8f, distanceFromNoon);
    }
}