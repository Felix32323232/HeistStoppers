using Unity.Netcode;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [SerializeField] Camera cam;

    InteractionUI ui;
    Interactable current;
    float held;
    bool completed;

    bool hasTarget;

    PlayerIdentity identity;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        identity = GetComponent<PlayerIdentity>();

        ui = InteractionUI.Instance;
        ui.Hide();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && ui != null) ui.Hide();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsOwner || ui == null) return;
        if (!other.TryGetComponent(out Interactable i)) return;
        if (identity == null || i.requiredPlayerId.Value != identity.playerId.Value) return;

        current = i;
        hasTarget = true;
        ResetHold();
        ui.Show(i.prompt);
    }

    void ClearCurrent()
    {
        current = null;
        hasTarget = false;
        ResetHold();
        if (ui != null) ui.Hide();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsOwner || ui == null) return;
        if (other.TryGetComponent(out Interactable i) && i == current)
            ClearCurrent();
    }

    void Update()
    {
        // Objetivo destruido/despawneado o desactivado: limpiar y ocultar
        if (hasTarget && (current == null || !current.isActiveAndEnabled))
        {
            ClearCurrent();
            return;
        }

        if (current == null) return;

        if (Input.GetKey(interactKey))
        {
            if (!completed)
            {
                held += Time.deltaTime;
                if (held >= current.holdTime)
                {
                    completed = true;
                    InteractuarServerRpc(current.GetComponent<NetworkObject>());
                    ClearCurrent();   // oculta el prompt y suelta el objetivo
                    return;
                }
            }
        }
        else
        {
            ResetHold();
        }

        ui.SetFill(Mathf.Clamp01(held / current.holdTime));
    }

    void LateUpdate()
    {
        if (current == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(current.PromptPosition);
        bool visible = screenPos.z > 0f;

        ui.SetVisible(visible);
        if (visible) ui.SetScreenPosition(screenPos);
    }

    void ResetHold()
    {
        held = 0f;
        completed = false;
        if (ui != null) ui.SetFill(0f);
    }

    [ServerRpc]
    private void InteractuarServerRpc(NetworkObjectReference interactableReference)
    {
        if (interactableReference.TryGet(out NetworkObject netObj) &&
            netObj.TryGetComponent(out Interactable i))
        {
            
            if (i.requiredPlayerId.Value == 0 || i.requiredPlayerId.Value == identity.playerId.Value)
            {
                i.InteractuarNet();
            }
        }
    }

}