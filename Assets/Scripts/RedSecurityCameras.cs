
using Unity.Netcode;
using UnityEngine;

public class RedSecurityCameras : NetworkBehaviour
{
    [Header("Cámara normal del jugador")]
    [SerializeField] private Camera playerCamera;

    [Header("Cámara de seguridad")]
    [SerializeField] private Camera securityCamera;

    [Header("Controles")]
    [SerializeField] private KeyCode keyCamera1 = KeyCode.F1;
    [SerializeField] private KeyCode keyCamera2 = KeyCode.F2;
    [SerializeField] private KeyCode keyCamera3 = KeyCode.F3;
    [SerializeField] private KeyCode keyCamera4 = KeyCode.F4;

    private RoleManager roleManager;
    private int activeCamera = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (securityCamera != null)
                securityCamera.gameObject.SetActive(false);

            enabled = false;
            return;
        }

        roleManager = RoleManager.Instance;

        if (securityCamera != null)
            securityCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (roleManager == null)
            roleManager = RoleManager.Instance;

        if (roleManager == null || !roleManager.IsSpawned)
            return;

        RoleManager.Role myRole =
            roleManager.GetRole(OwnerClientId);

        // Solo el rol rojo puede utilizar cámaras.
        if (myRole != RoleManager.Role.Red)
        {
            if (activeCamera != 0)
                ReturnToPlayerCamera();

            return;
        }

        if (Input.GetKeyDown(keyCamera1))
            ToggleCamera(1);

        else if (Input.GetKeyDown(keyCamera2))
            ToggleCamera(2);

        else if (Input.GetKeyDown(keyCamera3))
            ToggleCamera(3);

        else if (Input.GetKeyDown(keyCamera4))
            ToggleCamera(4);
    }

    private void ToggleCamera(int number)
    {
        if (activeCamera == number)
        {
            ReturnToPlayerCamera();
            return;
        }

        SecurityCameraPoints points =
            SecurityCameraPoints.Instance;

        if (points == null)
        {
            Debug.LogWarning(
                "No existe SecurityCameraPoints en la escena."
            );
            return;
        }

        Transform point = points.GetCameraPoint(number);

        if (point == null || securityCamera == null)
        {
            Debug.LogWarning(
                "Falta configurar la cámara de seguridad " + number
            );
            return;
        }

        // Colocar la cámara secundaria
        // en el punto de seguridad elegido.
        securityCamera.transform.SetPositionAndRotation(
            point.position,
            point.rotation
        );

        // Desactivar cámara normal.
        if (playerCamera != null)
            playerCamera.enabled = false;

        // Activar cámara secundaria.
        securityCamera.gameObject.SetActive(true);
        securityCamera.enabled = true;

        activeCamera = number;

        Debug.Log("Viendo cámara de seguridad " + number);
    }

    private void ReturnToPlayerCamera()
    {
        activeCamera = 0;

        if (securityCamera != null)
            securityCamera.gameObject.SetActive(false);

        if (playerCamera != null)
            playerCamera.enabled = true;

        Debug.Log("Volviendo a tercera persona.");
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            ReturnToPlayerCamera();
    }

    private void OnDisable()
    {
        if (IsOwner && activeCamera != 0)
            ReturnToPlayerCamera();
    }
}
