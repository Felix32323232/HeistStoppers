using UnityEngine;
using Unity.Netcode;

/// < summary >
/// Connection Approval Handler Component
/// < /summary >
/// < remarks >
/// This should be placed on the same GameObject as the NetworkManager.
/// It automatically declines the client connection for example purposes.
/// < /remarks >
public class ConnectionApprovalHandler : MonoBehaviour
{
    private NetworkManager m_NetworkManager;
    [SerializeField]
    public int maxPlayers = 4;
    private int currentPlayers = 0;

    private void Start()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        m_NetworkManager.NetworkConfig.ConnectionApproval = true; //Enable ConnectionApproval
        if (m_NetworkManager != null)
        {
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_NetworkManager.ConnectionApprovalCallback = ApprovalCheck;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // Your approval logic determines the following values
        if (maxPlayers > currentPlayers)
        {
            response.Approved = true;
            currentPlayers++;
        }
        else
            response.Approved = false;

        response.CreatePlayerObject = true;

        // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        response.Reason = "The room is full";

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }

    private void OnClientDisconnectCallback(ulong obj)
    {
        if (!m_NetworkManager.IsServer && m_NetworkManager.DisconnectReason != string.Empty)
        {
            Debug.Log($"Approval Declined Reason: {m_NetworkManager.DisconnectReason}");
        }
    }
}