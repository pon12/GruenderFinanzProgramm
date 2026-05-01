// Skript für: "Noch kein Konto? Registrieren"

using UnityEngine;

public class AuthPanelController : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    private void Start()
    {
        showLoginPanel();
    }

    public void showLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
    }

    public void showRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }
}