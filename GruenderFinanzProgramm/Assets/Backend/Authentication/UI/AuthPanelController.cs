using UnityEngine;

public class AuthPanelController : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject recoveryPanel;

    private void Start()
    {
        showLoginPanel();
    }

    public void showLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);

        if (recoveryPanel != null)
        {
            recoveryPanel.SetActive(false);
        }
    }

    public void showRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);

        if (recoveryPanel != null)
        {
            recoveryPanel.SetActive(false);
        }
    }

    public void showRecoveryPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);

        if (recoveryPanel != null)
        {
            recoveryPanel.SetActive(true);
        }
    }
}