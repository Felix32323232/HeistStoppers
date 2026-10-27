using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance { get; private set; }

    [SerializeField] GameObject root;      // PromptRoot
    [SerializeField] TMP_Text promptText;
    [SerializeField] Image fillImage;      // CircleFill

    void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(string text)
    {
        promptText.text = text;
        SetFill(0f);
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (root.activeSelf != visible) root.SetActive(visible);
    }

    public void SetFill(float amount)
    {
        fillImage.fillAmount = amount;
    }

    public void SetScreenPosition(Vector3 screenPos)
    {
        root.transform.position = screenPos;
    }
}