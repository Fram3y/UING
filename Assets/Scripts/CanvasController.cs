using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour
{
    public static CanvasController _instance;

    [Header("Stamina UI Elements")]
    [SerializeField] private Image _staminaProgressUI;

    private void Awake()
    {
        // If there is already a CanvasController, destroy this duplicate
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, this becomes the persistent instance
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (_staminaProgressUI == null) return;

        float fillValue = currentStamina / maxStamina;
        _staminaProgressUI.fillAmount = fillValue;
    }
}
