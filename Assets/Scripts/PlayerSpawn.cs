
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawn : NetworkBehaviour
{
    [Header("Posición inicial")]
    [SerializeField]
    private Vector3 spawnPosition =
        new Vector3(-2.2f, 1.9f, -12.6f);

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // Colocar al jugador en la posición inicial.
        transform.position = spawnPosition;

        Debug.Log(
            $"Jugador {OwnerClientId} apareció en {spawnPosition}"
        );
    }
}
