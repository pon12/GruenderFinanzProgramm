using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.IO;

public class PasswordHashTestOld : MonoBehaviour
{
     private string hashedPassword;

// Methode zum Hashen des Passworts und Schreiben in eine Datei
    public void writeHashedPasswordToFile(string fileName, string password)
    {
    // Pfad zur Datei im Assets-Ordner
    string filePath = Path.Combine(Application.dataPath, "Backend/Authentication/Test/" + fileName);
    // Überprüfen, ob die Datei bereits existiert
    if (!File.Exists(filePath))
    {
        // Verzeichnis erstellen, falls erforderlich
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        // Passwort hashen
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            string hashedPassword = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            // Hashed Passwort in die Datei schreiben
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine(hashedPassword);
            }
            // Debug-Ausgabe des Pfads zur erstellten Datei
            Debug.Log($"Passord in folgende Datei geschrieben: {fileName}");
        }
    }
    // Wenn die Datei bereits existiert, wird das gehashte Passwort angehängt
    else
    {   
        // Passwort hashen
        using (StreamWriter sw = File.AppendText(filePath))
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                string hashedPassword = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                // Hashed Passwort in die Datei anhängen
                sw.WriteLine(hashedPassword);
            }
        }
        // Debug-Ausgabe des Pfads zur aktualisierten Datei
        Debug.Log($"Password in folgende exestierende Datei geschrieben: {filePath}");
    }
    }
    public void Start()
    {
    writeHashedPasswordToFile("hashed_password.txt", "TestPasswort123");
    hashedPassword = getHashedPassword();
    }

    public string getHashedPassword()
    {
        return hashedPassword;
    }
}