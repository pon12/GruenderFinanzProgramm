using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EiAu
{
    public List<EinkommenEntry> einkommenEntries;
    public List<AusgabenEntry> ausgabenEntries;

    public EiAu()
    {
        einkommenEntries = new List<EinkommenEntry>();
        ausgabenEntries = new List<AusgabenEntry>();
    }

    // Methode zum Hinzufügen einer Einnahme
    public void addEinkommen(float amount, string description)
    {
        var einkommenEntry = new EinkommenEntry { Amount = amount, Description = description };
        einkommenEntries.Add(einkommenEntry);
    }

    // Methode zum Hinzufügen einer Ausgabe
    public void addAusgabe(float amount, string description)
    {
        var ausgabenEntry = new AusgabenEntry { Amount = amount, Description = description };
        ausgabenEntries.Add(ausgabenEntry);
    }

    // Methode zum Berechnen der Gesamteinnahmen, Gesamtausgaben und Differenz
    public float berechneTotalesEinkommen()
    {
        return einkommenEntries.Sum(e => e.Amount);
    }

    public float berechneTotaleAusgaben()
    {
        return ausgabenEntries.Sum(e => e.Amount);
    }

    public float berechneDifferenz()
    {
        return berechneTotalesEinkommen() - berechneTotaleAusgaben();
    }
}

public class EinkommenEntry
{
    public float Amount { get; set; }
    public string Description { get; set; }
}

public class AusgabenEntry
{
    public float Amount { get; set; }
    public string Description { get; set; }
}