using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneControllerSimple : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 50f;
    public float turnSpeed = 22f; 
    public float pitchSpeed = 30f;
    public float rollSpeed = 80f;

    [Header("Throttle Smoothing")]
    public float throttleAcceleration = 1.5f; // Brzina povećanja throttle-a
    public float throttleDeceleration = 2f; // Brzina smanjenja throttle-a
    
    [Header("Speed-Based Control")]
    public float minSpeedForControl = 5f; // Minimalna brzina za punu kontrolu rotacije
    public float minSpeedForLift = 8f; // Minimalna brzina za lift

    [Header("Lift Settings")]
    public float liftForce = 18f; // Sila uzgona
    public float maxLiftSpeed = 25f; // Maksimalna brzina dizanja
    
    [Header("Physics")]
    public float dragAmount = 0.5f; // Otpor zraka
    public float angularDragAmount = 2f; // Otpor rotacije

    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 1f; // Osjetljivost miša za pitch i yaw

    [Header("Flight Boundary")]
    public float maxDistanceFromSpawn = 2000f; // Maksimalna horizontalna udaljenost od centra
    public float maxHeight = 500f; // Maksimalna visina od centra 
    public Vector3 boundaryCenter; // Centar boundary-ja

    private Rigidbody rb;
    private float currentThrottle = 0f; // Trenutni throttle

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Gravitacija uključena za realniji let
        rb.drag = dragAmount;
        rb.angularDrag = angularDragAmount;
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleLift();
    }

    void HandleMovement()
    {
        float targetThrottle = 0f;
        
        // Samo naprijed
        if(Input.GetKey(KeyCode.W))
        {
            targetThrottle = 1f; // W = naprijed
        }

        // Gradijentno povećavanje/smanjivanje throttle-a
        if (targetThrottle > currentThrottle)
        {
            currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, throttleAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, throttleDeceleration * Time.fixedDeltaTime);
        }

        float moveInput = currentThrottle;

        // Provjeri boundary prije primjene sile
        Vector3 positionRelativeToCenter = transform.position - boundaryCenter;
        
        // Horizontalna udaljenost
        Vector3 horizontalPosition = new Vector3(positionRelativeToCenter.x, 0f, positionRelativeToCenter.z);
        float horizontalDistance = horizontalPosition.magnitude;
        bool isAtHorizontalBoundary = horizontalDistance >= maxDistanceFromSpawn;
        
        // Visina
        float heightFromCenter = positionRelativeToCenter.y;
        bool isAtHeightBoundary = heightFromCenter >= maxHeight;
        
        if ((isAtHorizontalBoundary || isAtHeightBoundary) && moveInput > 0f) // Samo ako ide naprijed i na granici
        {
            // Provjeri horizontalnu granicu
            if (isAtHorizontalBoundary)
            {
                Vector3 horizontalDirectionToCenter = -horizontalPosition.normalized;
                Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                float horizontalDot = Vector3.Dot(horizontalForward, horizontalDirectionToCenter);
                
                // Ako ide dalje horizontalno, blokiraj
                if (horizontalDot < 0f)
                {
                    moveInput = 0f; // Blokiraj kretanje naprijed
                }
            }
            
            // Provjeri visinu
            if (isAtHeightBoundary)
            {
                // Ako ide gore (pozitivan Y u forward smjeru), blokiraj
                if (transform.forward.y > 0f)
                {
                    moveInput = 0f; // Blokiraj kretanje naprijed
                }
            }
        }
        
        Vector3 forwardForce = transform.forward * moveInput * moveSpeed;
        rb.AddForce(forwardForce);
        
        // Zaustavi brzinu u smjeru koji vodi dalje od granica
        Vector3 velocity = rb.velocity;
        
        // Horizontalna granica
        if (isAtHorizontalBoundary)
        {
            Vector3 horizontalDirectionToCenter = -horizontalPosition.normalized;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float velocityTowardsCenter = Vector3.Dot(horizontalVelocity, horizontalDirectionToCenter);
            
            // Ako se kreće dalje horizontalno, zaustavi tu komponentu brzine
            if (velocityTowardsCenter < 0f)
            {
                Vector3 velocityAwayFromCenter = horizontalVelocity - (horizontalDirectionToCenter * velocityTowardsCenter);
                velocity.x = velocityAwayFromCenter.x;
                velocity.z = velocityAwayFromCenter.z;
            }
        }
        
        // Visinska granica
        if (isAtHeightBoundary)
        {
            // Ako se kreće gore, zaustavi Y komponentu brzine
            if (velocity.y > 0f)
            {
                velocity.y = 0f;
            }
        }
        
        rb.velocity = velocity;
    }

    void HandleRotation()
    {
        // Izračunaj faktor kontrole ovisno o brzini
        float speed = rb.velocity.magnitude;
        float controlFactor = Mathf.Clamp01(speed / minSpeedForControl);
        
        float yaw = 0f;
        float pitch = 0f;
        float roll = 0f;
        
        float mouseX = Input.GetAxis("Mouse X");
        yaw = mouseX * mouseSensitivity;

        float mouseY = Input.GetAxis("Mouse Y");
        pitch = -mouseY * mouseSensitivity;

        if(Input.GetKey(KeyCode.A))
        {
            roll = 1f;
        }
        else if(Input.GetKey(KeyCode.D))
        {
            roll = -1f;
        }

        // Pitch i Yaw ovise o brzini, Roll ostaje neovisan
        Vector3 rotation = new Vector3(
            pitch * pitchSpeed * controlFactor, 
            yaw * turnSpeed * controlFactor, 
            roll * rollSpeed
        );

        rb.AddRelativeTorque(rotation);
    }

    void HandleLift()
    {
        // Space tipka za uzgon
        if (Input.GetKey(KeyCode.Space))
        {
            // Provjeri boundary prije primjene lifta
            Vector3 positionRelativeToCenter = transform.position - boundaryCenter;
            float heightFromCenter = positionRelativeToCenter.y;
            bool isAtHeightBoundary = heightFromCenter >= maxHeight;
            
            // Ako nije na granici visine, primijeni lift
            if (!isAtHeightBoundary)
            {
                // Lift ovisi o brzini
                float speed = rb.velocity.magnitude;
                float liftFactor = Mathf.Clamp01(speed / minSpeedForLift);
                
                // Ograniči brzinu dizanja da ne bude prebrzo
                if (rb.velocity.y < maxLiftSpeed)
                {
                    rb.AddForce(Vector3.up * liftForce * liftFactor, ForceMode.Acceleration);
                }
            }
        }
    }
}