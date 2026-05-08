using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    private bool loggedIn = false;
    private PassKeyRecord currentUser;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void login(PassKeyRecord user)
    {
        currentUser = user;
        loggedIn = true;

        Debug.Log("Session gestartet für Nutzer: " + user.username);
    }

    public void logout()
    {
        if (!loggedIn)
        {
            Debug.LogWarning("Logout nicht möglich: Kein Nutzer ist eingeloggt.");
            return;
        }

        Debug.Log("Logout erfolgreich für Nutzer: " + currentUser.username);

        currentUser = null;
        loggedIn = false;
    }

    public bool isLoggedIn()
    {
        return loggedIn;
    }

    public PassKeyRecord getCurrentUser()
    {
        return currentUser;
    }
}