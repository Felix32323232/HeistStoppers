using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NPCKnockback : NetworkBehaviour
{
    [Header("Fuerza del golpe")]
    [SerializeField] private float knockbackForce = 7f;
    [SerializeField] private float upwardForce = 2f;

    [Header("Recuperación")]
    [SerializeField] private float recoveryDelay = 2f;
    [SerializeField] private float groundY = 0f;

    private Rigidbody rb;
    private Coroutine recoveryCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void ReceiveHit(Vector3 direction)
    {
        if (!IsServer)
            return;

        direction.y = 0f;
        direction.Normalize();

        Vector3 force =
            direction * knockbackForce +
            Vector3.up * upwardForce;

        rb.AddForce(force, ForceMode.Impulse);

        if (recoveryCoroutine != null)
            StopCoroutine(recoveryCoroutine);

        recoveryCoroutine = StartCoroutine(RecoverAfterKnockback());
    }

    private IEnumerator RecoverAfterKnockback()
    {
        yield return new WaitForSeconds(recoveryDelay);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 position = rb.position;
        position.y = groundY;
        rb.position = position;
        Vector3 rotation = rb.rotation.eulerAngles;
        rb.rotation = Quaternion.Euler(0f, rotation.y, 0f);

        recoveryCoroutine = null;
    }
}
