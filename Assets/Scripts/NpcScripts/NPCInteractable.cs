using Unity.Netcode;
using UnityEngine;

public class NPCInteractable : Interactable
{
    [Header("NPC")]
    [Tooltip("Roles que pueden atender a este NPC. Vacío = cualquier jugador que ya tenga rol y esté listo.")]
    [SerializeField] RoleManager.Role[] allowedRoles;
    [Tooltip("Texto del prompt. Se le añade lo que diga el NPC: \"Atender: Quiero sacar dinero\".")]
    [SerializeField] string verb = "Atender";
    [SerializeField] float interactionRadius = 1.6f;

    public NetworkVariable<bool> canBeServed = new NetworkVariable<bool>(false);

    NPCBase npc;
    SphereCollider zone;

    void Reset() { }

    void Awake()
    {
        npc = GetComponent<NPCBase>();

        zone = gameObject.AddComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = interactionRadius;
        zone.center = Vector3.up;
        zone.enabled = false;

        enabled = false;
        prompt = verb;
    }

    public override void OnNetworkSpawn()
    {
        // Sin base.OnNetworkSpawn(): el de Interactable arranca el temporizador de tareas del pool.
        canBeServed.OnValueChanged += OnServableChanged;
        Apply(canBeServed.Value);
    }

    public override void OnNetworkDespawn()
    {
        canBeServed.OnValueChanged -= OnServableChanged;
    }

    void OnServableChanged(bool previous, bool current)
    {
        Apply(current);
    }

    void Apply(bool servable)
    {
        zone.enabled = servable;
        enabled = servable;
    }

    public void SetRequest(string text)
    {
        prompt = string.IsNullOrEmpty(text) ? verb : verb + ": " + text;
    }

    public override bool CanBeUsedBy(RoleManager.Role role)
    {
        if (role == RoleManager.Role.None) return false;
        if (allowedRoles == null || allowedRoles.Length == 0) return true;

        foreach (RoleManager.Role allowed in allowedRoles)
        {
            if (allowed == role) return true;
        }
        return false;
    }

    public override void InteractuarNet()
    {
        if (!IsServer || !canBeServed.Value || npc == null) return;

        canBeServed.Value = false;
        npc.OnServed();
    }
}