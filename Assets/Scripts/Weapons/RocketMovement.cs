using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RocketMovement : MonoBehaviour
{
    public GameObject explosion; // Prefab za BigExplosion
    public LayerMask whatIsEnemies;

    // Statistika
    [Range(0f, 1f)]
    public float bounciness;
    public bool useGravity;

    // Šteta
    public int explosionDamage;
    public float explosionRange;
    public float explosionForce;

    // Životni vijek
    public int maxCollisions;
    public float maxLifetime;
    public bool explodeOnTouch = true;

    private int collisions;
    private PhysicMaterial physics_mat;

    private bool hasExploded = false;
	
	public float explosionRadius = 50f; // Radijus eksplozije
	public float showRedRadius = 15f; // Radijus prikaza crvenog traga
    public float criticalRadius = 5f; // Radijus uništenja
	private Shake shake;
	private NetworkShake networkShake;

    private void Start()
    {
        shake = GameObject.FindGameObjectWithTag("ScreenShake").GetComponent<Shake>();
        networkShake = GameObject.FindGameObjectWithTag("ScreenShake").GetComponent<NetworkShake>();
        Setup();
    }

    private void Update(){
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Stvori eksploziju (BigExplosion)
        if (explosion != null)
        {
            GameObject spawnedExplosion = Instantiate(explosion, transform.position, Quaternion.identity);

            // Pronađi AudioSource komponentu na instanciranoj eksploziji
            AudioSource explosionAudio = spawnedExplosion.GetComponent<AudioSource>();
            if (explosionAudio != null && explosionAudio.clip != null)
            {
                explosionAudio.Play(); // Reproduciraj zvuk eksplozije
            }

            Destroy(spawnedExplosion, 2f); // Uništi eksploziju nakon 2 sekunde
        }
		
		// Provjeri udaljenost od igrača
		GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
		    float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

		    if (distanceToPlayer <= explosionRadius)
            {
			    if (distanceToPlayer <= showRedRadius) // Dodaj efekt crvenog traga ako je u radijusu 10f
			        {
				        ScreenEffect screenEffect = FindObjectOfType<ScreenEffect>();
				        if (screenEffect != null)
				        {
					        screenEffect.ShowEffect(1f); // Prikaz efekta u trajanju 1 sekunde
				        }

                        if (distanceToPlayer <= criticalRadius)
                        {
                             SceneManager.LoadScene("GameOver");
                        }
			        }
			    
			    // Podrhtavanje kamere
			    if (shake != null)
			        shake.CamShake();
                
                if (networkShake != null)
                    networkShake.CamShake();
		    }
	    }

        // Provjeri neprijatelje unutar radijusa eksplozije
        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, whatIsEnemies);

        for (int i = 0; i < enemies.Length; i++)
        {
            // Provjeri ima li objekt oznaku "Enemy"
            if (enemies[i].CompareTag("Enemy"))
            {
                // Uništi neprijateljski objekt
                Destroy(enemies[i].gameObject);
            }
        }

        // Dodaj malo kašnjenje prije uništavanja same rakete
        Invoke("Delay", 0.05f);
    }

    private void Delay()
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ne broji sudare s drugim raketama
        if (collision.collider.CompareTag("Bullet")) return;

        // Povećaj broj sudara
        collisions++;

        // Eksplodira ako raketa direktno pogodi neprijatelja i aktivira se explodeOnTouch
        if (collision.collider.CompareTag("Enemy") && explodeOnTouch)
        {
            // Odmah uništi neprijateljski objekt
            Destroy(collision.collider.gameObject);

             // Obavijesti GameManager da je avion uništen
            EnemyCounter.Instance.RegisterEnemyDestroyed();

            // Pokreni eksploziju
            Explode();
        }
        else
        {
            Explode();
        }
    }

    private void Setup()
    {
        // Stvori novi materijal
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;
    }

    /// Vizualizacija dometa eksplozije
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}