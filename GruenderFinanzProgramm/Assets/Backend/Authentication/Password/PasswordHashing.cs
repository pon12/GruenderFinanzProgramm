using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.IO;

public class PasswordHashing
{
    // Funktion zum Hashen eines Passworts mit SHA256
    public static string hashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
    }

    // Funktion zum Schreiben eines gehashten Passworts in eine Datei
    public static void writeHashedPasswordToFile(string fileName, string password)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Password", "Test", fileName);
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
            string hashedPassword = hashPassword(password);
            // Hashed Passwort in die Datei schreiben
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine(hashedPassword + "\n");
            }
            // Debug-Ausgabe des Pfads zur erstellten Datei
            Debug.Log($"Password in folgende Datei geschrieben: {fileName}");
        }
        else
        {
            // Passwort hashen
            string hashedPassword = hashPassword(password);
            // Hashed Passwort in die exestierende Datei schreiben
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(hashedPassword + "\n");
            }
            // Debug-Ausgabe des Pfads zur aktualisierten Datei
            Debug.Log($"Password in folgende Datei angehängt: {fileName}");
        }
    }

    // Funktion zum Überprüfen, ob ein gehashtes Passwort in der Datei existiert
    public static bool checkHashedPassword(string fileName, string hashedPassword)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Password", "Test", fileName);

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.Trim() == hashedPassword)
                {
                    Debug.Log("Gehashtes Passwort gefunden.");
                    return true;
                }
            }
        }

        Debug.Log("Gehashtes Passwort nicht gefunden.");
        return false;
    }

}