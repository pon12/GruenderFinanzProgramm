using UnityEngine;
using System.Collections.Generic;

public class TestEiAu : MonoBehaviour
{
    void Start()
    {
        EiAu eiAu = new EiAu();
        eiAu.addEinkommen(1000, "Gehalt");
        eiAu.addEinkommen(200, "Freelance Projekt");
        eiAu.addAusgabe(300, "Miete");
        eiAu.addAusgabe(150, "Lebensmittel");

        Debug.Log("Totales Einkommen: " + eiAu.berechneTotalesEinkommen());
        Debug.Log("Totale Ausgaben: " + eiAu.berechneTotaleAusgaben());
        Debug.Log("Differenz: " + eiAu.berechneDifferenz());

    // Teste die Beschreibungen der Einträge
    Dictionary<string, string> descriptions = new Dictionary<string, string>();
    foreach (var entry in eiAu.ausgabenEntries)
    {
        descriptions[entry.Description] = entry.Description;
    }
    foreach (var entry in eiAu.einkommenEntries)
    {
        descriptions[entry.Description] = entry.Description;
    }

    Debug.Log("Descriptions: " + string.Join(", ", descriptions.Keys));
    }
}