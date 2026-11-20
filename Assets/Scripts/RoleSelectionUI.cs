
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class RoleSelectionUI : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelLobby;
    [SerializeField] private GameObject panelSeleccionRol;
    [SerializeField] private GameObject panelListo;

    [Header("Botones de roles")]
    [SerializeField] private Button botonRojo;
    [SerializeField] private Button botonAzul;
    [SerializeField] private Button botonVerde;
    [SerializeField] private Button botonAmarillo;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private TMP_Text estadoSala;
    [SerializeField] private TMP_Text textoListo;

    [Header("Configuración")]
    [SerializeField, Min(1)]
    private int jugadoresMinimos = 2;

    [SerializeField] private KeyCode teclaListo = KeyCode.Space;

    private RoleManager roleManager;
    private bool interfazIniciada;
    private ulong ultimoClientId;

    private void Awake()
    {
        if (botonRojo != null)
            botonRojo.onClick.AddListener(
                () => SeleccionarRol(RoleManager.Role.Red));

        if (botonAzul != null)
            botonAzul.onClick.AddListener(
                () => SeleccionarRol(RoleManager.Role.Blue));

        if (botonVerde != null)
            botonVerde.onClick.AddListener(
                () => SeleccionarRol(RoleManager.Role.Green));

        if (botonAmarillo != null)
            botonAmarillo.onClick.AddListener(
                () => SeleccionarRol(RoleManager.Role.Yellow));
    }

    private void Start()
    {
        panelSeleccionRol.SetActive(false);
        panelListo.SetActive(false);
    }

    private void Update()
    {
        NetworkManager network = NetworkManager.Singleton;

        if (network == null ||
            !network.IsClient ||
            !network.IsConnectedClient)
        {
            if (interfazIniciada)
                ReiniciarInterfaz();

            return;
        }

        if (roleManager == null)
            roleManager = RoleManager.Instance;

        if (roleManager == null || !roleManager.IsSpawned)
            return;

        ulong localId = network.LocalClientId;

        if (!interfazIniciada || ultimoClientId != localId)
        {
            interfazIniciada = true;
            ultimoClientId = localId;

            if (panelLobby != null)
                panelLobby.SetActive(false);
        }

        RoleManager.Role myRole =
            roleManager.GetRole(localId);

        bool ready =
            roleManager.IsPlayerReady(localId);

        if (ready && roleManager.gameStarted.Value)
        {
            ActualizarPanelListo(ready);
            OcultarPaneles();
            return;
        }

        // Si todavía no hemos elegido un rol,
        // mostramos el panel de selección.
        if (myRole == RoleManager.Role.None)
        {
            MostrarSeleccionRol();

            ActualizarBotones(myRole);
            DetectarTeclasRol();

            if (textoEstado != null)
            {
                textoEstado.text =
                    "ELIGE TU ROL\n" +
                    "1 - ROJO\n" +
                    "2 - AZUL\n" +
                    "3 - VERDE\n" +
                    "4 - AMARILLO";
            }

            return;
        }

        // Comprobar si todos los jugadores
        // conectados han elegido un rol.
        bool todosConRol = true;

        foreach (var player in roleManager.players)
        {
            if (player.SelectedRole == RoleManager.Role.None)
            {
                todosConRol = false;
                break;
            }
        }

        bool hayMinimoJugadores =
            roleManager.players.Count >= jugadoresMinimos;

        // Si la partida ya ha comenzado,
        // los nuevos jugadores pueden pasar
        // directamente al panel Listo.
        bool puedePasarAListo =
            roleManager.gameStarted.Value ||
            (hayMinimoJugadores && todosConRol);

        if (!puedePasarAListo)
        {
            MostrarSeleccionRol();

            ActualizarBotones(myRole);

            // Permite cambiar a otro rol libre
            // mientras esperamos a los demás.
            DetectarTeclasRol();

            if (textoEstado != null)
            {
                textoEstado.text =
                    "TU ROL: " + NombreRol(myRole) +
                    "\n\nEsperando a los demás..." +
                    "\nPulsa 1, 2, 3 o 4 para cambiar.";
            }

            return;
        }

        // Todos tienen rol: mostrar Listo.
        MostrarPanelListo();

        ActualizarPanelListo(ready);

        if (!ready && Input.GetKeyDown(teclaListo))
        {
            roleManager.SetReady(true);
        }
    }

    // =====================================
    // CONTROLES DE TECLADO
    // =====================================

    private void DetectarTeclasRol()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Keypad1))
        {
            SeleccionarRol(RoleManager.Role.Red);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) ||
                 Input.GetKeyDown(KeyCode.Keypad2))
        {
            SeleccionarRol(RoleManager.Role.Blue);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) ||
                 Input.GetKeyDown(KeyCode.Keypad3))
        {
            SeleccionarRol(RoleManager.Role.Green);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) ||
                 Input.GetKeyDown(KeyCode.Keypad4))
        {
            SeleccionarRol(RoleManager.Role.Yellow);
        }
    }

    // =====================================
    // SELECCIÓN DE ROL
    // =====================================

    public void SeleccionarRol(RoleManager.Role role)
    {
        if (roleManager == null)
            return;

        ulong localId =
            NetworkManager.Singleton.LocalClientId;

        // No enviar solicitudes para roles ocupados.
        if (!roleManager.IsRoleAvailable(role))
            return;

        // No cambiar de rol mientras estamos listos.
        if (roleManager.IsPlayerReady(localId))
            return;

        roleManager.SelectRole(role);
    }

    // =====================================
    // PANELES
    // =====================================

    private void MostrarSeleccionRol()
    {
        if (panelSeleccionRol != null &&
            !panelSeleccionRol.activeSelf)
        {
            panelSeleccionRol.SetActive(true);
        }

        if (panelListo != null &&
            panelListo.activeSelf)
        {
            panelListo.SetActive(false);
        }
    }

    private void MostrarPanelListo()
    {
        if (panelSeleccionRol != null &&
            panelSeleccionRol.activeSelf)
        {
            panelSeleccionRol.SetActive(false);
        }

        if (panelListo != null &&
            !panelListo.activeSelf)
        {
            panelListo.SetActive(true);
        }
    }

    private void OcultarPaneles()
    {
        if (panelSeleccionRol != null)
            panelSeleccionRol.SetActive(false);

        if (panelListo != null)
            panelListo.SetActive(false);
    }

    private void ReiniciarInterfaz()
    {
        interfazIniciada = false;
        roleManager = null;

        OcultarPaneles();
    }

    // =====================================
    // ACTUALIZAR BOTONES
    // =====================================

    private void ActualizarBotones(RoleManager.Role myRole)
    {
        ActualizarBoton(
            botonRojo,
            RoleManager.Role.Red,
            myRole);

        ActualizarBoton(
            botonAzul,
            RoleManager.Role.Blue,
            myRole);

        ActualizarBoton(
            botonVerde,
            RoleManager.Role.Green,
            myRole);

        ActualizarBoton(
            botonAmarillo,
            RoleManager.Role.Yellow,
            myRole);
    }

    private void ActualizarBoton(
        Button boton,
        RoleManager.Role role,
        RoleManager.Role myRole)
    {
        if (boton == null)
            return;

        bool esMio = myRole == role;
        bool disponible = roleManager.IsRoleAvailable(role);

        boton.interactable = disponible && !esMio;

        TMP_Text texto =
            boton.GetComponentInChildren<TMP_Text>();

        if (texto == null)
            return;

        string nombre = NombreRol(role);

        if (esMio)
            texto.text = nombre + "\n(TÚ)";
        else if (!disponible)
            texto.text = nombre + "\n(OCUPADO)";
        else
            texto.text = nombre + "\n(DISPONIBLE)";
    }

    // =====================================
    // PANEL LISTO
    // =====================================

    private void ActualizarPanelListo(bool ready)
    {
        int preparados = 0;
        int total = roleManager.players.Count;

        foreach (var player in roleManager.players)
        {
            if (player.Ready)
                preparados++;
        }

        if (estadoSala != null)
        {
            estadoSala.text =
                $"Jugadores preparados: {preparados}/{total}";
        }

        if (textoListo != null)
        {
            if (ready)
            {
                textoListo.text =
                    "¡LISTO!\n" +
                    "Esperando a los demás jugadores...";
            }
            else
            {
                textoListo.text =
                    "PULSA ESPACIO PARA ESTAR LISTO";
            }
        }
    }

    // =====================================
    // NOMBRES DE ROLES
    // =====================================

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

    private void OnDestroy()
    {
        if (botonRojo != null)
            botonRojo.onClick.RemoveAllListeners();

        if (botonAzul != null)
            botonAzul.onClick.RemoveAllListeners();

        if (botonVerde != null)
            botonVerde.onClick.RemoveAllListeners();

        if (botonAmarillo != null)
            botonAmarillo.onClick.RemoveAllListeners();
    }
}
