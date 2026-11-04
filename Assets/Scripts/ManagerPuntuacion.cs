using UnityEngine;
using TMPro;

public class ManagerPuntuacion : MonoBehaviour
{
    public static ManagerPuntuacion Instance { get; private set; }
    [SerializeField] private TMP_Text textoPuntuacion;
    private int puntuacion = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);    
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ActualizarTexto();
    }

    public void Sumar()
    {
        puntuacion++;
        ActualizarTexto();
        Debug.Log("Puntuación: " + puntuacion);
    }
    
     public void Restar()
    {
        puntuacion --;
        ActualizarTexto();
        Debug.Log("Puntuación-: " + puntuacion);
    }

    private void ActualizarTexto()
    {
        if (textoPuntuacion != null)
        {
            textoPuntuacion.text = "Puntuación: " + puntuacion;
        }
    }
}
