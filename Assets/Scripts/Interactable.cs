
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : NetworkBehaviour
{
    public enum InteractionType
    {
        Hold,
        SkillCheck,
        Mash
    }

    [Header("Datos de red")]
    public NetworkVariable<ulong> taskId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> spawnPointIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Tipo")]
    public InteractionType type = InteractionType.Hold;

    [Header("Skill Check")]
    public float skillCheckRotationTime = 1.5f;

    [Range(0.05f, 0.4f)]
    public float skillCheckZoneSize = 0.12f;

    public int skillChecksRequired = 1;
    public float failCooldown = 0.1f;

    [Header("Mash")]
    public int mashPresses = 15;
    public float mashDecayPerSecond = 0.3f;

    [Header("Interacción")]
    public string prompt = "Interactuar";
    public float holdTime = 1.5f;
    public float destroyTime = 5f;
    public Transform promptAnchor;

    [Header("Materiales por jugador")]
    [SerializeField] private Renderer[] renderersObjetivo;

    [SerializeField] private Material materialJugador1;
    [SerializeField] private Material materialJugador2;
    [SerializeField] private Material materialJugador3;
    [SerializeField] private Material materialJugador4;

    private Coroutine timerRoutine;
    private bool finished;

    public Vector3 PromptPosition =>
        promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * 1.5f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;

        renderersObjetivo =
            GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        finished = false;

        taskId.OnValueChanged += OnTaskIdChanged;

        ActualizarMaterial();

        if (IsServer)
        {
            timerRoutine = StartCoroutine(DestroyAfterTime());
        }
    }

    private void OnTaskIdChanged(ulong previous, ulong current)
    {
        ActualizarMaterial();
    }

    private void ActualizarMaterial()
    {
        Material materialElegido = ObtenerMaterial(taskId.Value);

        if (materialElegido == null ||
            renderersObjetivo == null)
        {
            return;
        }

        foreach (Renderer rend in renderersObjetivo)
        {
            if (rend == null)
                continue;

            Material[] materiales = rend.sharedMaterials;

            for (int i = 0; i < materiales.Length; i++)
            {
                materiales[i] = materialElegido;
            }

            rend.sharedMaterials = materiales;
        }
    }

    private Material ObtenerMaterial(ulong clientId)
    {
        switch (clientId)
        {
            case 0:
                return materialJugador1;

            case 1:
                return materialJugador2;

            case 2:
                return materialJugador3;

            case 3:
                return materialJugador4;

            default:
                Debug.LogWarning(
                    "No hay material configurado para ClientId: " +
                    clientId
                );

                return null;
        }
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(destroyTime);

        if (finished || !IsSpawned)
            yield break;

        FinishTask(false);
    }

    public virtual void InteractuarNet()
    {
        if (!IsServer || finished || !IsSpawned)
            return;

        FinishTask(true);
    }

    private void FinishTask(bool completed)
    {
        if (!IsServer || finished || !IsSpawned)
            return;

        finished = true;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (ManagerPuntuacion.Instance != null)
        {
            if (completed)
                ManagerPuntuacion.Instance.Sumar();
            else
                ManagerPuntuacion.Instance.Restar();
        }

        InteractableGenerator generator =
            FindFirstObjectByType<InteractableGenerator>();

        if (generator != null)
        {
            generator.NotifyTaskFinished(
                spawnPointIndex.Value,
                NetworkObject
            );
        }

        NetworkObject.Despawn();
    }

    public override void OnNetworkDespawn()
    {
        taskId.OnValueChanged -= OnTaskIdChanged;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }
    }

    public void contadormas()
    {
        if (IsServer &&
            ManagerPuntuacion.Instance != null)
        {
            ManagerPuntuacion.Instance.Sumar();
        }
    }
}
