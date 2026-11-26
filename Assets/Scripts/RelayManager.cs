
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [Header("Configuracion de conexion")]
    [SerializeField] private bool usarRelay = true;

    [Header("Conexion local")]
    [SerializeField] private ushort puerto = 7777;

    public bool UsarRelay => usarRelay;

    // =====================================
    // CREAR HOST
    // =====================================

    public async Task<string> StartHostWithRelay(
        int maxConnections,
        string connectionType)
    {
        if (!usarRelay)
        {
            Debug.Log("Iniciando Host local...");

            var transport = NetworkManager.Singleton
                .GetComponent<UnityTransport>();

            transport.SetConnectionData(
                "0.0.0.0",
                puerto
            );

            return NetworkManager.Singleton.StartHost()
                ? "LOCAL"
                : null;
        }

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance
            .CreateAllocationAsync(maxConnections);

        var unityTransport = NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        unityTransport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(
                allocation,
                connectionType
            )
        );

        var joinCode = await RelayService.Instance
            .GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log("Codigo Relay: " + joinCode);

        return NetworkManager.Singleton.StartHost()
            ? joinCode
            : null;
    }

    // =====================================
    // UNIR CLIENTE
    // =====================================

    public async Task<bool> StartClientWithRelay(
        string joinCode,
        string connectionType)
    {
        if (!usarRelay)
        {
            Debug.Log("Iniciando cliente local...");

            var transport = NetworkManager.Singleton
                .GetComponent<UnityTransport>();

            transport.SetConnectionData(
                "127.0.0.1",
                puerto
            );

            return NetworkManager.Singleton.StartClient();
        }

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance
            .JoinAllocationAsync(joinCode);

        var unityTransport = NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        unityTransport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(
                allocation,
                connectionType
            )
        );

        return NetworkManager.Singleton.StartClient();
    }
}
