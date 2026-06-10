using SQLite4Unity3d;
using System.Collections.Generic;
using System;


[System.Serializable]
public class UserDB
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string name { get; set; }
    public string passKeyHash { get; set; }
    public string recoveryPassKeyHash { get; set; }
}


public class Company
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string name { get; set; }       // Firmenname
    public int legalForm { get; set; }     // Rechtsform (int)
    public int industry { get; set; }      // Branche (int)
    public string location { get; set; }   // Standort
    // --- Mapping Listen ---
    public static readonly List<string> LegalForms = new List<string>
    {
        "GmbH", "KG", "AG", "OHG", "GbR",
        "UG (haftungsbeschränkt)", "Einzelunternehmen", "GmbH & Co. KG", "eG"
    };
    public static readonly List<string> Industries = new List<string>
    {
        "IT & Software", "Handel", "Produktion", "Dienstleistung",
        "Gesundheitswesen", "Bauwesen", "Gastronomie",
        "Finanzwesen", "Logistik"
    };
    // --- Getter für lesbare Strings ---
    [Ignore]
    public string LegalFormName =>
        (legalForm >= 0 && legalForm < LegalForms.Count)
            ? LegalForms[legalForm]
            : "Unbekannt";
    [Ignore]
    public string IndustryName =>
        (industry >= 0 && industry < Industries.Count)
            ? Industries[industry]
            : "Unbekannt";
}

[System.Serializable]
public class UserDocument
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int documentType { get; set; }

    public string title { get; set; }

    public string text { get; set; }
}

public class ExportEintrag
{
    // Primärschlüssel für SQLite (wie in der Doku gefordert)
    public int id { get; set; }
    public string bezeichnung { get; set; }
    public string art { get; set; }
    public string format { get; set; }
    public string pfad { get; set; }
    public DateTime lastUpdated { get; set; }
}


[System.Serializable]
public class Dauerauftrag
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string typ { get; set; }              // "Einnahme" oder "Ausgabe"
    public float amount { get; set; }
    public string description { get; set; }

    public string startDatum { get; set; }
    public string naechstesDatum { get; set; }

    public int intervallTyp { get; set; }        // 1 = monatlich, 2 = jährlich
    public bool isActive { get; set; }

    public System.DateTime lastUpdated { get; set; } = System.DateTime.Now;
}