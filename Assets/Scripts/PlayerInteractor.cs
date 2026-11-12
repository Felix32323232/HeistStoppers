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
    float needleAngle, zoneStart, zoneDeg;
    bool skillCheckActive;
    int successes;
    float nextAttemptTime;
    float mashProgress;
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
        if (identity == null) return;
        if (i.requiredPlayerId.Value != 0 && i.requiredPlayerId.Value != identity.playerId.Value) return;

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
        if (hasTarget && (current == null || !current.isActiveAndEnabled))
        {
            ClearCurrent();
            return;
        }
        if (current == null) return;

        switch (current.type)
        {
            case Interactable.InteractionType.Hold: UpdateHold(); break;
            case Interactable.InteractionType.SkillCheck: UpdateSkillCheck(); break;
            case Interactable.InteractionType.Mash: UpdateMash(); break;
        }
    }

    void Complete()
    {
        InteractuarServerRpc(current.GetComponent<NetworkObject>());
        ClearCurrent();
    }

    void UpdateHold()
    {
        if (Input.GetKey(interactKey))
        {
            held += Time.deltaTime;
            if (held >= current.holdTime) { Complete(); return; }
        }
        else held = 0f;

        ui.SetFill(Mathf.Clamp01(held / current.holdTime));
    }

    void UpdateMash()
    {
        if (Input.GetKeyDown(interactKey))
            mashProgress += 3f / current.mashPresses;

        mashProgress = Mathf.Clamp01(mashProgress - current.mashDecayPerSecond * Time.deltaTime);
        ui.SetFill(mashProgress);

        if (mashProgress >= 1f) Complete();
    }

    void UpdateSkillCheck()
    {
        if (!skillCheckActive)
        {
            if (Time.time >= nextAttemptTime && Input.GetKeyDown(interactKey))
                StartSkillCheck();
            return;
        }

        needleAngle += 360f / current.skillCheckRotationTime * Time.deltaTime;
        ui.SetNeedle(needleAngle);

        bool pressed = Input.GetKeyDown(interactKey);
        if (!pressed) return;

        bool hit = pressed && needleAngle >= zoneStart && needleAngle <= zoneStart + zoneDeg;
        EndSkillCheck();

        if (hit)
        {
            successes++;
            if (successes >= current.skillChecksRequired) { Complete(); return; }
            StartSkillCheck();   // siguiente check de la serie
        }
        else
        {
            successes = 0;
            nextAttemptTime = Time.time + current.failCooldown;
        }
    }

    void StartSkillCheck()
    {
        skillCheckActive = true;
        needleAngle = 0f;
        zoneDeg = current.skillCheckZoneSize * 360f;
        // empieza lejos de la aguja y sin cruzar el 0 para evitar el wrap
        zoneStart = Random.Range(60f, 360f - zoneDeg - 10f);
        ui.ShowSkillCheck(zoneStart, current.skillCheckZoneSize);
    }

    void EndSkillCheck()
    {
        skillCheckActive = false;
        ui.HideSkillCheck();
    }

    void ResetHold()
    {
        held = 0f;
        mashProgress = 0f;
        successes = 0;
        skillCheckActive = false;
        if (ui != null)
        {
            ui.SetFill(0f);
            ui.HideSkillCheck();
        }
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