using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PassKeyAuthController : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passKeyInput;
    [SerializeField] private TMP_InputField recoveryKeyInput;

    [SerializeField] private TMP_Text registerResultText;
    [SerializeField] private TMP_Text recoveryResultText;

    [SerializeField] private TMP_Text loginErrorText;
    [SerializeField] private TMP_Text registerErrorText;
    [SerializeField] private TMP_Text recoveryErrorText;

    [SerializeField] private string mainSceneName = "SampleScene";

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

        PassKeyRecord record = authService.registerUser(usernameInput.text);

        if (record == null)
        {
            showRegisterError("Registrierung fehlgeschlagen. Prüfe den Nutzernamen.");
            return;
        }

        if (registerResultText != null)
        {
            registerResultText.gameObject.SetActive(true);
            registerResultText.text =
                "Registrierung erfolgreich!\n" +
                "Nutzer: " + usernameInput.text + "\n" +
                "PassKey und RecoveryKey wurden in der Konsole ausgegeben.\n" +
                "Bitte sicher notieren.";
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
            recoveryResultText.gameObject.SetActive(true);
            recoveryResultText.text =
                "PassKey erfolgreich zurückgesetzt!\n" +
                "Neuer PassKey wurde in der Konsole ausgegeben.\n" +
                "Bitte sicher notieren.";
        }
    }

    public void logout()
    {
        StateManager.Instance.logout();
    }

    public void clearMessagesFromOutside()
    {
        clearAllMessages();
    }

    private void clearAllMessages()
    {
        if (registerResultText != null)
        {
            registerResultText.text = "";
            registerResultText.gameObject.SetActive(false);
        }

        if (recoveryResultText != null)
        {
            recoveryResultText.text = "";
            recoveryResultText.gameObject.SetActive(false);
        }

        if (loginErrorText != null)
        {
            loginErrorText.text = "";
            loginErrorText.gameObject.SetActive(false);
        }

        if (registerErrorText != null)
        {
            registerErrorText.text = "";
            registerErrorText.gameObject.SetActive(false);
        }

        if (recoveryErrorText != null)
        {
            recoveryErrorText.text = "";
            recoveryErrorText.gameObject.SetActive(false);
        }
    }

    private void showLoginError(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.text = message;
            loginErrorText.gameObject.SetActive(true);
        }

        Debug.LogError(message);
    }

    private void showRegisterError(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.text = message;
            registerErrorText.gameObject.SetActive(true);
        }

        Debug.LogError(message);
    }

    private void showRecoveryError(string message)
    {
        if (recoveryErrorText != null)
        {
            recoveryErrorText.text = message;
            recoveryErrorText.gameObject.SetActive(true);
        }

        Debug.LogError(message);
    }
}