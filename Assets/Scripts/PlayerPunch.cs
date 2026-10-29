
using Unity.Netcode;
using UnityEngine;

public class PlayerPunch : NetworkBehaviour
{
    [Header("Golpe")]
    [SerializeField] private float range = 2f;
    [SerializeField] private float knockbackUp = 1f;
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Punto de golpe")]
    [SerializeField] private Transform punchPoint;

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0))
            Punch();
    }

    private void Punch()
    {
        // Si no asignas punchPoint, utiliza el centro del jugador.
        Vector3 origin = punchPoint != null
            ? punchPoint.position
            : transform.position + Vector3.up;

        // Dirección horizontal hacia donde mira el personaje.
        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            range,
            hitLayers,
            QueryTriggerInteraction.Ignore))
        {
            NPCKnockback npc =
                hit.collider.GetComponentInParent<NPCKnockback>();

            if (npc == null || !npc.NetworkObject.IsSpawned)
                return;

            HitNPCServerRpc(
                new NetworkObjectReference(npc.NetworkObject)
            );
        }
    }

    [ServerRpc]
    private void HitNPCServerRpc(
        NetworkObjectReference npcReference)
    {
        if (!npcReference.TryGet(out NetworkObject npcObject))
            return;

        NPCKnockback npc =
            npcObject.GetComponent<NPCKnockback>();

        if (npc == null)
            return;

        Vector3 origin = punchPoint != null
            ? punchPoint.position
            : transform.position + Vector3.up;

        Vector3 difference =
            npcObject.transform.position - origin;

        // Validar distancia y dirección también en el servidor.
        if (difference.magnitude > range + 0.5f)
            return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 toNpc = difference.normalized;

        if (Vector3.Dot(forward, toNpc) < 0.3f)
            return;

        npc.ReceiveHit(forward + Vector3.up * knockbackUp);
    }
}
