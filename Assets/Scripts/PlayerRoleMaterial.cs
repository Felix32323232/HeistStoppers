
using Unity.Netcode;
using UnityEngine;

public class PlayerRoleMaterial : NetworkBehaviour
{
    [Header("Renderers del personaje")]
    [SerializeField] private Renderer[] renderersPersonaje;

    [Header("Materiales por rol")]
    [SerializeField] private Material materialRojo;
    [SerializeField] private Material materialAzul;
    [SerializeField] private Material materialVerde;
    [SerializeField] private Material materialAmarillo;

    private RoleManager roleManager;
    private RoleManager.Role ultimoRol =
        RoleManager.Role.None;

    private bool materialAplicado = false;

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (roleManager == null)
            roleManager = RoleManager.Instance;

        if (roleManager == null || !roleManager.IsSpawned)
            return;

        // OwnerClientId identifica al jugador
        // al que pertenece este personaje.
        RoleManager.Role rolActual =
            roleManager.GetRole(OwnerClientId);

        // No cambiar material si todavía
        // no se ha seleccionado un rol.
        if (rolActual == RoleManager.Role.None)
            return;

        // Solo actualizar cuando cambie el rol
        // o todavía no se haya aplicado material.
        if (materialAplicado && ultimoRol == rolActual)
            return;

        AplicarMaterial(rolActual);

        ultimoRol = rolActual;
        materialAplicado = true;
    }

    private void AplicarMaterial(RoleManager.Role rol)
    {
        Material material = ObtenerMaterial(rol);

        if (material == null)
            return;

        foreach (Renderer renderer in renderersPersonaje)
        {
            if (renderer == null)
                continue;

            // Cambia todos los materiales
            // del renderer por el del rol.
            Material[] materiales = renderer.sharedMaterials;

            for (int i = 0; i < materiales.Length; i++)
            {
                materiales[i] = material;
            }

            renderer.sharedMaterials = materiales;
        }

        Debug.Log(
            $"Personaje {OwnerClientId}: " +
            $"material cambiado a {rol}"
        );
    }

    private Material ObtenerMaterial(RoleManager.Role rol)
    {
        switch (rol)
        {
            case RoleManager.Role.Red:
                return materialRojo;

            case RoleManager.Role.Blue:
                return materialAzul;

            case RoleManager.Role.Green:
                return materialVerde;

            case RoleManager.Role.Yellow:
                return materialAmarillo;

            default:
                return null;
        }
    }

    public override void OnNetworkDespawn()
    {
        ultimoRol = RoleManager.Role.None;
        materialAplicado = false;
        roleManager = null;
    }
}
