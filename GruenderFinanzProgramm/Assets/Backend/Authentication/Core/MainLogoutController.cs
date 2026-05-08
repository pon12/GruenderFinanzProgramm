using UnityEngine;
using UnityEngine.SceneManagement;

public class MainLogoutController : MonoBehaviour
{
    [SerializeField] private string loginSceneName = "LoginScene";

    public void logout()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.logout();
        }
        else
        {
            Debug.LogWarning("Logout: StateManager wurde nicht gefunden.");
        }

        SceneManager.LoadScene(loginSceneName);
    }
}