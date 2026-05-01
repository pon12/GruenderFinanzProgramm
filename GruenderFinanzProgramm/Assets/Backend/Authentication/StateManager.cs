// Merkt sich ob jemand eingelogt ist und welcher Nutzer aktiv ist.

using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    private bool loggedIn = false;
    private User currentUser;

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

    public void login(User user)
    {
        currentUser = user;
        loggedIn = true;
    }

    public void logout()
    {
        currentUser = null;
        loggedIn = false;
    }

    public bool isLoggedIn()
    {
        return loggedIn;
    }

    public User getCurrentUser()
    {
        return currentUser;
    }
}