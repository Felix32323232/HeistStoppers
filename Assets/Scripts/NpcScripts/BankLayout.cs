using UnityEngine;
using UnityEngine.AI;

public class BankLayout : MonoBehaviour
{
    public static BankLayout Instance { get; private set; }

    [Header("Entrada / salida")]
    [Tooltip("Donde aparecen los NPCs. Tiene que estar sobre (o muy cerca) del NavMesh, delante de la puerta.")]
    public Transform entrance;
    [Tooltip("Donde desaparecen al irse. Puede ser el mismo sitio que la entrada.")]
    public Transform exit;

    [Header("Puntos de interés")]
    public ServicePointGroup counter;
    public MetalDetector metalDetector;
    public Transform[] wanderPoints;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static Vector3 Snap(Vector3 point, float maxDistance = 2f)
    {
        return NavMesh.SamplePosition(point, out NavMeshHit hit, maxDistance, NavMesh.AllAreas)
            ? hit.position
            : point;
    }

    public Transform RandomWanderPoint(Transform avoid = null)
    {
        if (wanderPoints == null || wanderPoints.Length == 0) return null;
        if (wanderPoints.Length == 1) return wanderPoints[0];

        Transform t;
        int guard = 0;
        do
        {
            t = wanderPoints[Random.Range(0, wanderPoints.Length)];
        }
        while (t == avoid && ++guard < 10);
        return t;
    }

    public Vector3 PointNear(Transform t, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        return Snap(t.position + new Vector3(offset.x, 0f, offset.y));
    }

    void OnDrawGizmos()
    {
        if (entrance != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(entrance.position, 0.4f);
        }
        if (exit != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(exit.position, 0.4f);
        }
        if (wanderPoints != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Transform p in wanderPoints)
            {
                if (p != null) Gizmos.DrawWireSphere(p.position, 0.3f);
            }
        }
    }
}
