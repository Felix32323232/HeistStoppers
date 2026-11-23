
using UnityEngine;

public class SecurityCameraPoints : MonoBehaviour
{
    public static SecurityCameraPoints Instance { get; private set; }

    [Header("Puntos de vista")]
    public Transform cameraPoint1;
    public Transform cameraPoint2;
    public Transform cameraPoint3;
    public Transform cameraPoint4;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Transform GetCameraPoint(int number)
    {
        switch (number)
        {
            case 1: return cameraPoint1;
            case 2: return cameraPoint2;
            case 3: return cameraPoint3;
            case 4: return cameraPoint4;
            default: return null;
        }
    }
}
