using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class InteractableGenerator : NetworkBehaviour
{
    public Transform[] spawnPoints;
    public GameObject[] interactablePrefabs;   // un prefab por tipo: Hold, SkillCheck, Mash
    public float tSpawnMin = 5f;
    public float tSpawnMax = 5f;
    public bool bucleInfinito = true;

    NetworkObject[] ocupantes;   // qué caja hay ahora en cada punto
    Coroutine routine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (interactablePrefabs == null || interactablePrefabs.Length == 0)
        {
            Debug.LogError("[InteractableGenerator] Faltan prefabs en interactablePrefabs.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[InteractableGenerator] El array spawnPoints está vacío.");
            return;
        }

        ocupantes = new NetworkObject[spawnPoints.Length];
        routine = StartCoroutine(Spawns());
    }

    public override void OnNetworkDespawn()
    {
        if (routine != null) StopCoroutine(routine);
    }

    IEnumerator Spawns()
    {
        yield return new WaitUntil(() => LobbyManager.Instance != null && LobbyManager.Instance.unlocked.Value);
        do
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                // si la caja de este punto sigue en pie, no spawneamos otra
                if (ocupantes[i] != null && ocupantes[i].IsSpawned) continue;

                var prefab = interactablePrefabs[Random.Range(0, interactablePrefabs.Length)];
                var go = Instantiate(prefab, spawnPoints[i].position, spawnPoints[i].rotation);
                var netObj = go.GetComponent<NetworkObject>();
                // aquí asignarías requiredPlayerId antes del Spawn
                netObj.Spawn();
                ocupantes[i] = netObj;

                yield return new WaitForSeconds(Random.Range(tSpawnMin, tSpawnMax));
            }

            // pausa entre pasadas para no recorrer los puntos cada frame
            yield return new WaitForSeconds(1f);
        }
        while (bucleInfinito);
    }
}