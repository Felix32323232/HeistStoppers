using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NPCSpawner : NetworkBehaviour
{
    [System.Serializable]
    public class NPCType
    {
        public NetworkObject prefab;
        [Tooltip("Probabilidad relativa: 2 sale el doble de veces que 1.")]
        [Min(0f)] public float weight = 1f;
    }

    [SerializeField] NPCType[] npcTypes;
    [SerializeField] float firstDelay = 3f;
    [SerializeField] float intervalMin = 6f;
    [SerializeField] float intervalMax = 12f;
    [Tooltip("Máximo de NPCs en el banco a la vez.")]
    [SerializeField] int maxAlive = 6;

    readonly List<NetworkObject> alive = new List<NetworkObject>();
    Coroutine routine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (npcTypes == null || npcTypes.Length == 0)
        {
            Debug.LogError("[NPCSpawner] Faltan prefabs en npcTypes.", this);
            return;
        }

        routine = StartCoroutine(SpawnLoop());
    }

    public override void OnNetworkDespawn()
    {
        if (routine != null) StopCoroutine(routine);
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitUntil(() => LobbyManager.Instance != null && LobbyManager.Instance.unlocked.Value);
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            alive.RemoveAll(n => n == null || !n.IsSpawned);
            if (alive.Count < maxAlive) SpawnOne();

            yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
        }
    }

    void SpawnOne()
    {
        BankLayout layout = BankLayout.Instance;
        if (layout == null || layout.entrance == null)
        {
            Debug.LogError("[NPCSpawner] Falta BankLayout (o su Entrance) en la escena.", this);
            return;
        }

        NPCType type = PickWeighted();
        if (type == null) return;

        NetworkObject npc = Instantiate(type.prefab, layout.entrance.position, layout.entrance.rotation);
        npc.Spawn(true);
        alive.Add(npc);
    }

    NPCType PickWeighted()
    {
        float total = 0f;
        foreach (NPCType t in npcTypes)
        {
            if (t != null && t.prefab != null) total += t.weight;
        }
        if (total <= 0f) return null;

        float roll = Random.value * total;
        NPCType last = null;
        foreach (NPCType t in npcTypes)
        {
            if (t == null || t.prefab == null) continue;
            last = t;
            roll -= t.weight;
            if (roll <= 0f) return t;
        }
        return last;
    }
}
