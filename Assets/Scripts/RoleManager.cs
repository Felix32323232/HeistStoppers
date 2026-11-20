
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance { get; private set; }

    public enum Role
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4
    }

    public struct PlayerRoleData : INetworkSerializable,
        System.IEquatable<PlayerRoleData>
    {
        public ulong ClientId;
        public Role SelectedRole;
        public bool Ready;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref SelectedRole);
            serializer.SerializeValue(ref Ready);
        }

        public bool Equals(PlayerRoleData other)
        {
            return ClientId == other.ClientId &&
                   SelectedRole == other.SelectedRole &&
                   Ready == other.Ready;
        }
    }

    [Header("Configuración")]
    [SerializeField, Range(1, 4)]
    private int minimumPlayers = 2;

    public NetworkVariable<bool> gameStarted = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkList<PlayerRoleData> players;

    // Solicitudes pendientes: destinatario -> solicitante
    private Dictionary<ulong, ulong> swapRequests = new();

    public event System.Action<ulong> OnSwapRequestReceived;
    public event System.Action<bool> OnSwapRequestResult;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        players = new NetworkList<PlayerRoleData>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback +=
            HandleClientConnected;

        NetworkManager.OnClientDisconnectCallback +=
            HandleClientDisconnected;

        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            HandleClientConnected(id);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -=
                HandleClientConnected;

            NetworkManager.OnClientDisconnectCallback -=
                HandleClientDisconnected;
        }

        swapRequests.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        players?.Dispose();
    }

    // ====================================
    // CONEXIONES
    // ====================================

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        if (GetPlayerIndex(clientId) >= 0)
            return;

        players.Add(new PlayerRoleData
        {
            ClientId = clientId,
            SelectedRole = Role.None,
            Ready = false
        });

        Debug.Log("Jugador conectado: " + clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        int index = GetPlayerIndex(clientId);

        if (index >= 0)
            players.RemoveAt(index);

        swapRequests.Remove(clientId);

        List<ulong> toRemove = new();

        foreach (var request in swapRequests)
        {
            if (request.Value == clientId)
                toRemove.Add(request.Key);
        }

        foreach (ulong id in toRemove)
            swapRequests.Remove(id);

        Debug.Log("Jugador desconectado: " + clientId);
    }

    // ====================================
    // CONSULTAS
    // ====================================

    private int GetPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == clientId)
                return i;
        }

        return -1;
    }

    public Role GetRole(ulong clientId)
    {
        int index = GetPlayerIndex(clientId);

        if (index < 0)
            return Role.None;

        return players[index].SelectedRole;
    }

    public bool IsRoleAvailable(Role role)
    {
        if (role == Role.None)
            return false;

        if ((int)role < 1 || (int)role > 4)
            return false;

        foreach (var player in players)
        {
            if (player.SelectedRole == role)
                return false;
        }

        return true;
    }

    public ulong? GetRoleOwner(Role role)
    {
        foreach (var player in players)
        {
            if (player.SelectedRole == role)
                return player.ClientId;
        }

        return null;
    }

    public bool IsPlayerReady(ulong clientId)
    {
        int index = GetPlayerIndex(clientId);

        return index >= 0 && players[index].Ready;
    }

    // ====================================
    // SELECCIÓN DE ROL
    // ====================================

    public void SelectRole(Role role)
    {
        if (!IsSpawned)
            return;

        SelectRoleServerRpc(role);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectRoleServerRpc(
        Role role,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        int index = GetPlayerIndex(clientId);

        if (index < 0)
            return;

        if (!IsRoleAvailable(role))
            return;

        PlayerRoleData data = players[index];

        data.SelectedRole = role;

        // Antes de comenzar, cambiar de rol
        // cancela el estado Listo.
        // Durante la partida, conserva Listo.
        if (!gameStarted.Value)
            data.Ready = false;

        players[index] = data;

        Debug.Log(
            $"Jugador {clientId} eligió el rol {role}"
        );
    }

    // ====================================
    // LISTO
    // ====================================

    public void SetReady(bool ready)
    {
        if (!IsSpawned)
            return;

        SetReadyServerRpc(ready);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(
        bool ready,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        int index = GetPlayerIndex(clientId);

        if (index < 0)
            return;

        PlayerRoleData data = players[index];

        if (data.SelectedRole == Role.None)
            return;

        data.Ready = ready;

        players[index] = data;

        CheckStartConditions();
    }

    private void CheckStartConditions()
    {
        if (!IsServer || gameStarted.Value)
            return;

        if (players.Count < minimumPlayers)
            return;

        foreach (var player in players)
        {
            if (player.SelectedRole == Role.None ||
                !player.Ready)
            {
                return;
            }
        }

        gameStarted.Value = true;

        Debug.Log("¡Todos listos! Comienza la partida.");
    }

    // ====================================
    // SOLICITUDES DE INTERCAMBIO
    // ====================================

    public void RequestSwap(Role desiredRole)
    {
        if (!IsSpawned)
            return;

        RequestSwapServerRpc(desiredRole);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSwapServerRpc(
        Role desiredRole,
        ServerRpcParams rpcParams = default)
    {
        ulong requesterId =
            rpcParams.Receive.SenderClientId;

        if (GetRole(requesterId) == Role.None)
            return;

        ulong? targetId = GetRoleOwner(desiredRole);

        if (!targetId.HasValue ||
            targetId.Value == requesterId)
        {
            return;
        }

        ulong target = targetId.Value;

        // Una solicitud pendiente por destinatario.
        swapRequests[target] = requesterId;

        NotifySwapRequestClientRpc(
            requesterId,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { target }
                }
            }
        );
    }

    [ClientRpc]
    private void NotifySwapRequestClientRpc(
        ulong requesterId,
        ClientRpcParams rpcParams = default)
    {
        OnSwapRequestReceived?.Invoke(requesterId);

        Debug.Log(
            $"Solicitud de intercambio recibida de {requesterId}"
        );
    }

    // ====================================
    // RESPONDER SOLICITUD
    // ====================================

    public void RespondSwap(bool accept)
    {
        if (!IsSpawned)
            return;

        RespondSwapServerRpc(accept);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespondSwapServerRpc(
        bool accept,
        ServerRpcParams rpcParams = default)
    {
        ulong targetId =
            rpcParams.Receive.SenderClientId;

        if (!swapRequests.TryGetValue(
            targetId,
            out ulong requesterId))
        {
            return;
        }

        swapRequests.Remove(targetId);

        bool swapped = false;

        if (accept)
        {
            int targetIndex = GetPlayerIndex(targetId);
            int requesterIndex = GetPlayerIndex(requesterId);

            if (targetIndex >= 0 && requesterIndex >= 0)
            {
                PlayerRoleData target = players[targetIndex];
                PlayerRoleData requester = players[requesterIndex];

                if (target.SelectedRole != Role.None &&
                    requester.SelectedRole != Role.None)
                {
                    Role temp = target.SelectedRole;

                    target.SelectedRole = requester.SelectedRole;
                    requester.SelectedRole = temp;

                    // Los roles cambian, pero las misiones
                    // siguen perteneciendo a sus colores.
                    players[targetIndex] = target;
                    players[requesterIndex] = requester;

                    swapped = true;
                }
            }
        }

        NotifySwapResultClientRpc(
            swapped,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { requesterId }
                }
            }
        );
    }

    [ClientRpc]
    private void NotifySwapResultClientRpc(
        bool accepted,
        ClientRpcParams rpcParams = default)
    {
        OnSwapRequestResult?.Invoke(accepted);

        Debug.Log(
            accepted
                ? "Intercambio aceptado."
                : "Intercambio rechazado."
        );
    }
}
