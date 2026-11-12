using System.Collections.Generic;
using UnityEngine;

public class ServicePointGroup : MonoBehaviour
{
    [Tooltip("Sitios donde se coloca el NPC. El forward (eje azul) de cada Transform es hacia donde mirará.")]
    public Transform[] spots;

    [Tooltip("Inicio de la cola. La cola crece hacia el forward (eje azul) de este Transform, es decir, hacia atrás.")]
    public Transform queueStart;
    public float queueSpacing = 1.1f;

    NPCBase[] occupants;
    readonly List<NPCBase> queue = new List<NPCBase>();

    void Awake()
    {
        occupants = new NPCBase[spots != null ? spots.Length : 0];
    }

    public void Join(NPCBase npc)
    {
        if (!queue.Contains(npc)) queue.Add(npc);
    }

    public bool TryClaim(NPCBase npc, out Transform spot)
    {
        spot = null;
        queue.RemoveAll(n => n == null);
        if (queue.Count == 0 || queue[0] != npc) return false;

        for (int i = 0; i < occupants.Length; i++)
        {
            if (occupants[i] != null) continue;
            occupants[i] = npc;
            queue.RemoveAt(0);
            spot = spots[i];
            return true;
        }
        return false;
    }

    public void Leave(NPCBase npc)
    {
        queue.Remove(npc);
        for (int i = 0; i < occupants.Length; i++)
        {
            if (occupants[i] == npc) occupants[i] = null;
        }
    }

    public Vector3 QueuePosition(NPCBase npc)
    {
        int index = Mathf.Max(0, queue.IndexOf(npc));
        Vector3 origin = queueStart != null ? queueStart.position : transform.position;
        Vector3 direction = queueStart != null ? queueStart.forward : transform.forward;
        return origin + direction * (queueSpacing * index);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (spots != null)
        {
            foreach (Transform s in spots)
            {
                if (s == null) continue;
                Gizmos.DrawWireSphere(s.position, 0.3f);
                Gizmos.DrawRay(s.position, s.forward * 0.6f);
            }
        }

        if (queueStart != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 6; i++)
                Gizmos.DrawWireSphere(queueStart.position + queueStart.forward * (queueSpacing * i), 0.2f);
        }
    }
}
