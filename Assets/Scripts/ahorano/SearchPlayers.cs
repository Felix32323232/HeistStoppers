
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SearchPlayers : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [Header("Material de resaltado")]
    [SerializeField] private Material highlightMaterial;

    [Header("Colores por rol")]
    [SerializeField] private Color colorRojo = Color.red;
    [SerializeField] private Color colorAzul = Color.blue;
    [SerializeField] private Color colorVerde = Color.green;
    [SerializeField] private Color colorAmarillo = Color.yellow;

    private bool tracking = false;
    private InteractableGenerator generator;
    private RoleManager roleManager;

    private class HighlightVisual
    {
        public Renderer[] renderers;
        public Material[][] originalMaterials;
        public Material materialInstance;
        public RoleManager.Role role;
    }

    private Dictionary<ulong, HighlightVisual> highlightedPlayers =
        new Dictionary<ulong, HighlightVisual>();

    private Dictionary<int, HighlightVisual> highlightedInteractables =
        new Dictionary<int, HighlightVisual>();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        generator = FindFirstObjectByType<InteractableGenerator>();
        roleManager = RoleManager.Instance;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (roleManager == null)
            roleManager = RoleManager.Instance;

        if (Input.GetKeyDown(toggleKey))
        {
            tracking = !tracking;

            if (!tracking)
            {
                HideAllPlayers();
                HideAllInteractables();

                Debug.Log("Visión especial desactivada.");
            }
            else
            {
                Debug.Log("Visión especial activada.");
            }
        }

        if (!tracking || roleManager == null ||
            !roleManager.IsSpawned)
            return;

        UpdatePlayers();
        UpdateInteractables();
    }

    // =====================================
    // JUGADORES
    // =====================================

    private void UpdatePlayers()
    {
        if (NetworkManager.Singleton == null)
            return;

        HashSet<ulong> activePlayers = new HashSet<ulong>();

        foreach (NetworkClient client in
                 NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null || !playerObject.IsSpawned)
                continue;

            // No resaltar nuestro propio personaje.
            if (playerObject.OwnerClientId == OwnerClientId)
                continue;

            ulong id = playerObject.OwnerClientId;

            RoleManager.Role playerRole =
                roleManager.GetRole(id);

            if (playerRole == RoleManager.Role.None)
                continue;

            activePlayers.Add(id);

            // Si ya está resaltado, comprobar
            // si ha cambiado de rol.
            if (highlightedPlayers.TryGetValue(
                id, out HighlightVisual existing))
            {
                if (existing.role == playerRole)
                    continue;

                RestoreHighlight(existing);
                highlightedPlayers.Remove(id);
            }

            Renderer[] renderers =
                playerObject.GetComponentsInChildren<Renderer>(true);

            HighlightVisual visual = CreateHighlight(
                renderers,
                playerRole
            );

            if (visual != null)
                highlightedPlayers.Add(id, visual);
        }

        // Eliminar resaltados de jugadores
        // desconectados o sin rol.
        List<ulong> toRemove = new List<ulong>();

        foreach (var pair in highlightedPlayers)
        {
            if (!activePlayers.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (ulong id in toRemove)
        {
            RestoreHighlight(highlightedPlayers[id]);
            highlightedPlayers.Remove(id);
        }
    }

    // =====================================
    // INTERACTUABLES
    // =====================================

    private void UpdateInteractables()
    {
        if (generator == null)
        {
            generator = FindFirstObjectByType<InteractableGenerator>();

            if (generator == null)
                return;
        }

        if (NetworkManager.Singleton == null)
            return;

        ulong localId =
            NetworkManager.Singleton.LocalClientId;

        RoleManager.Role myRole =
            roleManager.GetRole(localId);

        // Si todavía no tenemos rol,
        // eliminar cualquier resaltado anterior.
        if (myRole == RoleManager.Role.None)
        {
            HideAllInteractables();
            return;
        }

        HashSet<int> activePoints = new HashSet<int>();

        Interactable[] tasks =
            FindObjectsByType<Interactable>(
                FindObjectsSortMode.None
            );

        foreach (Interactable task in tasks)
        {
            if (!task.IsSpawned)
                continue;

            // Solo mostrar tareas de nuestro rol.
            if (task.taskId.Value != (ulong)myRole)
                continue;

            int index = task.spawnPointIndex.Value;

            if (index < 0)
                continue;

            InteractableSpawnPoint point =
                generator.GetSpawnPoint(index);

            if (point == null)
                continue;

            activePoints.Add(index);

            if (highlightedInteractables.TryGetValue(
                index, out HighlightVisual existing))
            {
                // Si el rol no ha cambiado,
                // mantener el resaltado.
                if (existing.role == myRole)
                    continue;

                RestoreHighlight(existing);
                highlightedInteractables.Remove(index);
            }

            HighlightVisual visual = CreateHighlight(
                point.ObjectRenderers,
                myRole
            );

            if (visual != null)
            {
                highlightedInteractables.Add(index, visual);

                Debug.Log(
                    "Tarea resaltada: " + point.name +
                    " | Rol: " + myRole
                );
            }
        }

        // Restaurar máquinas cuya tarea
        // terminó o ya no pertenece a nuestro rol.
        List<int> toRemove = new List<int>();

        foreach (var pair in highlightedInteractables)
        {
            if (!activePoints.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (int index in toRemove)
        {
            RestoreHighlight(
                highlightedInteractables[index]
            );

            highlightedInteractables.Remove(index);
        }
    }

    // =====================================
    // MATERIALES
    // =====================================

    private HighlightVisual CreateHighlight(
        Renderer[] renderers,
        RoleManager.Role role)
    {
        if (highlightMaterial == null ||
            renderers == null ||
            renderers.Length == 0)
        {
            return null;
        }

        HighlightVisual visual = new HighlightVisual();

        visual.renderers = renderers;
        visual.originalMaterials =
            new Material[renderers.Length][];

        visual.role = role;

        // Crear una instancia independiente
        // del material de resaltado.
        visual.materialInstance =
            new Material(highlightMaterial);

        Color color = GetRoleColor(role);

        visual.materialInstance.color = color;

        // Compatibilidad con materiales URP.
        if (visual.materialInstance.HasProperty("_BaseColor"))
        {
            visual.materialInstance.SetColor(
                "_BaseColor",
                color
            );
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];

            if (rend == null)
                continue;

            visual.originalMaterials[i] =
                rend.sharedMaterials;

            Material[] newMaterials =
                new Material[
                    visual.originalMaterials[i].Length
                ];

            for (int j = 0; j < newMaterials.Length; j++)
            {
                newMaterials[j] =
                    visual.materialInstance;
            }

            rend.sharedMaterials = newMaterials;
        }

        return visual;
    }

    private Color GetRoleColor(RoleManager.Role role)
    {
        switch (role)
        {
            case RoleManager.Role.Red:
                return colorRojo;

            case RoleManager.Role.Blue:
                return colorAzul;

            case RoleManager.Role.Green:
                return colorVerde;

            case RoleManager.Role.Yellow:
                return colorAmarillo;

            default:
                return Color.white;
        }
    }

    private void RestoreHighlight(HighlightVisual visual)
    {
        if (visual == null)
            return;

        for (int i = 0; i < visual.renderers.Length; i++)
        {
            Renderer rend = visual.renderers[i];

            if (rend == null ||
                visual.originalMaterials[i] == null)
                continue;

            rend.sharedMaterials =
                visual.originalMaterials[i];
        }

        if (visual.materialInstance != null)
            Destroy(visual.materialInstance);
    }

    // =====================================
    // LIMPIEZA
    // =====================================

    private void HideAllPlayers()
    {
        foreach (var pair in highlightedPlayers)
            RestoreHighlight(pair.Value);

        highlightedPlayers.Clear();
    }

    private void HideAllInteractables()
    {
        foreach (var pair in highlightedInteractables)
            RestoreHighlight(pair.Value);

        highlightedInteractables.Clear();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;

        HideAllPlayers();
        HideAllInteractables();

        tracking = false;
    }

    private void OnDestroy()
    {
        HideAllPlayers();
        HideAllInteractables();
    }
}
