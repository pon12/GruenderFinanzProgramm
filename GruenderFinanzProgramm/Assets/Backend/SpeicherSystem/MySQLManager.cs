using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Networking;

public static class MySQLManager
{
    // Server URL zugriff.
    readonly static string SERVER_URL = "localhost:80/Test";
    // Registrierung eines neuen Benutzers.
    public static async Task<bool> RegisterUser(string email, string password, string username)
    {
        string REGISTER_USER_URL = $"{SERVER_URL}/registerUser.php";
        return await SendPostRequest(REGISTER_USER_URL, new Dictionary<string, string>()
        {
            {"email", email },
            {"password", password },
            {"username", username }
            
        });
    }
    // Überprüfung ob funktioniert.
    static async Task<bool> SendPostRequest(string url, Dictionary<string, string> data)
    {
       using (UnityWebRequest req = UnityWebRequest.Post(url, data))
        {
            req.SendWebRequest();  
            while (!req.isDone) await Task.Delay(100);
           
           if (req.error != null
            || !string.IsNullOrWhiteSpace(req.error)
            || HasErrorMessage(req.downloadHandler.text))
        return false;
        }
         return true;
    }
    // Überprüfung ob die Antwort eine Fehlermeldung ist.
     static bool HasErrorMessage(string msg) => int.TryParse(msg, out var res);
}