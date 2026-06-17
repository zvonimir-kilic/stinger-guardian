using UnityEngine;

public class JetMovement : MonoBehaviour
{
    public float baseSpeed = 0.06f;
    public float yAmplitude = 250f;
    public float radiusX = 1000f;
    public float radiusZ = 400f;
    public Transform player; // Referenca na Transform igrača
    public float slowdownRadius = 400f;   // Radijus za usporavanje
    public float slowdownSpeed = 0.02f;   // Fiksna brzina unutar radijusa

    public AudioSource airplaneAudioSource; // Referenca na AudioSource na avionu
    public float soundActivationRadius = 650f; // Radijus za aktivaciju zvuka
    private bool hasPlayed = false; // Je li zvuk već reproduciran

    private float t = 0f;
    private bool hasStartedMoving = false; // Je li avion započeo kretnju

    void Start()
    {
        // Pozovi metodu StartMoving nakon 15 sekundi
        Invoke("StartMoving", 15f);
    }

    void Update()
    {
        // Ako se avion još nije počeo kretati, nemoj raditi ništa
        if (!hasStartedMoving)
        {
            return;
        }

        // Ako igrač nije postavljen, nemoj raditi ništa
        if (player == null)
        {
            Debug.LogWarning("Player nije postavljen!");
            return;
        }

        // Provjera udaljenosti i odabir brzine
        float currentSpeed = Vector3.Distance(transform.position, player.position) <= slowdownRadius
            ? slowdownSpeed
            : baseSpeed;

        t += Time.deltaTime * currentSpeed;

        // Izračunaj novu poziciju aviona oko igrača
        float x = player.position.x + radiusX * Mathf.Cos(t);
        float z = player.position.z + radiusZ * Mathf.Sin(2 * t);
        float y = Mathf.Clamp(player.position.y + yAmplitude * Mathf.Abs(Mathf.Cos(t)), 150, 400);

        UpdatePositionAndRotation(new Vector3(x, y, z));

        // Provjeri udaljenost od igrača i reproduciraj zvuk ako je potrebno
        CheckSoundActivation();
    }

    void UpdatePositionAndRotation(Vector3 newPosition)
    {
        Vector3 direction = newPosition - transform.position;
        if(direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                Time.deltaTime * 2f
            );
        }
        
        transform.position = newPosition;
    }

    void CheckSoundActivation()
    {
        // Provjeri udaljenost aviona od igrača
        float distance = Vector3.Distance(transform.position, player.position);

        // Ako je avion unutar radijusa i zvuk još nije reproduciran
        if (distance <= soundActivationRadius && !hasPlayed)
        {
            // Reproduciraj zvuk
            if (airplaneAudioSource != null && !airplaneAudioSource.isPlaying)
            {
                airplaneAudioSource.Play();
                hasPlayed = true; // Označi da je zvuk reproduciran
            }
        }

        // Ako avion izađe iz radijusa, resetiraj hasPlayed
        if (distance > soundActivationRadius && hasPlayed)
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