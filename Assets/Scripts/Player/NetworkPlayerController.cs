using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Mrežni wrapper za kontrolu Player-a koji omogućava da samo vlasnik kontrolira svog igrača
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    
    private InputManager inputManager;
    private CharacterController characterController;
    
    private bool freezeYPosition = false;
    private float frozenYPosition;

    private void Awake()
    {
        // Dohvati postojeće komponente
        inputManager = GetComponent<InputManager>();
        characterController = GetComponent<CharacterController>();
        
        // Ako kamera nije postavljena u Inspectoru, pokušaj pronaći unutar ovog objekta
        if (playerCamera == null)
        {
            // Traži kameru samo unutar djece ovog objekta
            foreach (Transform child in transform)
            {
                Camera cam = child.GetComponent<Camera>();
                if (cam != null)
                {
                    playerCamera = cam;
                    audioListener = child.GetComponent<AudioListener>();
                    break;
                }
                
                // Ako nije direktno dijete, traži dublje
                cam = child.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    playerCamera = cam;
                    audioListener = cam.GetComponent<AudioListener>();
                    break;
                }
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Provjeri je li u lobby sceni, ako je onda ne omogućavaj komponente
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MultiplayerGameLobby")
        {
            // Onemogući sve komponente za lobby
            if (inputManager != null)
                inputManager.enabled = false;
            
            NetworkAiming networkAiming = GetComponent<NetworkAiming>();
            if (networkAiming != null)
                networkAiming.enabled = false;
            
            StingerScript stingerScript = GetComponentInChildren<StingerScript>();
            if (stingerScript != null)
                stingerScript.enabled = false;
            
            NetworkStingerScript networkStingerScript = GetComponentInChildren<NetworkStingerScript>();
            if (networkStingerScript != null)
                networkStingerScript.enabled = false;
            
            // Onemogući PlayerCamera
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                if (audioListener != null)
                    audioListener.enabled = false;
            }
            
            // Onemogući CharacterController
            if (characterController != null)
                characterController.enabled = false;
            
            // Osiguraj da je Main Camera aktivna
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }
            else
            {
                GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCamObj != null)
                {
                    Camera cam = mainCamObj.GetComponent<Camera>();
                    if (cam != null) cam.enabled = true;
                }
            }
            
            // Osiguraj da je kursor vidljiv i otključan za UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        // Postavi spawn poziciju, zamrzni Y na kratko i omogući kontrole samo za vlasnika igrača
        if (IsOwner)
        {
            // Onemogući CharacterController privremeno
            if (characterController != null)
                characterController.enabled = false;
            
            // Pozicija se već postavlja u NetworkSpawnManager, ali osiguravamo da je ispravna
            frozenYPosition = transform.position.y;
            freezeYPosition = true;
            
            // Odmrzni nakon 3 sekunde i omogući CharacterController
            StartCoroutine(UnfreezeYPositionDelayed());

            // Omogući InputManager
            if (inputManager != null)
                inputManager.enabled = true;
            
            // Omogući kameru i audio samo za vlasnika igrača
            if (playerCamera != null)
                playerCamera.enabled = true;
            
            if (audioListener != null)
                audioListener.enabled = true;

            // Zaključaj kursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Onemogući InputManager za remote igrače
            if (inputManager != null)
                inputManager.enabled = false;
            
            // Onemogući kameru i audio za remote igrače
            if (playerCamera != null)
                playerCamera.enabled = false;
            
            if (audioListener != null)
                audioListener.enabled = false;
        }
    }

    private IEnumerator UnfreezeYPositionDelayed()
    {
        // Čekaj 3 sekunde
        yield return new WaitForSeconds(3f);
        
        // Omogući CharacterController
        if (characterController != null)
            characterController.enabled = true;
        
        // Odmrzni Y poziciju
        freezeYPosition = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Provjeri je li voda
        if (other.CompareTag("Water"))
        {
            // Pozovi ServerRpc za respawn
            RequestRespawnServerRpc();
        }
    }

    [ServerRpc]
    private void RequestRespawnServerRpc()
    {
        // Provjeri je li objekt još spawnan
        if (!IsSpawned) return;

        ulong ownerId = NetworkObject.OwnerClientId;
        
        // Provjeri je li netko ranio ovog igrača prije smrti
        NetworkHealth health = GetComponent<NetworkHealth>();
        ulong lastAttackerId = health != null ? health.GetLastAttackerId() : ulong.MaxValue;
        
        // Ažuriraj statistike
        NetworkPlayerStats victimStats = GetComponent<NetworkPlayerStats>();
        if (victimStats != null)
        {
            victimStats.AddDeath();
        }
        
        // Ako postoji lastAttacker, daj mu kill
        if (lastAttackerId != ulong.MaxValue && lastAttackerId != ownerId)
        {
            // Pronađi stats napadača
            NetworkPlayerStats[] allStats = FindObjectsOfType<NetworkPlayerStats>();
            foreach (var stats in allStats)
            {
                NetworkObject obj = stats.GetComponent<NetworkObject>();
                if (obj != null && obj.IsSpawned && obj.OwnerClientId == lastAttackerId)
                {
                    stats.AddKill();
                    break;
                }
            }
        }
        
        // Ispiši sve statistike
        NetworkSpawnManager.LogAllPlayerStats();
        
        // Despawnaj igrača
        NetworkObject.Despawn();
        Destroy(gameObject);

        // Respawnaj igrača na početnoj poziciji
        NetworkSpawnManager spawnManager = FindObjectOfType<NetworkSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.RespawnPlayer(ownerId);
        }
    }
}

