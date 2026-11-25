using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class YellowRoleAbility : NetworkBehaviour
{
    public static YellowRoleAbility Instance { get; private set; }

    [Header("Controles")]
    [SerializeField] private KeyCode abilityKey = KeyCode.F;

    [Header("Configuración Activa (Alto / Cacheo rápido)")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float cooldown = 14f;

    [Header("Pasiva de Golpe")]
    [SerializeField] private float punchRangeBonus = 1.2f;

    private RoleManager roleManager;
    private float nextUseTime;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    public bool IsLocalPlayerYellow
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
                return false;

            if (roleManager == null)
                roleManager = RoleManager.Instance;

            if (roleManager == null || !roleManager.IsSpawned)
                return false;

            ulong localId = NetworkManager.Singleton.LocalClientId;
            return roleManager.GetRole(localId) == RoleManager.Role.Yellow;
        }
    }

    public float GetExtraPunchRange()
    {
        return IsLocalPlayerYellow ? punchRangeBonus : 0f;
    }

    private void Update()
    {
        if (!IsSpawned || !IsLocalPlayerYellow)
            return;

        if (Input.GetKeyDown(abilityKey) && Time.time >= nextUseTime)
        {
            nextUseTime = Time.time + cooldown;
            SecurityShoutRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SecurityShoutRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance == null ||
            RoleManager.Instance.GetRole(senderId) != RoleManager.Role.Yellow)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out NetworkClient client) ||
            client.PlayerObject == null)
            return;

        Vector3 playerPos = client.PlayerObject.transform.position;
        Vector3 playerForward = client.PlayerObject.transform.forward;
        Collider[] hits = Physics.OverlapSphere(playerPos, radius);

        HashSet<NPCMetalDetectorVisitor> frisked = new HashSet<NPCMetalDetectorVisitor>();
        HashSet<NPCKnockback> pushed = new HashSet<NPCKnockback>();

        foreach (Collider hit in hits)
        {
            NPCMetalDetectorVisitor visitor = hit.GetComponentInParent<NPCMetalDetectorVisitor>();
            if (visitor != null && frisked.Add(visitor))
            {
                visitor.OnServed();
            }

            NPCKnockback knockback = hit.GetComponentInParent<NPCKnockback>();
            if (knockback != null && pushed.Add(knockback))
            {
                Vector3 dir = knockback.transform.position - playerPos;
                if (dir.sqrMagnitude < 0.01f)
                    dir = playerForward;

                knockback.ReceiveHit(dir);
            }
        }

        Debug.Log($"[Amarillo] Jugador {senderId} usó Alto de Seguridad.");
    }
}