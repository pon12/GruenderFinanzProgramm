using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeypadInputTarget : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    [SerializeField] private NumberKeypadController keypadController;
    [SerializeField] private int maxLength = 4;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        if (inputField != null)
        {
            inputField.readOnly = true;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
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
}