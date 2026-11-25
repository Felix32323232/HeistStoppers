using System.Collections;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;

public class GreenRoleAbility : NetworkBehaviour
{
    [Header("Controles")]
    [SerializeField] private KeyCode abilityKey = KeyCode.F;

    [Header("Configuración de Velocidad")]
    [SerializeField] private float speedMultiplier = 1.65f;
    [SerializeField] private float duration = 4f;
    [SerializeField] private float cooldown = 12f;

    private RoleManager roleManager;
    private ThirdPersonController localController;
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private bool boostActive;
    private float nextUseTime;
    private Coroutine boostRoutine;

    private void Update()
    {
        if (!IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return;

        if (roleManager == null)
            roleManager = RoleManager.Instance;

        if (roleManager == null || !roleManager.IsSpawned)
            return;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isGreen = roleManager.GetRole(localId) == RoleManager.Role.Green;

        if (!isGreen)
        {
            if (boostActive)
                StopBoost();
            return;
        }

        if (Input.GetKeyDown(abilityKey) && !boostActive && Time.time >= nextUseTime)
        {
            if (TryCacheLocalController())
            {
                nextUseTime = Time.time + cooldown;
                boostRoutine = StartCoroutine(SpeedBoostRoutine());
            }
        }
    }

    private bool TryCacheLocalController()
    {
        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null)
            return false;

        ThirdPersonController controller = localPlayer.GetComponent<ThirdPersonController>();
        if (controller == null)
            return false;

        if (localController != controller)
        {
            localController = controller;
            baseMoveSpeed = localController.MoveSpeed;
            baseSprintSpeed = localController.SprintSpeed;
        }

        return true;
    }

    private IEnumerator SpeedBoostRoutine()
    {
        boostActive = true;
        localController.MoveSpeed = baseMoveSpeed * speedMultiplier;
        localController.SprintSpeed = baseSprintSpeed * speedMultiplier;

        Debug.Log("[Verde] Sprint táctico activado.");

        yield return new WaitForSeconds(duration);

        StopBoost();
    }

    private void StopBoost()
    {
        if (boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
            boostRoutine = null;
        }

        if (localController != null)
        {
            localController.MoveSpeed = baseMoveSpeed;
            localController.SprintSpeed = baseSprintSpeed;
        }

        boostActive = false;
    }

    public override void OnNetworkDespawn()
    {
        if (boostActive)
            StopBoost();

        localController = null;
    }

    private void OnDisable()
    {
        if (boostActive)
            StopBoost();
    }
}