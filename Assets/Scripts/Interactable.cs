using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    public string prompt = "Interactuar";
    public float holdTime = 1.5f;
    public Transform promptAnchor;
    public int requiredPlayerId; //id que tiene que tener el player para que funcione el interactable
    public UnityEvent onInteract;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public Vector3 PromptPosition =>  promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 1.5f;
    //Lo he tenido que buscar como se hace, asi sale el E arriba de la caja


}