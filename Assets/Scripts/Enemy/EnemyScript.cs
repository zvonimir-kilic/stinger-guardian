using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Početno zdravlje neprijatelja
    public int startingHealth = 100;
    private int currentHealth;

    void Start()
    {
        // Postavljanje trenutnog zdravlja na početno zdravlje
        currentHealth = startingHealth;
    }

    // Metoda za primanje štete
    public void TakeDamage(int damage)
    {
        // Smanjenje trenutnog zdravlja
        currentHealth -= damage;

        // Provjera je li neprijatelj mrtav
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Metoda za smrt neprijatelja
    private void Die()
    {
        // Uništavanje neprijatelja iz scene
        Destroy(gameObject);
    }
}
