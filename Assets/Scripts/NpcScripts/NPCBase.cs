using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class NPCBase : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float repathInterval = 0.4f;
    [Tooltip("Si un NPC no llega a su destino en este tiempo, se da por llegado para no quedarse atascado.")]
    [SerializeField] float maxMoveSeconds = 45f;
    [SerializeField] float turnSpeed = 360f;

    [Header("Visual (opcional)")]
    [SerializeField] Animator animator;
    [Tooltip("Parámetro float del Animator que recibe la velocidad. Se calcula en cada cliente a partir de la posición.")]
    [SerializeField] string speedParameter = "Speed";
    [Tooltip("Objeto con el texto sobre la cabeza (se activa solo cuando hay algo que decir).")]
    [SerializeField] GameObject speechRoot;
    [SerializeField] TMP_Text speechText;

    public NetworkVariable<FixedString128Bytes> speech = new NetworkVariable<FixedString128Bytes>();

    protected NavMeshAgent agent;
    protected BankLayout Layout => BankLayout.Instance;
    protected bool IsLeaving => leaving;

    Rigidbody rb;
    NPCKnockback knockback;
    NPCInteractable interactable;

    bool knocked;
    bool leaving;
    bool hasDestination;
    Vector3 destination;
    float moveStartTime;
    float nextRepathTime;
    Quaternion? facing;

    int speedHash;
    bool hasSpeedParam;
    float smoothSpeed;
    Vector3 lastPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        knockback = GetComponent<NPCKnockback>();
        interactable = GetComponent<NPCInteractable>();

        agent.enabled = false;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            speedHash = Animator.StringToHash(speedParameter);
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == speedParameter && p.type == AnimatorControllerParameterType.Float)
                    hasSpeedParam = true;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        speech.OnValueChanged += OnSpeechChanged;
        OnSpeechChanged(default, speech.Value);
        lastPosition = transform.position;

        rb.isKinematic = true;

        if (!IsServer) return;

        if (knockback != null)
        {
            knockback.KnockedBack += OnKnockedBack;
            knockback.Recovered += OnRecovered;
        }

        if (!TryPlaceOnNavMesh(transform.position, 3f))
        {
            Debug.LogError($"[NPC] {name} no está cerca del NavMesh y se elimina. ¿Está horneado el NavMesh y bien colocado el Entrance?", this);
            StartCoroutine(DespawnNextFrame());
            return;
        }

        OnServerSpawned();
    }

    public override void OnNetworkDespawn()
    {
        speech.OnValueChanged -= OnSpeechChanged;

        if (!IsServer) return;

        if (knockback != null)
        {
            knockback.KnockedBack -= OnKnockedBack;
            knockback.Recovered -= OnRecovered;
        }
        OnServerDespawned();
    }

    protected virtual void OnServerSpawned() { }

    protected abstract void OnServerTick();

    public virtual void OnServed() { }

    protected virtual void OnServerDespawned() { }

    void Update()
    {
        UpdateAnimatorSpeed();

        if (!IsServer || !NetworkObject.IsSpawned || knocked || !agent.enabled) return;

        ApplyFacing();

        if (leaving)
        {
            if (HasArrived()) DespawnNow();
            return;
        }

        OnServerTick();
    }

    void LateUpdate()
    {
        if (speechRoot == null || !speechRoot.activeSelf) return;
        Camera cam = Camera.main;
        if (cam != null) speechRoot.transform.rotation = cam.transform.rotation;
    }

    void UpdateAnimatorSpeed()
    {
        if (!hasSpeedParam) return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speed = (transform.position - lastPosition).magnitude / dt;
        lastPosition = transform.position;
        smoothSpeed = Mathf.Lerp(smoothSpeed, speed, 10f * Time.deltaTime);
        animator.SetFloat(speedHash, smoothSpeed);
    }

    bool CanDrive => !knocked && agent != null && agent.enabled && agent.isOnNavMesh;

    bool TryPlaceOnNavMesh(Vector3 from, float radius)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit hit, radius, NavMesh.AllAreas)) return false;
        transform.position = hit.position;
        agent.enabled = true;
        return agent.isOnNavMesh;
    }

    protected void MoveTo(Vector3 target)
    {
        destination = BankLayout.Snap(target);
        hasDestination = true;
        moveStartTime = Time.time;
        facing = null;

        if (CanDrive)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    protected bool HasArrived(float tolerance = 0.35f)
    {
        if (!hasDestination || !CanDrive) return false;
        if (Time.time - moveStartTime > maxMoveSeconds) return true;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= Mathf.Max(tolerance, agent.stoppingDistance);
    }

    protected bool RepathDue()
    {
        if (Time.time < nextRepathTime) return false;
        nextRepathTime = Time.time + repathInterval;
        return true;
    }

    protected void FaceRotation(Quaternion rotation)
    {
        facing = rotation;
    }

    void ApplyFacing()
    {
        if (!facing.HasValue || agent.velocity.sqrMagnitude > 0.04f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, facing.Value, turnSpeed * Time.deltaTime);
    }

    protected void BeginLeaving(string line = "")
    {
        if (leaving) return;
        leaving = true;
        SetServable(false);
        Say(line);

        if (Layout != null && Layout.exit != null)
            MoveTo(Layout.exit.position);
        else
            StartCoroutine(DespawnNextFrame());
    }

    protected void DespawnNow()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    IEnumerator DespawnNextFrame()
    {
        yield return null;
        DespawnNow();
    }

    protected void SetServable(bool value)
    {
        if (IsServer && interactable != null && interactable.canBeServed.Value != value)
            interactable.canBeServed.Value = value;
    }

    protected void ScoreSuccess()
    {
        if (ManagerPuntuacion.Instance != null) ManagerPuntuacion.Instance.Sumar();
    }

    protected void ScoreFailure()
    {
        if (ManagerPuntuacion.Instance != null) ManagerPuntuacion.Instance.Restar();
    }

    protected void Say(string text)
    {
        if (!IsServer) return;
        if (text == null) text = "";
        if (text.Length > 40) text = text.Substring(0, 40);

        FixedString128Bytes value = new FixedString128Bytes(text);
        if (!speech.Value.Equals(value)) speech.Value = value;
    }

    void OnSpeechChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        string s = current.ToString();
        if (speechText != null) speechText.text = s;
        if (speechRoot != null) speechRoot.SetActive(s.Length > 0);
        if (interactable != null) interactable.SetRequest(s);
    }

    void OnKnockedBack()
    {
        knocked = true;
        if (agent.enabled) agent.enabled = false;
        rb.isKinematic = false;   // para que AddForce del golpe haga efecto
    }

    void OnRecovered()
    {
        if (!knocked) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        bool placed = TryPlaceOnNavMesh(transform.position, 4f);
        if (!placed && Layout != null && Layout.entrance != null)
            placed = TryPlaceOnNavMesh(Layout.entrance.position, 4f);

        if (!placed)
        {
            StartCoroutine(DespawnNextFrame());
            return;
        }

        knocked = false;

        if (hasDestination)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }
}
