using UnityEngine;
using System.Collections;

public class AirplaneAttack : MonoBehaviour
{
    public Transform player; // Referenca na igrača
    public GameObject missilePrefab; // Prefab rakete
    public Transform firePoint; // Mjesto ispaljivanja raketa na avionu
    public float attackRadius = 50f; // Radijus unutar kojeg avion počinje pucati
    public float missileSpreadRadius = 10f; // Radijus unutar kojeg rakete padaju slučajno oko igrača
    public float fireRate = 2f; // Vrijeme između napada
    public float missileSpeed = 20f; // Brzina rakete
    public int missilesPerAttack = 3; // Broj raketa po napadu
    public float timeBetweenMissiles = 0.5f; // Razmak između raketa u sekundama
    private float nextFireTime;
    private bool hasAttacked = false; // Je li avion već napao u ovom ulasku u radijus

    void Update()
    {
        // Provjeri udaljenost igrača
        if (player == null) return; // Ako nema igrača, ništa ne radi
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Ako je avion unutar attackRadius
        if (distanceToPlayer <= attackRadius)
        {
            // Ako nije već napao i vrijeme je za sljedeći napad
            if (!hasAttacked && Time.time >= nextFireTime)
            {
                // Pokreni korutinu za ispaljivanje raketa s razmakom
                StartCoroutine(FireMissilesWithDelay());
                hasAttacked = true; // Označi da je napao
                nextFireTime = Time.time + fireRate; // Postavi vrijeme za sljedeći napad
            }
        }
        else
        {
            // Ako avion izađe iz attackRadius, resetiraj hasAttacked
            hasAttacked = false;
        }
    }

    IEnumerator FireMissilesWithDelay()
    {
        for (int i = 0; i < missilesPerAttack; i++)
        {
            // Generiraj slučajnu točku unutar missileSpreadRadius oko igrača
            Vector3 randomPointAroundPlayer = player.position + Random.insideUnitSphere * missileSpreadRadius;

            // Zadrži rakete u istoj visini
            randomPointAroundPlayer.y = player.position.y;

            // Stvori raketu na firePoint poziciji
            GameObject missile = Instantiate(missilePrefab, firePoint.position, firePoint.rotation);

            // Postavi raketi smjer prema slučajnoj točki oko igrača
            Vector3 direction = (randomPointAroundPlayer - firePoint.position).normalized;

            missile.GetComponent<Rigidbody>().velocity = direction * missileSpeed;

            // Uništi raketu nakon određenog vremena (sprječava nakupljanje objekata)
            Destroy(missile, 10f);

            // Pričekaj 0.5 sekundi prije sljedeće rakete
            yield return new WaitForSeconds(timeBetweenMissiles);
        }
    }
}