
using UnityEngine;

public class InteractableSpawnPoint : MonoBehaviour
{
    [Header("Zona de interacción")]
    [SerializeField] private Transform interactionPoint;

    [Header("Objeto visual")]
    [SerializeField] private Renderer[] objectRenderers;

    [Header("Configuración")]
    [SerializeField, Min(0f)]
    private float cooldown = 10f;

    private bool hasActiveTask = false;
    private float availableAt = 0f;

    public Transform InteractionPoint =>
        interactionPoint != null ? interactionPoint : transform;

    public Renderer[] ObjectRenderers => objectRenderers;

    public bool HasActiveTask => hasActiveTask;

    public bool IsAvailable =>
        !hasActiveTask && Time.time >= availableAt;

    public void SetTaskActive()
    {
        hasActiveTask = true;
    }

    public void SetTaskFinished()
    {
        hasActiveTask = false;
        availableAt = Time.time + cooldown;
    }

    private void Reset()
    {
        interactionPoint = transform;
        objectRenderers =
            GetComponentsInChildren<Renderer>(true);
    }

    private void OnValidate()
    {
        if (cooldown < 0f)
            cooldown = 0f;
    }
}
