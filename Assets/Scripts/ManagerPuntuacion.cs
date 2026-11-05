using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ManagerPuntuacion : NetworkBehaviour
{
    public static ManagerPuntuacion Instance { get; private set; }
    [SerializeField] private TMP_Text textoPuntuacion;
    //private int puntuacion = 0;
    public NetworkVariable<int> puntuacion = new NetworkVariable<int>(0);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);    
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
    }
    public override void OnNetworkSpawn()
    {
        ActualizarTexto(0,puntuacion.Value);
        puntuacion.OnValueChanged += ActualizarTexto;
    }
    public void Sumar()
    {
        if (!IsServer) return; 
        puntuacion.Value++;
      //  ActualizarTexto();
        Debug.Log("Puntuación: " + puntuacion.Value);
    }
    
     public void Restar()
    {
        if (!IsServer) return;
        puntuacion.Value--;
       // ActualizarTexto();
        Debug.Log("Puntuación-: " + puntuacion.Value);
    }

    private void ActualizarTexto(int oldValue, int newValue)
    {
        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = "Puntuación: " +newValue;
        }
    }
}
