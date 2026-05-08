using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PassKeyAuthController : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField companyNameInput;
    [SerializeField] private TMP_InputField passKeyInput;
    [SerializeField] private TMP_InputField recoveryKeyInput;

    [SerializeField] private TMP_Text registerResultText;
    [SerializeField] private TMP_Text recoveryResultText;
    [SerializeField] private TMP_Text errorText;

    [SerializeField] private string mainSceneName = "MainScene";

    private AuthService authService;

    private void Start()
    {
        authService = new AuthService();

        if (registerResultText != null)
        {
            registerResultText.text = "";
        }

        if (recoveryResultText != null)
        {
            recoveryResultText.text = "";
        }

        if (errorText != null)
        {
            errorText.text = "";
        }

        Debug.Log("PassKeyAuthController gestartet.");
        Debug.Log("Speicherort: " + authService.getStoragePath());
    }

    public void registerUser()
    {
        clearMessages();

        PassKeyRecord record = authService.registerUser(usernameInput.text, companyNameInput.text);

        if (record == null)
        {
            showError("Registrierung fehlgeschlagen. Prüfe Nutzername und Firmenname.");
            return;
        }

        if (registerResultText != null)
        {
            registerResultText.text =
                "Registrierung erfolgreich!\n" +
                "Nutzer: " + record.username + "\n" +
                "Firma: " + record.companyName + "\n" +
                "PassKey: " + record.passKey + "\n" +
                "RecoveryKey: " + record.recoveryKey + "\n\n" +
                "Bitte diese Daten sicher notieren.";
        }
    }

    public void loginWithPassKey()
    {
        clearMessages();

        PassKeyRecord user = authService.loginWithPassKey(passKeyInput.text);

        if (user == null)
        {
            showError("Login fehlgeschlagen. PassKey ist falsch oder leer.");
            return;
        }

        StateManager.Instance.login(user);
        SceneManager.LoadScene(mainSceneName);
    }

    public void resetPassKey()
    {
        clearMessages();

        string newPassKey = authService.resetPassKeyWithRecoveryKey(recoveryKeyInput.text);

        if (string.IsNullOrWhiteSpace(newPassKey))
        {
            showError("Reset fehlgeschlagen. RecoveryKey ist falsch oder leer.");
            return;
        }

        if (recoveryResultText != null)
        {
            recoveryResultText.text =
                "PassKey erfolgreich zurückgesetzt!\n" +
                "Neuer PassKey: " + newPassKey + "\n\n" +
                "Bitte den neuen PassKey sicher notieren.";
        }
    }

    public void logout()
    {
        StateManager.Instance.logout();
    }

    private void clearMessages()
    {
        if (registerResultText != null)
        {
            registerResultText.text = "";
        }

        if (recoveryResultText != null)
        {
            recoveryResultText.text = "";
        }

        if (errorText != null)
        {
            errorText.text = "";
        }
    }

    private void showError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }

        Debug.LogError(message);
    }
}