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
        string testVersion = "Version:1.0.0";

        // Versionsnummer in die Datei schreiben
        Versionsnummer.writeVersionToFile(testVersion);
    }

    // Funktion zum Testen der lesen einer Versionsnummer aus einer Datei
    public void getVersion()
    {
        // Versionsnummer aus der Datei lesen
        string version = Versionsnummer.getVersion();
        if (version != null)
        {
            Debug.Log($"Gelesene Versionsnummer: {version}");
        }
    }

    public void Start()
    {
        writeTestVersionen();
        getVersion();
    }
}