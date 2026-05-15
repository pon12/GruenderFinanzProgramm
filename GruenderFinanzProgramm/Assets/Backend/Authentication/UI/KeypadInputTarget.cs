using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeypadInputTarget : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    [SerializeField] private NumberKeypadController keypadController;
    [SerializeField] private int maxLength = 4;
    [SerializeField] private bool allowKeyboardInput = false;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        if (inputField != null)
        {
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

            if (!allowKeyboardInput)
            {
                inputField.readOnly = true;
            }

            inputField.onValueChanged.AddListener(validateInput);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        setAsTarget();
    }

    public void OnSelect(BaseEventData eventData)
    {
        setAsTarget();
    }

    private void setAsTarget()
    {
        if (keypadController == null)
        {
            Debug.LogError("KeypadController ist nicht gesetzt.");
            return;
        }

        keypadController.setTargetInput(inputField, maxLength);
    }

    private void validateInput(string value)
    {
        if (inputField == null)
        {
            return;
        }

        string cleanedValue = "";

        foreach (char c in value)
        {
            if (char.IsDigit(c))
            {
                cleanedValue += c;
            }
        }

        if (cleanedValue.Length > maxLength)
        {
            cleanedValue = cleanedValue.Substring(0, maxLength);
        }

        if (inputField.text != cleanedValue)
        {
            inputField.text = cleanedValue;
        }
    }
}