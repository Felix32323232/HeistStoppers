using UnityEngine;
using Unity.Netcode;
public class PlayerIdentity : NetworkBehaviour
{
    //public int playerId;
    public NetworkVariable<int> playerId = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
           playerId.Value = (int)OwnerClientId+1;
        }
    }

}