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

    [SerializeField] private TMP_Text loginErrorText;
    [SerializeField] private TMP_Text registerErrorText;
    [SerializeField] private TMP_Text recoveryErrorText;

    [SerializeField] private string mainSceneName = "MainScene";

    private AuthService authService;

    private void Start()
    {
        authService = new AuthService();

        clearAllMessages();

        Debug.Log("PassKeyAuthController gestartet.");
        Debug.Log("Speicherort: " + authService.getStoragePath());
    }

    public void registerUser()
    {
        clearAllMessages();

        PassKeyRecord record = authService.registerUser(usernameInput.text, companyNameInput.text);

        if (record == null)
        {
            showRegisterError("Registrierung fehlgeschlagen. Prüfe Nutzername und Firmenname.");
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
        clearAllMessages();

        PassKeyRecord user = authService.loginWithPassKey(passKeyInput.text);

        if (user == null)
        {
            showLoginError("Login fehlgeschlagen. PassKey ist falsch oder leer.");
            return;
        }

        StateManager.Instance.login(user);
        SceneManager.LoadScene(mainSceneName);
    }

    public void resetPassKey()
    {
        clearAllMessages();

        string newPassKey = authService.resetPassKeyWithRecoveryKey(recoveryKeyInput.text);

        if (string.IsNullOrWhiteSpace(newPassKey))
        {
            showRecoveryError("Reset fehlgeschlagen. RecoveryKey ist falsch oder leer.");
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

    private void clearAllMessages()
    {
        if (registerResultText != null)
            registerResultText.text = "";

        if (recoveryResultText != null)
            recoveryResultText.text = "";

        if (loginErrorText != null)
            loginErrorText.text = "";

        if (registerErrorText != null)
            registerErrorText.text = "";

        if (recoveryErrorText != null)
            recoveryErrorText.text = "";
    }

    private void showLoginError(string message)
    {
        if (loginErrorText != null)
            loginErrorText.text = message;

        Debug.LogError(message);
    }

    private void showRegisterError(string message)
    {
        if (registerErrorText != null)
            registerErrorText.text = message;

        Debug.LogError(message);
    }

    private void showRecoveryError(string message)
    {
        if (recoveryErrorText != null)
            recoveryErrorText.text = message;

        Debug.LogError(message);
    }
}