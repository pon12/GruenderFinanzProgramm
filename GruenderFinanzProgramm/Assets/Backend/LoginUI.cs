
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{

    [SerializeField] private TMP_InputField passKeyInput;
    [SerializeField] private StateManager stateManager;
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private GameObject errorText;

    private void Start()
    {
        errorText.SetActive(false);
    }

    public void onLoginButtonClicked()
    {

        // Fehlerbehandlung wenn kein int eingegeben wird
        if (!int.TryParse(passKeyInput.text, out int enteredPassKey))
        {
            errorText.SetActive(true);
            Debug.LogError("Fehler: Bitte nur Zahlen eingeben.");
            return;
        }

        bool isValid = stateManager.validatePassKey(enteredPassKey);

        if (isValid)
        {
            errorText.SetActive(false);
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            Debug.LogError("Falscher Passkey.");
            errorText.SetActive(true);
        }

    }

}