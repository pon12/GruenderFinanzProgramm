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

    private AuthService authService;

    private void Start()
    {
        errorText.SetActive(false);

        IUserRepository userRepository = new DummyUserRepository();
        authService = new AuthService(userRepository);

        // MARK: Test für Dummy Login
        authService.register("test@dummy.de", "1234", "1234");
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
}