using UnityEngine;
using System.Collections;
using Unity.Netcode;
[RequireComponent(typeof(Collider))]
public class Interactable : NetworkBehaviour
{
    public enum InteractionType { Hold, SkillCheck, Mash }

    [Header("Tipo")]
    public InteractionType type = InteractionType.Hold;

    [Header("Skill check")]
    public float skillCheckRotationTime = 1.5f; // segundos por vuelta
    [Range(0.05f, 0.4f)] public float skillCheckZoneSize = 0.12f; // fracción del círculo
    public int skillChecksRequired = 1;
    public float failCooldown = 0.1f;

    [Header("Machacar")]
    public int mashPresses = 15;
    public float mashDecayPerSecond = 0.3f; // progreso que se pierde por segundo
    public string prompt = "Interactuar";
    public float holdTime = 1.5f;
    public float destroyTime = 5f;
    public Transform promptAnchor;
    //public int requiredPlayerId; 
    //id que tiene que tener el player para que funcione el interactable
    public NetworkVariable<int> requiredPlayerId = new NetworkVariable<int>(1);


    public void contadormas()
    {
        
        ManagerPuntuacion.Instance.Sumar();
    }
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public Vector3 PromptPosition =>  promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 1.5f;
    //Lo he tenido que buscar como se hace, asi sale el E arriba de la caja
    IEnumerator DestroyAfterTime()

    {
        yield return new WaitForSeconds(destroyTime);
        ManagerPuntuacion.Instance.Restar();
        //Destroy(gameObject);
        GetComponent<NetworkObject>().Despawn();
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
            StartCoroutine(DestroyAfterTime());
    }
    public void InteractuarNet()
    {
      ManagerPuntuacion.Instance.Sumar();
        GetComponent<NetworkObject>().Despawn();
    }
}