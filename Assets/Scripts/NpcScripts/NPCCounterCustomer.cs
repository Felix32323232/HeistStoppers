using UnityEngine;

public class NPCCounterCustomer : NPCBase
{
    enum State { Queueing, GoingToWindow, AtWindow }

    [Header("Mostrador")]
    [Tooltip("Segundos que espera en la ventanilla antes de irse enfadado.")]
    [SerializeField] float patience = 20f;
    [SerializeField] string[] requests =
    {
        "Quiero sacar dinero",
        "Quiero abrir una cuenta",
        "Quiero cambiar divisas",
        "Quiero ingresar un cheque",
        "¿Cuál es mi saldo?"
    };
    [SerializeField] string servedLine = "¡Gracias!";
    [SerializeField] string angryLine = "Me enfado y no respiro";

    State state;
    Transform window;
    float waitStart;

    ServicePointGroup Counter => Layout != null ? Layout.counter : null;

    protected override void OnServerSpawned()
    {
        if (Counter == null)
        {
            Debug.LogError("[NPCCounterCustomer] Falta BankLayout.counter en la escena.", this);
            BeginLeaving();
            return;
        }

        Counter.Join(this);
        state = State.Queueing;
    }

    protected override void OnServerTick()
    {
        switch (state)
        {
            case State.Queueing:
                if (Counter.TryClaim(this, out window))
                {
                    MoveTo(window.position);
                    state = State.GoingToWindow;
                }
                else if (RepathDue())
                {
                    MoveTo(Counter.QueuePosition(this));
                }
                break;

            case State.GoingToWindow:
                if (HasArrived())
                {
                    FaceRotation(window.rotation);
                    Say(requests != null && requests.Length > 0
                        ? requests[Random.Range(0, requests.Length)]
                        : "...");
                    SetServable(true);
                    waitStart = Time.time;
                    state = State.AtWindow;
                }
                break;

            case State.AtWindow:
                if (Time.time - waitStart >= patience)
                {
                    ScoreFailure();
                    Counter.Leave(this);
                    BeginLeaving(angryLine);
                }
                break;
        }
    }

    public override void OnServed()
    {
        if (state != State.AtWindow || IsLeaving) return;

        ScoreSuccess();
        Counter.Leave(this);
        BeginLeaving(servedLine);
    }

    protected override void OnServerDespawned()
    {
        ServicePointGroup counter = Counter;
        if (counter != null) counter.Leave(this);
    }
}
