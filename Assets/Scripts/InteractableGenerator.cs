
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

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No hay Spawn Points asignados.");
            return;
        }

        if (interactablePrefabs == null ||
            interactablePrefabs.Length == 0)
        {
            Debug.LogError("No hay prefabs de interacción.");
            return;
        }

        ocupantes = new NetworkObject[spawnPoints.Length];
        routine = StartCoroutine(Spawns());
    }

    public override void OnNetworkDespawn()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
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

            yield return new WaitForSeconds(
                Random.Range(min, max)
            );
        }
        while (bucleInfinito);

        // Seguir comprobando las tareas existentes
        // aunque se desactive la generación.
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

        return disponibles[
            Random.Range(0, disponibles.Count)
        ];
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
            // Solo asignar tareas a jugadores preparados.
            if (!player.Ready ||
                player.SelectedRole == RoleManager.Role.None)
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

        selectedRole = candidates[
            Random.Range(0, candidates.Count)
        ];

        return true;
    }


    private void GenerarTarea(int index)
    {
        if (!ElegirRol(out ulong roleId)) return;

        InteractableSpawnPoint point = spawnPoints[index];

        GameObject prefab = interactablePrefabs[
            Random.Range(0, interactablePrefabs.Length)
        ];

        if (prefab == null)
            return;

        GameObject go = Instantiate(
            prefab,
            point.InteractionPoint.position,
            point.InteractionPoint.rotation
        );

        if (!go.TryGetComponent(out Interactable task) ||
            !go.TryGetComponent(out NetworkObject netObj))
        {
            Debug.LogError(
                "El prefab necesita Interactable y NetworkObject."
            );

            Destroy(go);
            return;
        }

        // El servidor establece el propietario de la tarea.
        task.taskId.Value = roleId;
        task.spawnPointIndex.Value = index;
        // El prefab determina el tipo de interacción.
        // Cada prefab debe tener configurado su tipo.
        netObj.Spawn();

        ocupantes[index] = netObj;
        point.SetTaskActive();

        Debug.Log(
    $"Tarea generada en {point.name}, " +
    $"rol {roleId}, tipo {task.type}"
);
    }

    private void ActualizarTareasFinalizadas()
    {
        for (int i = 0; i < ocupantes.Length; i++)
        {
            // Si el objeto ya estaba libre, no hacemos nada.
            if (spawnPoints[i] == null ||
                !spawnPoints[i].HasActiveTask)
            {
                continue;
            }

            // Si la tarea sigue existiendo y activa,
            // todavía no hay que liberar el punto.
            if (ocupantes[i] != null &&
                ocupantes[i].IsSpawned)
            {
                continue;
            }

            // La tarea terminó o fue destruida.
            ocupantes[i] = null;

            // Liberamos el punto y comienza el cooldown.
            spawnPoints[i].SetTaskFinished();

            Debug.Log(
                $"Tarea finalizada en {spawnPoints[i].name}. " +
                "Cooldown iniciado."
            );
        }
    }

    public InteractableSpawnPoint GetSpawnPoint(int index)
    {
        if (spawnPoints == null ||
            index < 0 ||
            index >= spawnPoints.Length)
        {
            return null;
        }

        return spawnPoints[index];
    }

    public void NotifyTaskFinished(
        int index,
        NetworkObject finishedObject)
    {
        if (!IsServer || spawnPoints == null ||
            ocupantes == null ||
            index < 0 ||
            index >= spawnPoints.Length)
        {
            return;
        }

        // Evita liberar el punto por una tarea antigua.
        if (ocupantes[index] != finishedObject)
            return;

        ocupantes[index] = null;

        if (spawnPoints[index] != null &&
            spawnPoints[index].HasActiveTask)
        {
            spawnPoints[index].SetTaskFinished();
        }

        Debug.Log(
            $"Tarea finalizada en punto {index}. " +
            "Cooldown iniciado."
        );
    }

}
