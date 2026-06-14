using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SQLite;

public class Kassenbuch
{
    public List<Einkommen> einkommenEntries;
    public List<Ausgaben> ausgabenEntries;

    public Kassenbuch()
    {
        einkommenEntries = new List<Einkommen>();
        ausgabenEntries = new List<Ausgaben>();
    }

    // Methode zum Hinzufügen einer Einnahme
    public void addEinkommen(float amount, string description)
    {
        var einkommenEntry = new Einkommen   { Amount = amount, Description = description };
        einkommenEntries.Add(einkommenEntry);
    }

    // Methode zum Hinzufügen einer Ausgabe
    public void addAusgabe(float amount, string description)
    {
        var ausgabenEntry = new Ausgaben { Amount = amount, Description = description };
        ausgabenEntries.Add(ausgabenEntry);
    }

    // Methode zum Berechnen der Gesamteinnahmen, Gesamtausgaben und Differenz
    public float berechneTotalesEinkommen()
    {
        float totalEinkommen = einkommenEntries.Sum(e => e.Amount);
        return totalEinkommen;
    }

    public float berechneTotaleAusgaben()
    {
        float totalAusgaben = ausgabenEntries.Sum(e => e.Amount);
        return totalAusgaben;
    }

    public float berechneDifferenzAuto()
    {
        float totalEinkommen = berechneTotalesEinkommen();
        float totalAusgaben = berechneTotaleAusgaben();
        return totalEinkommen - totalAusgaben;
    }

    public float berechneDifferenz(float totalEinkommen, float totalAusgaben)
    {
        return totalEinkommen - totalAusgaben;
    }

    public List<Einkommen> getEinkommenEntries()
    {
    return einkommenEntries;
    }

    public List<Ausgaben> getAusgabenEntries()
    {
    return ausgabenEntries;
    }
    
}