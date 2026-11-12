using Unity.Netcode;
using UnityEngine;

public class NPCMetalDetectorVisitor : NPCBase
{
    enum State { Queueing, GoingToGate, Crossing, Frisking, WalkingInside, Lingering }

    [Header("Detector")]
    [Range(0f, 1f)]
    [SerializeField] float metalChance = 0.5f;
    [Tooltip("Segundos que espera a que lo cacheen antes de colarse.")]
    [SerializeField] float friskPatience = 12f;
    [SerializeField] string[] alarmLines =
    {
        "¡Son solo las llaves!",
        "Será la hebilla...",
        "No llevo nada, lo juro"
    };
    [SerializeField] string friskedLine = "Vale, vale...";
    [SerializeField] string escapedLine = "¡Hasta luego!";

    [Header("Después de cruzar")]
    [SerializeField] float lingerMin = 3f;
    [SerializeField] float lingerMax = 8f;

    State state;
    bool hasMetal;
    bool alarmChecked;
    Transform gateSpot;
    float friskStart;
    float lingerUntil;

    MetalDetector Detector => Layout != null ? Layout.metalDetector : null;

    protected override void OnServerSpawned()
    {
        MetalDetector d = Detector;
        if (d == null || d.queue == null || d.center == null || d.exitPoint == null)
        {
            Debug.LogError("[NPCMetalDetectorVisitor] Falta BankLayout.metalDetector o alguno de sus campos (queue, center, exitPoint).", this);
            BeginLeaving();
            return;
        }

        hasMetal = Random.value < metalChance;
        d.queue.Join(this);
        state = State.Queueing;
    }

    protected override void OnServerTick()
    {
        MetalDetector d = Detector;

        switch (state)
        {
            case State.Queueing:
                if (d.queue.TryClaim(this, out gateSpot))
                {
                    MoveTo(gateSpot.position);
                    state = State.GoingToGate;
                }
                else if (RepathDue())
                {
                    MoveTo(d.queue.QueuePosition(this));
                }
                break;

            case State.GoingToGate:
                if (HasArrived())
                {
                    state = State.Crossing;
                    MoveTo(d.exitPoint.position);
                }
                break;

            case State.Crossing:
                bool atCenter = FlatDistance(transform.position, d.center.position) <= d.triggerRadius;
                if (!alarmChecked && (atCenter || HasArrived()))
                {
                    alarmChecked = true;
                    if (hasMetal)
                    {
                        StartAlarm(d);
                        break;
                    }
                }
                if (alarmChecked && HasArrived()) FinishCrossing(d);
                break;

            case State.Frisking:
                if (Time.time - friskStart >= friskPatience)
                {
                    SetServable(false);
                    ScoreFailure();
                    hasMetal = false;
                    Say(escapedLine);
                    ResumeCrossing(d);
                }
                break;

            case State.WalkingInside:
                if (HasArrived(0.8f))
                {
                    lingerUntil = Time.time + Random.Range(lingerMin, lingerMax);
                    state = State.Lingering;
                }
                break;

            case State.Lingering:
                if (Time.time >= lingerUntil) BeginLeaving();
                break;
        }
    }

    public override void OnServed()
    {
        if (state != State.Frisking || IsLeaving) return;

        ScoreSuccess();
        hasMetal = false;
        Say(friskedLine);
        ResumeCrossing(Detector);
    }

    void StartAlarm(MetalDetector d)
    {
        state = State.Frisking;
        MoveTo(d.center.position);
        AlarmClientRpc();
        Say(alarmLines != null && alarmLines.Length > 0
            ? alarmLines[Random.Range(0, alarmLines.Length)]
            : "¡Piiip!");
        SetServable(true);
        friskStart = Time.time;
    }

    void ResumeCrossing(MetalDetector d)
    {
        state = State.Crossing;
        MoveTo(d.exitPoint.position);
    }

    void FinishCrossing(MetalDetector d)
    {
        d.queue.Leave(this);
        Say("");

        Transform point = Layout.RandomWanderPoint();
        if (point == null)
        {
            BeginLeaving();
            return;
        }

        MoveTo(Layout.PointNear(point, 1.5f));
        state = State.WalkingInside;
    }

    [ClientRpc]
    void AlarmClientRpc()
    {
        BankLayout layout = BankLayout.Instance;
        if (layout != null && layout.metalDetector != null) layout.metalDetector.PlayAlarm();
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    protected override void OnServerDespawned()
    {
        MetalDetector d = Detector;
        if (d != null && d.queue != null) d.queue.Leave(this);
    }
}
