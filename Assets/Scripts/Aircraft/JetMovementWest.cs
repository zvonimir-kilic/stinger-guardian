using UnityEngine;

public class WesternJet : MonoBehaviour
{
    public float baseSpeed = 0.06f;
    public float yAmplitude = 250f;
    public float radiusX = 1000f;
    public float radiusZ = 400f;
    public Transform player;
    public float slowdownRadius = 400f;
    public float slowdownSpeed = 0.02f;
    public AudioSource airplaneAudioSource;
    public float soundActivationRadius = 650f;
    
    private float t = 0f;
    private bool hasPlayed = false;
    private bool hasStartedMoving = false; // Dodajemo varijablu za praćenje početka kretanja

    void Start()
    {
        // Pozovi metodu StartMoving nakon 25 sekundi
        Invoke("StartMoving", 25f);
    }

    void Update()
    {
        // Ako se avion još nije počeo kretati, nemoj raditi ništa
        if (!hasStartedMoving) return;

        if (player == null) return;

        float currentSpeed = Vector3.Distance(transform.position, player.position) <= slowdownRadius 
            ? slowdownSpeed 
            : baseSpeed;

        t += Time.deltaTime * currentSpeed;

        // Ista putanja kao sjeverni avion, ali s faznim pomakom od π
        float x = player.position.x + radiusX * Mathf.Cos(t + Mathf.PI); // Fazni pomak za suprotan smjer
        float z = player.position.z + radiusZ * Mathf.Sin(2 * (t + Mathf.PI));
        float y = Mathf.Clamp(player.position.y + yAmplitude * Mathf.Abs(Mathf.Cos(t + Mathf.PI)), 150, 400);

        UpdatePositionAndRotation(new Vector3(x, y, z));
        CheckSoundActivation();
    }

    void UpdatePositionAndRotation(Vector3 newPosition)
    {
        Vector3 direction = newPosition - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
        transform.position = newPosition;
    }

    void CheckSoundActivation()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= soundActivationRadius && !hasPlayed)
        {
            if (airplaneAudioSource != null && !airplaneAudioSource.isPlaying)
            {
                airplaneAudioSource.Play();
                hasPlayed = true;
            }
        }
        else if (distance > soundActivationRadius && hasPlayed)
        {
            hasPlayed = false;
        }
    }

    void StartMoving()
    {
        // Postavi da se avion počeo kretati
        hasStartedMoving = true;
    }
}