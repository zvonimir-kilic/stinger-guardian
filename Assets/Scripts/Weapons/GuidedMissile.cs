using UnityEngine;

public class GuidedMissile : MonoBehaviour
{
    public GameObject target; // Cilj rakete
    public float speed = 70f; // Brzina rakete

    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        // Provjeri postoji li target
        if (target != null)
        {
            // Provjeri da li je target još uvijek aktivan
            if (target.activeInHierarchy)
            {
                // Kreći se direktno prema cilju
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            }
            else
            {
                // Ako je target uništen, očisti referencu
                target = null;
            }
        }
    }
}