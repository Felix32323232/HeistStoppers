using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Relay")]
    [SerializeField] RelayManager relayManager;
    [SerializeField] string connectionType = "wss";
    [Header("UI")]
    [SerializeField] GameObject initButtons;
    [SerializeField] GameObject joinButtons;
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button backButton;
    [SerializeField] Button joinPartyButton;
    [SerializeField] TMP_Text hostCode;
    private bool toggleButtons = false;

    void OnEnable()
    {
        hostButton.onClick.AddListener(OnStartHostClickAsync);
        joinPartyButton.onClick.AddListener(OnStartClientClickAsync);
        clientButton.onClick.AddListener(ToggleButtons);
        backButton.onClick.AddListener(ToggleButtons);
    }

    void OnDisable()
    {
        hostButton.onClick.RemoveAllListeners();
        joinPartyButton.onClick.RemoveAllListeners();
        clientButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    public async void OnStartHostClickAsync()
    {
        try
        {
            hostButton.enabled = false;
            initButtons.SetActive(false);
            int maxPlayers = NetworkManager.Singleton.GetComponent<ConnectionApprovalHandler>().maxPlayers;
            await relayManager.StartHostWithRelay(maxPlayers, connectionType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error trying to connect the host with Relay: {e}");
            hostButton.enabled = true;
            initButtons.SetActive(true);
        }

    }

    public async void OnStartClientClickAsync()
    {
        try
        {
            clientButton.enabled = false;
            joinButtons.SetActive(false);
            string cleanCode = hostCode.text.Trim().Replace("\u200B", "");
            Debug.Log($"Intentando conectar con código puro: '{cleanCode}' | Longitud: {cleanCode.Length}");
            await relayManager.StartClientWithRelay(cleanCode, connectionType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error trying to connect the client with Relay: {e}");
            clientButton.enabled = true;
            joinButtons.SetActive(true);
        }
    }

    public void OnStartServerClick()
    {
        NetworkManager.Singleton.StartServer();
    }

    private void ToggleButtons()
    {
        toggleButtons = !toggleButtons;
        initButtons.SetActive(!toggleButtons);
        joinButtons.SetActive(toggleButtons);

    }


}