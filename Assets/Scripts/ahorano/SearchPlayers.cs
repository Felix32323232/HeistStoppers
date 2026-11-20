
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SearchPlayers : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [Header("Resaltado de jugadores")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("Resaltado de interactuables")]
    [SerializeField] private Material interactableHighlightMaterial;
    [SerializeField] private Color interactableHighlightColor = Color.yellow;

    private bool tracking = false;
    private InteractableGenerator generator;

    private class HighlightVisual
    {
        public Renderer[] renderers;
        public Material[][] originalMaterials;
        public Material materialInstance;
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
    }

    private void Update()
    {
        if (!IsOwner)
            return;

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

        if (!tracking)
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

            if (playerObject == null)
                continue;

            if (playerObject.OwnerClientId == OwnerClientId)
                continue;

            ulong id = playerObject.OwnerClientId;
            activePlayers.Add(id);

            if (!highlightedPlayers.ContainsKey(id))
            {
                Renderer[] renderers =
                    playerObject.GetComponentsInChildren<Renderer>(true);

                HighlightVisual visual = CreateHighlight(
                    renderers,
                    highlightMaterial,
                    highlightColor
                );

                if (visual != null)
                    highlightedPlayers.Add(id, visual);
            }
        }

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

        HashSet<int> activePoints = new HashSet<int>();

        Interactable[] tasks =
            FindObjectsByType<Interactable>(FindObjectsSortMode.None);

        foreach (Interactable task in tasks)
        {
            if (!task.IsSpawned)
                continue;

            // Solo tareas asignadas a este jugador.
            if (task.taskId.Value != OwnerClientId)
                continue;

            int index = task.spawnPointIndex.Value;

            if (index < 0)
                continue;

            InteractableSpawnPoint point =
                generator.GetSpawnPoint(index);

            if (point == null)
                continue;

            activePoints.Add(index);

            if (highlightedInteractables.ContainsKey(index))
                continue;

            HighlightVisual visual = CreateHighlight(
                point.ObjectRenderers,
                interactableHighlightMaterial,
                interactableHighlightColor
            );

            if (visual != null)
            {
                highlightedInteractables.Add(index, visual);

                Debug.Log(
                    "Interactuable resaltado: " + point.name
                );
            }
        }

        // Restaurar objetos cuyas tareas terminaron.
        List<int> toRemove = new List<int>();

        foreach (var pair in highlightedInteractables)
        {
            if (!activePoints.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (int index in toRemove)
        {
            RestoreHighlight(highlightedInteractables[index]);
            highlightedInteractables.Remove(index);
        }
    }

    // =====================================
    // MATERIALES
    // =====================================

    private HighlightVisual CreateHighlight(
        Renderer[] renderers,
        Material sourceMaterial,
        Color color)
    {
        if (sourceMaterial == null ||
            renderers == null ||
            renderers.Length == 0)
        {
            return null;
        }

        HighlightVisual visual = new HighlightVisual();

        visual.renderers = renderers;
        visual.originalMaterials = new Material[renderers.Length][];

        // Una instancia independiente para no modificar
        // el material original del proyecto.
        visual.materialInstance = new Material(sourceMaterial);
        visual.materialInstance.color = color;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];

            if (rend == null)
                continue;

            visual.originalMaterials[i] = rend.sharedMaterials;

            Material[] newMaterials =
                new Material[visual.originalMaterials[i].Length];

            for (int j = 0; j < newMaterials.Length; j++)
            {
                newMaterials[j] = visual.materialInstance;
            }

            rend.sharedMaterials = newMaterials;
        }

        return visual;
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

            rend.sharedMaterials = visual.originalMaterials[i];
        }

        if (visual.materialInstance != null)
            Destroy(visual.materialInstance);
    }

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
