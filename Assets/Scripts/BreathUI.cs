using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// VR-friendly UI for displaying breath system status.
/// Use with a World Space Canvas so it's visible in the headset.
/// </summary>
public class BreathUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the BreathInputManager component here.")]
    public BreathInputManager breathInput;

    [Header("UI Elements")]
    [Tooltip("TextMeshPro text to display status messages.")]
    public TextMeshProUGUI statusText;

    [Tooltip("Image used as a fill bar to visualize breath level.")]
    public Image breathFillBar;

    void Update()
    {
        if (breathInput == null)
        {
            if (statusText != null)
                statusText.text = "Error: No BreathInputManager assigned!";
            return;
        }

        // Update status text
        if (statusText != null)
        {
            if (breathInput.IsCalibrating)
            {
                float timeLeft = breathInput.CalibrationTimeRemaining;
                statusText.text = $"Calibrating... {timeLeft:F1}s remaining";
            }
            else
            {
                statusText.text = $"Ready!\nBreath: {breathInput.BreathValue:F2}";
            }
        }

        // Update fill bar
        if (breathFillBar != null)
        {
            breathFillBar.fillAmount = breathInput.BreathValue;
        }
    }
}
