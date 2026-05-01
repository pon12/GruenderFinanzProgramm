// Brücke zwischen Unity UI und Backend 
// Nicht für Datenlogik oder Hashing
// -> "User hat auf Login gedrückt, mache ...
// Input lesen

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private GameObject errorText;
    [SerializeField] private string mainSceneName = "MainScene";

    [SerializeField] private TMP_InputField passKeyInput;

    private AuthService authService;

    private void Start()
    {
        errorText.SetActive(false);

        IUserRepository userRepository = new DummyUserRepository();
        authService = new AuthService(userRepository);

        // MARK: Test für Dummy Login
        authService.register("test@dummy.de", "1234", "1234");

        // MARK: Temporäre Test-Keys generieren
        string passKey = authService.generatePassKey();
        string recoveryKey = authService.generateRecoveryKey();

        authService.saveTemporaryKeys();

        Debug.Log("Temporary PassKey: " + passKey);
        Debug.Log("Temporary RecoveryKey: " + recoveryKey);
        Debug.Log("Keys gespeichert unter: " + authService.getPassKeyStoragePath());


    }

    public void onLoginButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        User loggedInUser = authService.login(email, password);

        if (loggedInUser == null)
        {
            errorText.SetActive(true);
            return;
        }

        errorText.SetActive(false);
        StateManager.Instance.login(loggedInUser);

        SceneManager.LoadScene(mainSceneName);
    }

    public void onPassKeyLoginButtonClicked()
    {
        string enteredPassKey = passKeyInput.text;

        bool isValid = authService.loginWithPassKey(enteredPassKey);

        if (!isValid)
        {
            errorText.SetActive(true);
            return;
        }

        errorText.SetActive(false);

        // Dummy-User für temporären Passkey-Login
        User passKeyUser = new User(0, "passkey-user@local.test", "");

        StateManager.Instance.login(passKeyUser);

        SceneManager.LoadScene(mainSceneName);
    }



}