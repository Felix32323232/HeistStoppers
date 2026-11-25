using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InteractableGenerator : NetworkBehaviour
{
    [Header("Objetos del escenario")]
    [SerializeField]
    private InteractableSpawnPoint[] spawnPoints;

    [Header("Prefabs de interacción")]
    [SerializeField]
    private GameObject[] interactablePrefabs;

    [Header("Pool")]
    [SerializeField, Min(1)]
    private int initialPoolSizePerPrefab = 3;

    [Header("Límite global")]
    [SerializeField, Min(1)]
    private int maxActiveTasks = 5;

    [Header("Tiempo entre generaciones")]
    [SerializeField, Min(0.1f)]
    private float tSpawnMin = 3f;

    [SerializeField, Min(0.1f)]
    private float tSpawnMax = 6f;

    [SerializeField]
    private bool bucleInfinito = true;

    private NetworkObject[] ocupantes;
    private Coroutine routine;

    // Pool: índice de prefab -> cola de objetos disponibles
    private List<Queue<NetworkObject>> pools;
    private readonly List<NetworkObject> allPooledObjects = new List<NetworkObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No hay Spawn Points asignados.");
            return;
        }

        if (interactablePrefabs == null || interactablePrefabs.Length == 0)
        {
            Debug.LogError("No hay prefabs de interacción.");
            return;
        }

        ocupantes = new NetworkObject[spawnPoints.Length];
        InicializarPool();
        routine = StartCoroutine(Spawns());
    }

    public override void OnNetworkDespawn()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (IsServer)
        {
            LimpiarPool();
        }
    }


    private void InicializarPool()
    {
        pools = new List<Queue<NetworkObject>>(interactablePrefabs.Length);

        for (int p = 0; p < interactablePrefabs.Length; p++)
        {
            Queue<NetworkObject> queue = new Queue<NetworkObject>();
            pools.Add(queue);

            if (interactablePrefabs[p] == null)
                continue;

            for (int i = 0; i < initialPoolSizePerPrefab; i++)
            {
                NetworkObject netObj = CrearInstanciaParaPool(p);
                if (netObj != null)
                    queue.Enqueue(netObj);
            }
        }
    }

    private NetworkObject CrearInstanciaParaPool(int prefabIndex)
    {
        GameObject prefab = interactablePrefabs[prefabIndex];
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        go.SetActive(false);

        if (!go.TryGetComponent(out Interactable task) ||
            !go.TryGetComponent(out NetworkObject netObj))
        {
            Debug.LogError($"El prefab {prefab.name} necesita Interactable y NetworkObject.");
            Destroy(go);
            return null;
        }

        task.SetPoolIndex(prefabIndex);
        allPooledObjects.Add(netObj);
        return netObj;
    }

    private NetworkObject ObtenerDelPool(int prefabIndex)
    {
        if (pools == null || prefabIndex < 0 || prefabIndex >= pools.Count)
            return null;

        Queue<NetworkObject> queue = pools[prefabIndex];

        while (queue.Count > 0)
        {
            NetworkObject candidate = queue.Dequeue();
            if (candidate != null && !candidate.IsSpawned)
                return candidate;
        }

        // Si el pool está vacío temporalmente, ampliamos con una instancia nueva
        return CrearInstanciaParaPool(prefabIndex);
    }

    private void DevolverAlPool(NetworkObject netObj)
    {
        if (netObj == null) return;

        if (netObj.IsSpawned)
            netObj.Despawn(false);

        netObj.gameObject.SetActive(false);

        if (netObj.TryGetComponent(out Interactable task))
        {
            task.spawnPointIndex.Value = -1;
            int poolIdx = task.PrefabPoolIndex;

            if (pools != null && poolIdx >= 0 && poolIdx < pools.Count)
            {
                if (!pools[poolIdx].Contains(netObj))
                    pools[poolIdx].Enqueue(netObj);
            }
        }
    }

    private void LimpiarPool()
    {
        foreach (NetworkObject obj in allPooledObjects)
        {
            if (obj != null)
            {
                if (obj.IsSpawned)
                    obj.Despawn(true);
                else
                    Destroy(obj.gameObject);
            }
        }

        allPooledObjects.Clear();
        pools?.Clear();
    }


    private IEnumerator Spawns()
    {
        yield return new WaitUntil(() =>
            LobbyManager.Instance != null &&
            LobbyManager.Instance.unlocked.Value);

        do
        {
            ActualizarTareasFinalizadas();

            if (ContarTareasActivas() < maxActiveTasks)
            {
                int index = ElegirObjetoDisponible();

                if (index != -1)
                    GenerarTarea(index);
            }

            float min = Mathf.Min(tSpawnMin, tSpawnMax);
            float max = Mathf.Max(tSpawnMin, tSpawnMax);

            yield return new WaitForSeconds(Random.Range(min, max));
        }
        while (bucleInfinito);

        while (true)
        {
            ActualizarTareasFinalizadas();
            yield return new WaitForSeconds(0.2f);
        }
    }

    private int ContarTareasActivas()
    {
        int count = 0;

        foreach (NetworkObject obj in ocupantes)
        {
            if (obj != null && obj.IsSpawned)
                count++;
        }

        return count;
    }

    private int ElegirObjetoDisponible()
    {
        List<int> disponibles = new List<int>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
                continue;

            if (ocupantes[i] != null)
                continue;

            if (spawnPoints[i].IsAvailable)
                disponibles.Add(i);
        }

        if (disponibles.Count == 0)
            return -1;

        return disponibles[Random.Range(0, disponibles.Count)];
    }

    private bool ElegirRol(out ulong selectedRole)
    {
        selectedRole = 0;

        if (RoleManager.Instance == null)
            return false;

        int minTasks = int.MaxValue;
        List<ulong> candidates = new List<ulong>();

        foreach (var player in RoleManager.Instance.players)
        {
            if (!player.Ready || player.SelectedRole == RoleManager.Role.None)
                continue;

            ulong roleId = (ulong)player.SelectedRole;
            int count = 0;

            foreach (NetworkObject obj in ocupantes)
            {
                if (obj == null || !obj.IsSpawned)
                    continue;

                if (obj.TryGetComponent(out Interactable task) &&
                    task.taskId.Value == roleId)
                {
                    count++;
                }
            }

            if (count < minTasks)
            {
                minTasks = count;
                candidates.Clear();
                candidates.Add(roleId);
            }
            else if (count == minTasks)
            {
                candidates.Add(roleId);
            }
        }

        if (candidates.Count == 0)
            return false;

        selectedRole = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private void GenerarTarea(int index)
    {
        if (!ElegirRol(out ulong roleId)) return;

        InteractableSpawnPoint point = spawnPoints[index];
        int prefabIndex = Random.Range(0, interactablePrefabs.Length);

        NetworkObject netObj = ObtenerDelPool(prefabIndex);
        if (netObj == null)
            return;

        Interactable task = netObj.GetComponent<Interactable>();

        // Posicionar y reactivar antes de spawnear en red
        netObj.transform.SetPositionAndRotation(
            point.InteractionPoint.position,
            point.InteractionPoint.rotation
        );
        netObj.gameObject.SetActive(true);

        task.taskId.Value = roleId;
        task.spawnPointIndex.Value = index;

        // Spawn(false) evita que Netcode destruya el objeto al despawnearlo después
        netObj.Spawn(false);

        ocupantes[index] = netObj;
        point.SetTaskActive();

        Debug.Log($"Tarea reutilizada del pool en {point.name}, rol {roleId}, tipo {task.type}");
    }

    private void ActualizarTareasFinalizadas()
    {
        for (int i = 0; i < ocupantes.Length; i++)
        {
            if (spawnPoints[i] == null || !spawnPoints[i].HasActiveTask)
                continue;

            if (ocupantes[i] != null && ocupantes[i].IsSpawned)
                continue;

            NetworkObject finishedObj = ocupantes[i];
            ocupantes[i] = null;

            if (finishedObj != null)
                DevolverAlPool(finishedObj);

            spawnPoints[i].SetTaskFinished();

            Debug.Log($"Tarea finalizada en {spawnPoints[i].name}. Devuelta al pool y cooldown iniciado.");
        }
    }

    public InteractableSpawnPoint GetSpawnPoint(int index)
    {
        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
            return null;

        return spawnPoints[index];
    }

    public void NotifyTaskFinished(int index, NetworkObject finishedObject)
    {
        if (!IsServer || spawnPoints == null || ocupantes == null ||
            index < 0 || index >= spawnPoints.Length)
        {
            return;
        }

        if (ocupantes[index] != finishedObject)
            return;

        ocupantes[index] = null;
        DevolverAlPool(finishedObject);

        if (spawnPoints[index] != null && spawnPoints[index].HasActiveTask)
        {
            spawnPoints[index].SetTaskFinished();
        }

        Debug.Log($"Tarea finalizada en punto {index}. Devuelta al pool y cooldown iniciado.");
    }
}