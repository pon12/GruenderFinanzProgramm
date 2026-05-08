using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeypadInputTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NumberKeypadController keypadController;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (keypadController == null)
        {
            Debug.LogError("KeypadController ist nicht gesetzt.");
            return;
        }

        keypadController.setTargetInput(inputField);
    }
}