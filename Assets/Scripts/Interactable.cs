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
    private int prefabPoolIndex = -1;

    public int PrefabPoolIndex => prefabPoolIndex;

    public Vector3 PromptPosition =>
        promptAnchor != null
            ? promptAnchor.position
            : transform.position + Vector3.up * 1.5f;

    public void SetPoolIndex(int index)
    {
        prefabPoolIndex = index;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        renderersObjetivo = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        finished = false;

        taskId.OnValueChanged += OnTaskIdChanged;

        ActualizarMaterial();

        if (IsServer)
        {
            if (timerRoutine != null)
                StopCoroutine(timerRoutine);

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

        if (materialElegido == null || renderersObjetivo == null)
            return;

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

    private Material ObtenerMaterial(ulong roleId)
    {
        switch (roleId)
        {
            case 1: return materialJugador1; // Rojo
            case 2: return materialJugador2; // Azul
            case 3: return materialJugador3; // Verde
            case 4: return materialJugador4; // Amarillo
            default: return null;
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

        // Despawn(false) saca el objeto de la red SIN destruirlo para devolverlo al pool
        NetworkObject.Despawn(false);
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
        if (IsServer && ManagerPuntuacion.Instance != null)
        {
            ManagerPuntuacion.Instance.Sumar();
        }
    }
}