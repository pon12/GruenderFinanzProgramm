using UnityEngine;

public class StateManager : MonoBehaviour
{

    [SerializeField] private int testHardCodedPK = 1234;
    private bool loggedIn = false;

    public bool validatePassKey(int PassKey)
    {
        if (PassKey == testHardCodedPK)
        {
            loggedIn = true;
            return true;
        }

        loggedIn = false;
        return false;

    }

    public bool isLoggedIn()
    {
        return loggedIn;
    }

}