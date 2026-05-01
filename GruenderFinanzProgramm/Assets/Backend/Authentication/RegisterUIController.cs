// Das selbe wie LoginUIController nur für die Registrierung

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RegisterUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private GameObject errorText;
    [SerializeField] private GameObject successText;
    [SerializeField] private string loginSceneName = "LoginScene";

    private AuthService authService;

    private void Start()
    {
        errorText.SetActive(false);
        successText.SetActive(false);

        IUserRepository userRepository = new DummyUserRepository();
        authService = new AuthService(userRepository);
    }

    public void onRegisterButtonClicked()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        bool registered = authService.register(email, password, confirmPassword);

        if (!registered)
        {
            errorText.SetActive(true);
            successText.SetActive(false);
            return;
        }

        errorText.SetActive(false);
        successText.SetActive(true);
    }

    public void goToLogin()
    {
        SceneManager.LoadScene(loginSceneName);
    }
}