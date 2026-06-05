using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PassKeyAuthController : MonoBehaviour
{
    [Header("Optionale TMP-Testfelder")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passKeyInput;
    [SerializeField] private TMP_InputField recoveryKeyInput;

    [Header("Optionale Ausgabetexte")]
    [SerializeField] private TMP_Text registerResultText;
    [SerializeField] private TMP_Text recoveryResultText;

    [SerializeField] private TMP_Text loginErrorText;
    [SerializeField] private TMP_Text registerErrorText;
    [SerializeField] private TMP_Text recoveryErrorText;

    [Header("Scene Settings")]
    [SerializeField] private string mainSceneName = "SampleScene";

    private AuthService authService;

    private void Awake()
    {
        authService = new AuthService();
    }

    private void Start()
    {
        clearAllMessages();

        Debug.Log("PassKeyAuthController gestartet.");
        Debug.Log("Speicherort: " + authService.getStoragePath());
    }

    // --------------------------------------------------
    // Alte Methoden für TMP-Testszene
    // --------------------------------------------------

    public void registerUser()
    {
        string username = usernameInput != null ? usernameInput.text : "";
        registerUserWithName(username);
    }

    public void loginWithPassKey()
    {
        string passKey = passKeyInput != null ? passKeyInput.text : "";

        bool loginSuccessful = loginWithPassKeyValue(passKey);

        if (loginSuccessful)
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }

    public void resetPassKey()
    {
        string recoveryKey = recoveryKeyInput != null ? recoveryKeyInput.text : "";
        resetPassKeyWithRecoveryKeyValue(recoveryKey);
    }

    // --------------------------------------------------
    // Neue Methoden für UI Toolkit / anderes UI
    // --------------------------------------------------

    public bool registerUserWithName(string username)
    {
        clearAllMessages();

        PassKeyRecord record = authService.registerUser(username);

        if (record == null)
        {
            showRegisterError("Registrierung fehlgeschlagen. Prüfe den Nutzernamen.");
            return false;
        }

        showRegisterSuccess(
            "Registrierung erfolgreich!\n" +
            "Nutzer: " + record.username + "\n" +
            "PassKey: " + authService.passkeyGlobal + "\n" +
            "RecoveryKey: " + authService.recoveryPassKeyGlobal + "\n" +
            "Bitte sicher notieren."
        );

        return true;
    }

    public bool loginWithPassKeyValue(string passKey)
    {
        clearAllMessages();

        PassKeyRecord user = authService.loginWithPassKey(passKey);

        if (user == null)
        {
            showLoginError("Login fehlgeschlagen. PassKey ist falsch oder leer.");
            return false;
        }

        if (StateManager.Instance == null)
        {
            Debug.LogError("[Login] StateManager fehlt in der Scene. Login kann nicht abgeschlossen werden.");
            return false;
        }

        StateManager.Instance.login(user);
        return true;
    }

    public string resetPassKeyWithRecoveryKeyValue(string recoveryKey)
    {
        clearAllMessages();

        string newPassKey = authService.resetPassKeyWithRecoveryKey(recoveryKey);

        if (string.IsNullOrWhiteSpace(newPassKey))
        {
            showRecoveryError("Reset fehlgeschlagen. RecoveryKey ist falsch oder leer.");
            return null;
        }

        showRecoverySuccess(
            "PassKey erfolgreich zurückgesetzt!\n" +
            "Neuer PassKey: " + newPassKey + "\n" +
            "Bitte sicher notieren."
        );

        return newPassKey;
    }

    public void logout()
    {
        StateManager.Instance.logout();
    }

    public void clearMessagesFromOutside()
    {
        clearAllMessages();
    }

    // --------------------------------------------------
    // Hilfsfunktionen
    // --------------------------------------------------

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

    private void showRegisterSuccess(string message)
    {
        Debug.Log(message);

        if (registerResultText != null)
        {
            registerResultText.gameObject.SetActive(true);
            registerResultText.text = message;
        }
    }

    private void showRecoverySuccess(string message)
    {
        Debug.Log(message);

        if (recoveryResultText != null)
        {
            recoveryResultText.gameObject.SetActive(true);
            recoveryResultText.text = message;
        }
    }

    private void showLoginError(string message)
    {
        Debug.LogError(message);

        if (loginErrorText != null)
        {
            loginErrorText.text = message;
            loginErrorText.gameObject.SetActive(true);
        }
    }

    private void showRegisterError(string message)
    {
        Debug.LogError(message);

        if (registerErrorText != null)
        {
            registerErrorText.text = message;
            registerErrorText.gameObject.SetActive(true);
        }
    }

    private void showRecoveryError(string message)
    {
        Debug.LogError(message);

        if (recoveryErrorText != null)
        {
            recoveryErrorText.text = message;
            recoveryErrorText.gameObject.SetActive(true);
        }
    }
}