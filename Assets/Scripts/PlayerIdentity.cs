using UnityEngine;
using Unity.Netcode;
public class PlayerIdentity : NetworkBehaviour
{
    //public int playerId;
    public NetworkVariable<int> playerId = new NetworkVariable<int>(0);
}