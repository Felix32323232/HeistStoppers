using UnityEngine;

public class NPCWanderer : NPCBase
{
    enum State { Walking, Lingering }

    [Header("Paseante")]
    [SerializeField] int minStops = 2;
    [SerializeField] int maxStops = 4;
    [SerializeField] float lingerMin = 2f;
    [SerializeField] float lingerMax = 6f;
    [Tooltip("Radio alrededor de cada punto de paseo, para que no se apilen en el mismo sitio.")]
    [SerializeField] float spreadRadius = 1.5f;

    State state;
    int stopsLeft;
    Transform lastPoint;
    float lingerUntil;

    protected override void OnServerSpawned()
    {
        stopsLeft = Random.Range(minStops, maxStops + 1);
        GoToNextPoint();
    }

    protected override void OnServerTick()
    {
        switch (state)
        {
            case State.Walking:
                if (HasArrived(0.8f))
                {
                    lingerUntil = Time.time + Random.Range(lingerMin, lingerMax);
                    state = State.Lingering;
                }
                break;

            case State.Lingering:
                if (Time.time >= lingerUntil) GoToNextPoint();
                break;
        }
    }

    void GoToNextPoint()
    {
        Transform point = (stopsLeft > 0 && Layout != null) ? Layout.RandomWanderPoint(lastPoint) : null;
        if (point == null)
        {
            BeginLeaving();
            return;
        }

        stopsLeft--;
        lastPoint = point;
        MoveTo(Layout.PointNear(point, spreadRadius));
        state = State.Walking;
    }
}
