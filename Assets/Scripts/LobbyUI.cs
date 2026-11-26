
using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Relay")]
    [SerializeField] private RelayManager relayManager;
    [SerializeField] private string connectionType = "wss";

    [Header("Paneles")]
    [SerializeField] private GameObject initButtons;
    [SerializeField] private GameObject joinButtons;

    [Header("Botones")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button joinPartyButton;

    [Header("Codigo Relay")]
    [SerializeField] private TMP_Text codigoGenerado;
    [SerializeField] private TMP_InputField codigoIntroducido;

    [Header("Sonido Create Lobby")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip createLobbySound;
    [SerializeField] private float duracionSonido = 1f;

    private bool toggleButtons = false;
    private Coroutine rutinaSonido;

    private void OnEnable()
    {
        hostButton.onClick.AddListener(OnStartHostClickAsync);
        joinPartyButton.onClick.AddListener(OnStartClientClickAsync);
        clientButton.onClick.AddListener(ToggleButtons);
        backButton.onClick.AddListener(ToggleButtons);
    }

    private void OnDisable()
    {
        hostButton.onClick.RemoveListener(OnStartHostClickAsync);
        joinPartyButton.onClick.RemoveListener(OnStartClientClickAsync);
        clientButton.onClick.RemoveListener(ToggleButtons);
        backButton.onClick.RemoveListener(ToggleButtons);
    }

    // =====================================
    // CREAR HOST
    // =====================================

    public async void OnStartHostClickAsync()
    {
        hostButton.interactable = false;

        ReproducirSonidoCreateLobby();

        try
        {
            int maxPlayers = NetworkManager.Singleton
                .GetComponent<ConnectionApprovalHandler>()
                .maxPlayers;

            string code = await relayManager.StartHostWithRelay(
                Mathf.Max(1, maxPlayers - 1),
                connectionType
            );

            if (string.IsNullOrEmpty(code) ||
                !NetworkManager.Singleton.IsHost)
            {
                throw new Exception("No se pudo iniciar el Host.");
            }

            if (relayManager.UsarRelay)
                codigoGenerado.text = "Codigo Relay: " + code;
            else
                codigoGenerado.text = "Partida local - Sin Relay";

            codigoGenerado.gameObject.SetActive(true);

            Debug.Log("Host iniciado correctamente: " + code);

            OcultarPaneles();
        }
        catch (Exception e)
        {
            Debug.LogError("Error creando partida: " + e);
            hostButton.interactable = true;
        }
    }

    // =====================================
    // UNIR CLIENTE
    // =====================================

    public async void OnStartClientClickAsync()
    {
        joinPartyButton.interactable = false;

        try
        {
            string code = codigoIntroducido.text
                .Trim()
                .ToUpperInvariant();

            if (relayManager.UsarRelay && string.IsNullOrEmpty(code))
            {
                throw new Exception("Introduce un codigo Relay.");
            }

            bool started = await relayManager.StartClientWithRelay(
                code,
                connectionType
            );

            if (!started)
            {
                throw new Exception("No se pudo iniciar el cliente.");
            }

            // Esperar confirmacion de conexion.
            float timeout = Time.realtimeSinceStartup + 20f;

            while (!NetworkManager.Singleton.IsConnectedClient &&
                   Time.realtimeSinceStartup < timeout)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                throw new Exception(
                    "No se pudo conectar con el Host."
                );
            }

            Debug.Log(
                relayManager.UsarRelay
                    ? "Cliente conectado mediante Relay."
                    : "Cliente conectado mediante conexion local."
            );

            OcultarPaneles();
        }
        catch (Exception e)
        {
            Debug.LogError("Error uniendose: " + e);
            joinPartyButton.interactable = true;
        }
    }

    // =====================================
    // SONIDO
    // =====================================

    private void ReproducirSonidoCreateLobby()
    {
        if (audioSource == null || createLobbySound == null)
            return;

        if (rutinaSonido != null)
        {
            StopCoroutine(rutinaSonido);
            rutinaSonido = null;
        }

        audioSource.Stop();
        audioSource.clip = createLobbySound;
        audioSource.loop = false;
        audioSource.Play();

        rutinaSonido = StartCoroutine(
            DetenerSonidoDespuesDeUnSegundo()
        );
    }

    private IEnumerator DetenerSonidoDespuesDeUnSegundo()
    {
        yield return new WaitForSecondsRealtime(duracionSonido);

        if (audioSource != null)
            audioSource.Stop();

        rutinaSonido = null;
    }

    // =====================================
    // PANELES
    // =====================================

    private void OcultarPaneles()
    {
        initButtons.SetActive(false);
        joinButtons.SetActive(false);
    }

    private void ToggleButtons()
    {
        toggleButtons = !toggleButtons;

        initButtons.SetActive(!toggleButtons);
        joinButtons.SetActive(toggleButtons);
    }
}
