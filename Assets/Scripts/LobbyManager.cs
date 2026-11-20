
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private int requiredPlayers = 2;

    [Header("Objetos")]
    [SerializeField] private GameObject door;

    [Header("Interfaz")]
    [SerializeField] private TMP_Text counterText;

    public NetworkVariable<int> connectedCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> unlocked = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        connectedCount.OnValueChanged += OnCountChanged;
        unlocked.OnValueChanged += OnUnlockedChanged;

        RefreshUI();
        ApplyDoor(unlocked.Value);

        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += HandleConnected;
        NetworkManager.OnClientDisconnectCallback += HandleDisconnected;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
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

    private void Update()
    {
        if (!IsServer || unlocked.Value)
            return;

        if (RoleManager.Instance == null ||
            !RoleManager.Instance.IsSpawned)
            return;

        connectedCount.Value =
            NetworkManager.ConnectedClientsIds.Count;

        // RoleManager comprueba que todos tienen rol
        // y han pulsado Listo.
        if (connectedCount.Value >= requiredPlayers &&
            RoleManager.Instance.gameStarted.Value)
        {
            unlocked.Value = true;

            Debug.Log(
                "Todos los jugadores están listos. " +
                "¡Partida iniciada!"
            );
        }
    }

    private void HandleConnected(ulong clientId)
    {
        UpdateConnectedCount();
    }

    private void HandleDisconnected(ulong clientId)
    {
        UpdateConnectedCount();
    }

    private void UpdateConnectedCount()
    {
        if (!IsServer)
            return;

        connectedCount.Value =
            NetworkManager.ConnectedClientsIds.Count;
    }

    private void OnCountChanged(int previous, int current)
    {
        RefreshUI();
    }

    private void OnUnlockedChanged(bool previous, bool current)
    {
        ApplyDoor(current);
    }

    private void RefreshUI()
    {
        if (counterText != null)
        {
            counterText.text =
                $"{connectedCount.Value}/{requiredPlayers}";
        }
    }

    private void ApplyDoor(bool open)
    {
        if (door != null)
            door.SetActive(!open);
    }
}
