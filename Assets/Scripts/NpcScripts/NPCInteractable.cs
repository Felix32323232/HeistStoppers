using Unity.Netcode;
using UnityEngine;

public class NPCInteractable : Interactable
{
    [Header("NPC")]
    [Tooltip("0 = cualquier jugador puede atenderlo.")]
    [SerializeField] int assignedPlayerId = 0;
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
        canBeServed.OnValueChanged += OnServableChanged;
        Apply(canBeServed.Value);

        if (IsServer) requiredPlayerId.Value = assignedPlayerId;
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

    public override void InteractuarNet()
    {
        if (!IsServer || !canBeServed.Value || npc == null) return;

        canBeServed.Value = false;
        npc.OnServed();
    }
}
