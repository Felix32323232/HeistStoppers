using System.Collections;
using UnityEngine;

public class InteractableGenerator : MonoBehaviour
{
    public GameObject[] SpawnPoints;
    public GameObject Interactable_Prefab;
    public float tSpawnMin = 5;
    public float tSpawnMax = 5;
    
    IEnumerator Spawns()
    {
        for (int i = 0; i < SpawnPoints.Length; i++)
        {

            Instantiate(Interactable_Prefab, SpawnPoints[i].transform.position, SpawnPoints[i].transform.rotation);
            float espera = Random.Range(tSpawnMin, tSpawnMax);
            yield return new WaitForSeconds(espera);
        }
        
        yield return new WaitForSeconds(5); 
        Debug.Log("Resume Coroutine");
    }
    void Start()
    {
        StartCoroutine(Spawns());
    }
}
