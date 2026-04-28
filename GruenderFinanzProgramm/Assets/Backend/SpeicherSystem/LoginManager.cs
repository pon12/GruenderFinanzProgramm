using UnityEngine;
using TMPro;

public class LoginManager : MonoBehaviour
{
    //Zuordnung der Inputfelder für die Registrierung.
    [Header("Register")]
    [SerializeField] TMP_InputField Reg_Email;
    [SerializeField] TMP_InputField Reg_Password;
    [SerializeField] TMP_InputField Reg_Username;

    // Funktion die bei Klick auf den Registrieren Button ausgeführt wird.
    public async void OnRegisterPressed()
    {
        if (string.IsNullOrWhiteSpace(Reg_Email.text))  
        {
            Debug.LogError("Fülle alle Felder aus");
            return;
        }
        if (string.IsNullOrWhiteSpace(Reg_Password.text))  
        {
            Debug.LogError("Fülle alle Felder aus");
            return;
        }
        if (string.IsNullOrWhiteSpace(Reg_Username.text))  
        {
            Debug.LogError("Fülle alle Felder aus");
            return;
        }
        if (await MySQLManager.RegisterUser(Reg_Email.text, Reg_Password.text, Reg_Username.text))
        {
           print("User registriert");
        }
        else
        {
            print("User konnte nicht registriert werden");
       };
    }
    
}
