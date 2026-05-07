using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.IO;

public class PasswodCheckTest : MonoBehaviour
{
    public void checkHashedPasswordExample()
    {
        string password = "examplePassword";
        string hashedPassword = PasswordHashing.hashPassword(password);
        bool passwordExists = PasswordHashing.checkHashedPassword("hashedPasswordTest.txt", hashedPassword);
        Debug.Log($"Existiert das gehashte Passwort in der Datei? {passwordExists}");
    }
    public void Start()
    {
        checkHashedPasswordExample();
    }
}