
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoleSwapUI : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelMenuESC;
    [SerializeField] private GameObject panelSolicitud;

    [Header("Botones de roles")]
    [SerializeField] private Button botonRojo;
    [SerializeField] private Button botonAzul;
    [SerializeField] private Button botonVerde;
    [SerializeField] private Button botonAmarillo;

    [Header("Botones de solicitud")]
    [SerializeField] private Button botonAceptar;
    [SerializeField] private Button botonRechazar;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoRolActual;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private TMP_Text textoSolicitud;

    [Header("Controles")]
    [SerializeField] private KeyCode teclaMenu = KeyCode.Escape;

    private RoleManager roleManager;
    private bool menuAbierto;
    private bool solicitudPendiente;
    private ulong solicitanteId;

    private void Awake()
    {
        panelMenuESC.SetActive(false);
        panelSolicitud.SetActive(false);

        botonRojo.onClick.AddListener(
            () => ElegirRol(RoleManager.Role.Red));

        botonAzul.onClick.AddListener(
            () => ElegirRol(RoleManager.Role.Blue));

        botonVerde.onClick.AddListener(
            () => ElegirRol(RoleManager.Role.Green));

        botonAmarillo.onClick.AddListener(
            () => ElegirRol(RoleManager.Role.Yellow));

        botonAceptar.onClick.AddListener(
            () => ResponderSolicitud(true));

        botonRechazar.onClick.AddListener(
            () => ResponderSolicitud(false));
    }

    private void Update()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (network == null ||
            !network.IsClient ||
            !network.IsConnectedClient)
        {
            DesconectarEventos();
            CerrarMenu();
            solicitudPendiente = false;
            return;
        }

        if (roleManager == null)
        {
            RoleManager nuevo = RoleManager.Instance;

            if (nuevo != null && nuevo.IsSpawned)
            {
                roleManager = nuevo;

                roleManager.OnSwapRequestReceived +=
                    RecibirSolicitud;

                roleManager.OnSwapRequestResult +=
                    RecibirResultado;
            }
        }

        if (roleManager == null || !roleManager.IsSpawned)
            return;

        ulong localId = network.LocalClientId;

        // El menú solo funciona cuando ya
        // hemos seleccionado rol y estamos listos.
        bool puedeAbrir =
            roleManager.gameStarted.Value &&
            roleManager.IsPlayerReady(localId) &&
            roleManager.GetRole(localId) !=
            RoleManager.Role.None;

        if (!puedeAbrir)
        {
            if (menuAbierto)
                CerrarMenu();

            return;
        }

        if (Input.GetKeyDown(teclaMenu))
        {
            if (menuAbierto)
                CerrarMenu();
            else
                AbrirMenu();
        }

        if (!menuAbierto)
            return;

        ActualizarInterfaz(localId);

        // Cambiar o solicitar rol con números.
        if (!solicitudPendiente)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) ||
                Input.GetKeyDown(KeyCode.Keypad1))
                ElegirRol(RoleManager.Role.Red);

            else if (Input.GetKeyDown(KeyCode.Alpha2) ||
                     Input.GetKeyDown(KeyCode.Keypad2))
                ElegirRol(RoleManager.Role.Blue);

            else if (Input.GetKeyDown(KeyCode.Alpha3) ||
                     Input.GetKeyDown(KeyCode.Keypad3))
                ElegirRol(RoleManager.Role.Green);

            else if (Input.GetKeyDown(KeyCode.Alpha4) ||
                     Input.GetKeyDown(KeyCode.Keypad4))
                ElegirRol(RoleManager.Role.Yellow);
        }

        // Aceptar o rechazar una solicitud.
        if (solicitudPendiente)
        {
            if (Input.GetKeyDown(KeyCode.Y))
                ResponderSolicitud(true);

            else if (Input.GetKeyDown(KeyCode.N))
                ResponderSolicitud(false);
        }
    }

    // =====================================
    // ABRIR / CERRAR
    // =====================================

    private void AbrirMenu()
    {
        menuAbierto = true;
        panelMenuESC.SetActive(true);

        ActualizarPanelSolicitud();
    }

    private void CerrarMenu()
    {
        menuAbierto = false;

        if (panelMenuESC != null)
            panelMenuESC.SetActive(false);
    }

    // =====================================
    // ELEGIR O SOLICITAR ROL
    // =====================================

    private void ElegirRol(RoleManager.Role role)
    {
        if (roleManager == null ||
            !menuAbierto ||
            solicitudPendiente)
            return;

        ulong localId =
            NetworkManager.Singleton.LocalClientId;

        RoleManager.Role miRol =
            roleManager.GetRole(localId);

        if (miRol == role)
        {
            textoEstado.text =
                "Ya tienes ese rol.";
            return;
        }

        if (roleManager.IsRoleAvailable(role))
        {
            // Cambio inmediato a rol libre.
            roleManager.SelectRole(role);

            textoEstado.text =
                "Cambiando al rol " + NombreRol(role);

            // SelectRole pone Ready en false.
            // El jugador deberá volver a
            // confirmar desde la selección.
            CerrarMenu();
        }
        else
        {
            // El rol está ocupado:
            // enviar solicitud de intercambio.
            roleManager.RequestSwap(role);

            textoEstado.text =
                "Solicitud enviada al jugador " +
                NombreRol(role);
        }
    }

    // =====================================
    // SOLICITUDES
    // =====================================

    private void RecibirSolicitud(ulong requesterId)
    {
        solicitanteId = requesterId;
        solicitudPendiente = true;

        // Abrir automáticamente el menú
        // para que el jugador vea la solicitud.
        AbrirMenu();

        ActualizarPanelSolicitud();

        Debug.Log(
            "Solicitud de intercambio recibida de " +
            requesterId
        );
    }

    private void ActualizarPanelSolicitud()
    {
        if (panelSolicitud == null)
            return;

        panelSolicitud.SetActive(solicitudPendiente);

        if (!solicitudPendiente ||
            roleManager == null)
            return;

        RoleManager.Role rolSolicitante =
            roleManager.GetRole(solicitanteId);

        textoSolicitud.text =
            "El jugador " +
            NombreRol(rolSolicitante) +
            " quiere intercambiar su rol contigo." +
            "\n\nY - ACEPTAR" +
            "\nN - RECHAZAR";
    }

    private void ResponderSolicitud(bool aceptar)
    {
        if (!solicitudPendiente ||
            roleManager == null)
            return;

        roleManager.RespondSwap(aceptar);

        solicitudPendiente = false;

        panelSolicitud.SetActive(false);

        textoEstado.text = aceptar
            ? "Intercambio aceptado."
            : "Intercambio rechazado.";
    }

    private void RecibirResultado(bool aceptado)
    {
        if (textoEstado == null)
            return;

        textoEstado.text = aceptado
            ? "¡Intercambio realizado!"
            : "Intercambio rechazado o cancelado.";

        Debug.Log(textoEstado.text);
    }

    // =====================================
    // ACTUALIZACIÓN VISUAL
    // =====================================

    private void ActualizarInterfaz(ulong localId)
    {
        RoleManager.Role miRol =
            roleManager.GetRole(localId);

        if (textoRolActual != null)
        {
            textoRolActual.text =
                "TU ROL: " + NombreRol(miRol);
        }

        ActualizarBoton(
            botonRojo, RoleManager.Role.Red, miRol);

        ActualizarBoton(
            botonAzul, RoleManager.Role.Blue, miRol);

        ActualizarBoton(
            botonVerde, RoleManager.Role.Green, miRol);

        ActualizarBoton(
            botonAmarillo, RoleManager.Role.Yellow, miRol);

        ActualizarPanelSolicitud();
    }

    private void ActualizarBoton(
        Button boton,
        RoleManager.Role role,
        RoleManager.Role miRol)
    {
        if (boton == null)
            return;

        bool esMio = role == miRol;
        bool disponible = roleManager.IsRoleAvailable(role);

        boton.interactable = !esMio && !solicitudPendiente;

        TMP_Text texto =
            boton.GetComponentInChildren<TMP_Text>();

        if (texto == null)
            return;

        string nombre = NombreRol(role);

        if (esMio)
            texto.text = nombre + "\n(TÚ)";
        else if (disponible)
            texto.text = nombre + "\n(LIBRE)";
        else
            texto.text = nombre + "\n(SOLICITAR)";
    }

    private string NombreRol(RoleManager.Role role)
    {
        switch (role)
        {
            case RoleManager.Role.Red:
                return "ROJO";

            case RoleManager.Role.Blue:
                return "AZUL";

            case RoleManager.Role.Green:
                return "VERDE";

            case RoleManager.Role.Yellow:
                return "AMARILLO";

            default:
                return "NINGUNO";
        }
    }

    // =====================================
    // LIMPIEZA
    // =====================================

    private void DesconectarEventos()
    {
        if (roleManager == null)
            return;

        roleManager.OnSwapRequestReceived -=
            RecibirSolicitud;

        roleManager.OnSwapRequestResult -=
            RecibirResultado;

        roleManager = null;
    }

    private void OnDestroy()
    {
        DesconectarEventos();

        botonRojo.onClick.RemoveAllListeners();
        botonAzul.onClick.RemoveAllListeners();
        botonVerde.onClick.RemoveAllListeners();
        botonAmarillo.onClick.RemoveAllListeners();

        botonAceptar.onClick.RemoveAllListeners();
        botonRechazar.onClick.RemoveAllListeners();
    }
}
