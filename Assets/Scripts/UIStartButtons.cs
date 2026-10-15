using Unity.Netcode;
using UnityEngine;

public class UIStartButtons : MonoBehaviour
{
    public void OnStartHostClick()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void OnStartServerClick()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void OnStartClientClick()
    {
        NetworkManager.Singleton.StartClient();
    }
}