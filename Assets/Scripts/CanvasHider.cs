using UnityEngine;

public class CanvasHider : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
   public void Hide()
    {
        canvas.gameObject.SetActive(false);
    } 
}
