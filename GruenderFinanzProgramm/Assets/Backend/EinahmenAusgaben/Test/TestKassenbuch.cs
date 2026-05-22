using UnityEngine;
using System.Collections.Generic;

public class TestKassenbuch : MonoBehaviour
{
    void Start()
    {
        Kassenbuch kassenbuch = new Kassenbuch();
        kassenbuch.addEinkommen(1000, "Gehalt");
        kassenbuch.addEinkommen(200, "Freelance Projekt");
        kassenbuch.addAusgabe(300, "Miete");
        kassenbuch.addAusgabe(150, "Lebensmittel");

        Debug.Log("Totales Einkommen: " + kassenbuch.berechneTotalesEinkommen());
        Debug.Log("Totale Ausgaben: " + kassenbuch.berechneTotaleAusgaben());
        Debug.Log("Differenz: " + kassenbuch.berechneDifferenz(kassenbuch.berechneTotalesEinkommen(), kassenbuch.berechneTotaleAusgaben()));

    // Teste die Beschreibungen der Einträge
    Dictionary<string, string> descriptions = new Dictionary<string, string>();
    foreach (var entry in kassenbuch.ausgabenEntries)
    {
        descriptions[entry.Description] = entry.Description;
    }
    foreach (var entry in kassenbuch.einkommenEntries)
    {
        descriptions[entry.Description] = entry.Description;
    }

    Debug.Log("Descriptions: " + string.Join(", ", descriptions.Keys));
    }
}