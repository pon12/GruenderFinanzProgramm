using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.IO;


// Testklasse für die PasswordHash-Funktionalität
public class PasswordHashTest : MonoBehaviour
{
    public void writeHashedPasswordToFileExample()
    {
        string password = "examplePassword";
        PasswordHashing.writeHashedPasswordToFile("hashedPasswordTest.txt", password);
    }
    public void Start()
    {
        writeHashedPasswordToFileExample();
    }
}