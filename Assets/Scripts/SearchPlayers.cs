using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerTraSearchPlayerscker : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Color highlightColor = Color.cyan;

    private bool tracking = false;

    // Guarda los materiales originales de cada jugador.
    private class PlayerVisual
    {
        public Renderer[] renderers;
        public Material[][] originalMaterials;
        public Material highlightMaterial;
    }

    private System.Collections.Generic.Dictionary<
        ulong, PlayerVisual> highlightedPlayers =
        new System.Collections.Generic.Dictionary<
            ulong, PlayerVisual>();

    public override void OnNetworkSpawn()
    {
        // Solo el jugador local puede controlar este sistema.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(toggleKey))
        {
            tracking = !tracking;

            if (tracking)
                ShowAllPlayers();
            else
                HideAllPlayers();
        }

        // Actualiza las posiciones mientras está activado.
        if (tracking)
            ShowPlayerPositions();
    }

    private void ShowAllPlayers()
    {
        foreach (NetworkClient client
                 in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null) continue;

            // No aplicar el efecto a tu propio personaje.
            if (playerObject.OwnerClientId == OwnerClientId)
                continue;

            ApplyHighlight(playerObject);
        }

        Debug.Log("Seguimiento de jugadores activado.");
    }


    private void ApplyHighlight(NetworkObject playerObject)
    {
        ulong id = playerObject.OwnerClientId;

        if (highlightedPlayers.ContainsKey(id))
            return;

        if (highlightMaterial == null)
        {
            Debug.LogWarning("Asigna el material Highlight_Player en el Inspector.");
            return;
        }

        Renderer[] renderers =
            playerObject.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return;

        PlayerVisual visual = new PlayerVisual();
        visual.renderers = renderers;
        visual.originalMaterials = new Material[renderers.Length][];

        // Crear una copia para este jugador, sin modificar el material original.
        visual.highlightMaterial = new Material(highlightMaterial);
        visual.highlightMaterial.color = highlightColor;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];

            visual.originalMaterials[i] = rend.sharedMaterials;

            Material[] newMaterials =
                new Material[visual.originalMaterials[i].Length];

            for (int j = 0; j < newMaterials.Length; j++)
            {
                newMaterials[j] = visual.highlightMaterial;
            }

            rend.sharedMaterials = newMaterials;
        }

        highlightedPlayers.Add(id, visual);
    }


    private void ShowPlayerPositions()
    {
        foreach (NetworkClient client
                 in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null) continue;

            Vector3 pos = playerObject.transform.position;

            Debug.Log(
                $"Jugador {client.ClientId}: " +
                $"X={pos.x:F1}, Y={pos.y:F1}, Z={pos.z:F1}");
        }
    }

    private void HideAllPlayers()
    {
        foreach (var pair in highlightedPlayers)
        {
            PlayerVisual visual = pair.Value;

            for (int i = 0;
                 i < visual.renderers.Length; i++)
            {
                if (visual.renderers[i] != null)
                {
                    visual.renderers[i].sharedMaterials =
                        visual.originalMaterials[i];
                }
            }

            Destroy(visual.highlightMaterial);
        }

        highlightedPlayers.Clear();
        Debug.Log("Seguimiento de jugadores desactivado.");
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            HideAllPlayers();
    }
}

