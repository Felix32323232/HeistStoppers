using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlueRoleAbility : NetworkBehaviour
{
    public static BlueRoleAbility Instance { get; private set; }

    [Header("Controles")]
    [SerializeField] private KeyCode abilityKey = KeyCode.F;

    [Header("Configuración Activa (Calmar NPCs)")]
    [SerializeField] private float radius = 6f;
    [SerializeField] private float extraPatienceSeconds = 10f;
    [SerializeField] private float cooldown = 15f;

    [Header("Configuración Pasiva (Velocidad de tarea)")]
    [SerializeField] private float interactionSpeedMultiplier = 1.4f;

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

    public bool IsLocalPlayerBlue
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
            return roleManager.GetRole(localId) == RoleManager.Role.Blue;
        }
    }

    public float GetTaskSpeedMultiplier()
    {
        return IsLocalPlayerBlue ? interactionSpeedMultiplier : 1f;
    }

    private void Update()
    {
        if (!IsSpawned || !IsLocalPlayerBlue)
            return;

        if (Input.GetKeyDown(abilityKey) && Time.time >= nextUseTime)
        {
            nextUseTime = Time.time + cooldown;
            CalmNearbyNPCsRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CalmNearbyNPCsRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance == null ||
            RoleManager.Instance.GetRole(senderId) != RoleManager.Role.Blue)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out NetworkClient client) ||
            client.PlayerObject == null)
            return;

        Vector3 playerPos = client.PlayerObject.transform.position;
        Collider[] hits = Physics.OverlapSphere(playerPos, radius);
        HashSet<NPCBase> affected = new HashSet<NPCBase>();

        foreach (Collider hit in hits)
        {
            NPCCounterCustomer customer = hit.GetComponentInParent<NPCCounterCustomer>();
            if (customer != null && affected.Add(customer))
            {
                customer.AddPatience(extraPatienceSeconds);
                continue;
            }

            NPCMetalDetectorVisitor visitor = hit.GetComponentInParent<NPCMetalDetectorVisitor>();
            if (visitor != null && affected.Add(visitor))
            {
                visitor.AddFriskPatience(extraPatienceSeconds);
            }
        }

        Debug.Log($"[Azul] Jugador {senderId} calmó a {affected.Count} clientes cercanos.");
    }
}