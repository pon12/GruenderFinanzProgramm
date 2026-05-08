using TMPro;
using UnityEngine;

public class NumberKeypadController : MonoBehaviour
{
    private TMP_InputField currentTargetInput;

    public void setTargetInput(TMP_InputField inputField)
    {
        currentTargetInput = inputField;
    }

    public void addDigit(string digit)
    {
        if (currentTargetInput == null)
        {
            Debug.LogWarning("Kein Eingabefeld ausgewählt.");
            return;
        }

        currentTargetInput.text += digit;
        currentTargetInput.ActivateInputField();
    }

    public void removeLastDigit()
    {
        if (currentTargetInput == null)
        {
            Debug.LogWarning("Kein Eingabefeld ausgewählt.");
            return;
        }

        if (currentTargetInput.text.Length <= 0)
        {
            return;
        }

        currentTargetInput.text = currentTargetInput.text.Substring(0, currentTargetInput.text.Length - 1);
        currentTargetInput.ActivateInputField();
    }

    public void clearInput()
    {
        if (currentTargetInput == null)
        {
            Debug.LogWarning("Kein Eingabefeld ausgewählt.");
            return;
        }

        currentTargetInput.text = "";
        currentTargetInput.ActivateInputField();
    }
}