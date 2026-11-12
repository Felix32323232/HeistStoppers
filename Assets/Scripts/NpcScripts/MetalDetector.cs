using System.Collections;
using UnityEngine;

public class MetalDetector : MonoBehaviour
{
    [Header("Recorrido")]
    [Tooltip("Cola + sitio justo delante del arco (pon un solo spot). El forward del spot debe mirar hacia el arco.")]
    public ServicePointGroup queue;
    [Tooltip("Centro del arco, a la altura del suelo. Aquí suena la alarma.")]
    public Transform center;
    [Tooltip("Punto al otro lado del arco, dentro del NavMesh.")]
    public Transform exitPoint;
    public float triggerRadius = 0.6f;

    [Header("Feedback (opcional)")]
    public AudioSource alarmSound;
    public Light alarmLight;
    public float alarmDuration = 2f;

    Coroutine alarmRoutine;

    void Awake()
    {
        if (alarmLight != null) alarmLight.enabled = false;
    }

    public void PlayAlarm()
    {
        if (alarmRoutine != null) StopCoroutine(alarmRoutine);
        alarmRoutine = StartCoroutine(AlarmRoutine());
    }

    IEnumerator AlarmRoutine()
    {
        if (alarmLight != null) alarmLight.enabled = true;
        if (alarmSound != null) alarmSound.Play();
        yield return new WaitForSeconds(alarmDuration);
        if (alarmLight != null) alarmLight.enabled = false;
        if (alarmSound != null) alarmSound.Stop();
        alarmRoutine = null;
    }

    void OnDrawGizmos()
    {
        if (center != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center.position, triggerRadius);
        }
        if (exitPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
        }
    }
}
