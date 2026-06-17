using Unity.Netcode;
using UnityEngine;

/// Mrežna verzija GuidedMissile koja sinkronizira target preko mreže
public class NetworkGuidedMissile : NetworkBehaviour
{
    public float speed = 150f; // Brzina rakete
    
    // NetworkVariable za sinkronizaciju target NetworkObjectId
    private NetworkVariable<ulong> targetNetworkObjectId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private GameObject target;
    private bool targetFound = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Pretplati se na promjene targetNetworkObjectId
        targetNetworkObjectId.OnValueChanged += OnTargetChanged;
        
        // Pokušaj pronaći target ako je već postavljen
        if (targetNetworkObjectId.Value != 0)
        {
            FindTargetByNetworkObjectId(targetNetworkObjectId.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        targetNetworkObjectId.OnValueChanged -= OnTargetChanged;
    }

    /// Postavlja target
    public void SetTarget(GameObject newTarget)
    {
        if (!IsServer) return;
        
        if (newTarget != null)
        {
            NetworkObject targetNetObj = newTarget.GetComponent<NetworkObject>();
            if (targetNetObj != null && targetNetObj.IsSpawned)
            {
                targetNetworkObjectId.Value = targetNetObj.NetworkObjectId;
                target = newTarget;
                targetFound = true;
            }
        }
    }

    /// Callback kada se targetNetworkObjectId promijeni
    private void OnTargetChanged(ulong previousValue, ulong newValue)
    {
        if (newValue != 0)
        {
            FindTargetByNetworkObjectId(newValue);
        }
    }

    /// Pronađi target GameObject po NetworkObjectId
    private void FindTargetByNetworkObjectId(ulong networkObjectId)
    {
        if (NetworkManager.Singleton != null && 
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject targetNetObj))
        {
            target = targetNetObj.gameObject;
            targetFound = true;
        }
    }

    void Update()
    {
        // Samo server upravlja kretanjem rakete
        // Pozicija će se sinkronizirati na klijente preko NetworkTransform
        if (!IsServer) return;
        
        // Ako target još nije pronađen, pokušaj ponovo
        if (!targetFound && targetNetworkObjectId.Value != 0)
        {
            FindTargetByNetworkObjectId(targetNetworkObjectId.Value);
        }
        
        // Provjeri postoji li target
        if (target != null)
        {
            // Provjeri da li je target još uvijek aktivan
            if (target.activeInHierarchy)
            {
                // Kreći se direktno prema cilju
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
                
                // Rotiraj raketu prema cilju
                Vector3 direction = (target.transform.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            else
            {
                // Ako je target uništen, očisti referencu
                target = null;
                targetFound = false;
            }
        }
    }
}

