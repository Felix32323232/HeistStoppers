using System.Collections;
using UnityEngine;
using Unity.Netcode;
public class InteractableGenerator : NetworkBehaviour
{
    public GameObject[] SpawnPoints;
    public GameObject Interactable_Prefab;
    public float tSpawnMin = 5;
    public float tSpawnMax = 5;
    public bool bucleInfinito = true;
    IEnumerator Spawns()
    {
        for (int i = 0; i < SpawnPoints.Length; i++)
        {

            GameObject cajaNueva = Instantiate(Interactable_Prefab, SpawnPoints[i].transform.position, SpawnPoints[i].transform.rotation);
            cajaNueva.GetComponent<NetworkObject>().Spawn();
            //habria que meterlos en una lista si queremos tenerlos mas localizados
            float espera = Random.Range(tSpawnMin, tSpawnMax);
            yield return new WaitForSeconds(espera);
        }
        
        yield return new WaitForSeconds(5); 
        Debug.Log("Resume Coroutine");
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (Interactable_Prefab == null)
        {
            Debug.LogError("[InteractableGenerator] Falta asignar Interactable_Prefab en el Inspector.");
            return;
        }

        if (SpawnPoints == null || SpawnPoints.Length == 0)
        {
            Debug.LogError("[InteractableGenerator] El array SpawnPoints está vacío.");
            return;
        }
        StartCoroutine(Spawns());
    }
}
