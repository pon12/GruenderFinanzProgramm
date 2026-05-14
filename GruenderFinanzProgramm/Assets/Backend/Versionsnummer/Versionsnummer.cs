using System;
using System.Text;
using UnityEngine;
using System.IO;




public class Versionsnummer
{


    static string fileName = "version.txt";
    // Funktion zum Schreiben der Versionsnummer in eine Datei
    public static void writeVersionToFile(string version)
    {
        string filePath = Path.Combine(Application.dataPath, "Backend/Versionsnummer/" + fileName);
        {
            // Versionsnummer in die  Datei schreiben
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine(version + "\n");
            }
            // Debug-Ausgabe des Pfads zur Datei
            Debug.Log($"Versionsnummer in folgende Datei angehängt: {fileName}");
        }
    }

    // Funktion zum Lesen der Versionsnummer aus einer Datei
    public static string getVersion()
    {
        // Pfad zur Datei erstellen
        string filePath = Path.Combine(Application.dataPath, "Backend/Versionsnummer/" + fileName);
        if (File.Exists(filePath))
        {
            // Versionsnummer aus der Datei lesen
            using (StreamReader sr = new StreamReader(filePath))
            {
                string version = sr.ReadToEnd();
                return version;
            }
        }
        else
        {
            // Datei nicht gefunden
            Debug.LogError($"Datei nicht gefunden: {fileName}");
            return null;
        }
    }
}