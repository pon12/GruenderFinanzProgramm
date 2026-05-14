using System;
using System.Text;
using UnityEngine;
using System.IO;



// Testklasse für die Versionsnummer-Funktionalität
public class TestVersionsnummer : MonoBehaviour
{ 
    
    // Funktion zum Testen der schreiben einer Versionsnummer in eine Datei.
    public void writeTestVersionen()
    {
        // Testdaten
        string testFileName = "test_version.txt";
        string testVersion = "Version:1.0.0";

        // Versionsnummer in die Datei schreiben
        Versionsnummer.writeVersionToFile(testFileName, testVersion);
    }

    // Funktion zum Testen der lesen einer Versionsnummer aus einer Datei
    public void readTestVersionen()
    {
        // Testdaten
        string testFileName = "test_version.txt";

        // Versionsnummer aus der Datei lesen
        string version = Versionsnummer.readVersionFromFile(testFileName);
        if (version != null)
        {
            Debug.Log($"Gelesene Versionsnummer: {version}");
        }
    }

    public void Start()
    {
        writeTestVersionen();
        readTestVersionen();
    }
}