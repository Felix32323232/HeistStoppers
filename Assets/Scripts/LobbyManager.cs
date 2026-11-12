using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] int requiredPlayers = 2;
    [SerializeField] bool keepOpenOnceUnlocked = true;
    [SerializeField] GameObject door;        // barrera con collider que cierra la sala
    [SerializeField] TMP_Text counterText;   // opcional: "1/2"

    public NetworkVariable<int> connectedCount = new(0); //num de jugadores online
    public NetworkVariable<bool> unlocked = new(false); //variable netcode

    readonly Dictionary<ulong, int> ids = new();

    void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        connectedCount.OnValueChanged += OnCountChanged;
        unlocked.OnValueChanged += OnUnlockedChanged;
        RefreshUI();
        ApplyDoor(unlocked.Value);   // para quien entra tarde

        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback += HandleConnected;
        NetworkManager.OnClientDisconnectCallback += HandleDisconnected;

        // el host (y cualquiera ya conectado) puede haberse conectado antes de que existiera el lobby
        foreach (var clientId in NetworkManager.ConnectedClientsIds)
            HandleConnected(clientId);
    }

    public override void OnNetworkDespawn()
    {
        connectedCount.OnValueChanged -= OnCountChanged;
        unlocked.OnValueChanged -= OnUnlockedChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleDisconnected;
        }
    }

    void HandleConnected(ulong clientId)
    {
        if (ids.ContainsKey(clientId)) return;

        int id = NextFreeId();
        ids[clientId] = id;
        StartCoroutine(AssignWhenReady(clientId, id));
        UpdateLobbyState();
    }

    void HandleDisconnected(ulong clientId)
    {
        if (ids.Remove(clientId))
            UpdateLobbyState();
    }

    // el id 0 queda como "sin asignar" (es el valor por defecto de la NetworkVariable)
    int NextFreeId()
    {
        int id = 1;
        while (ids.ContainsValue(id)) id++;
        return id;
    }

    IEnumerator AssignWhenReady(ulong clientId, int id)
    {
        // el PlayerObject puede tardar un instante en existir
        while (ids.ContainsKey(clientId))
        {
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)
                && client.PlayerObject != null)
         
            {
               
                yield break;
            }
            yield return null;
        }
    }

    void UpdateLobbyState()
    {
        connectedCount.Value = ids.Count;

        if (!unlocked.Value && ids.Count >= requiredPlayers)
            unlocked.Value = true;
        else if (unlocked.Value && !keepOpenOnceUnlocked && ids.Count < requiredPlayers)
            unlocked.Value = false;
    }

    void OnCountChanged(int _, int __) => RefreshUI();
    void OnUnlockedChanged(bool _, bool open) => ApplyDoor(open);

    void RefreshUI()
    {
        if (counterText != null)
            counterText.text = $"{connectedCount.Value}/{requiredPlayers}";
    }

    void ApplyDoor(bool open)
    {
        if (door != null) door.SetActive(!open);
    }
}