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
    public string steuerNr { get; set; }       // Steuernummer 
    public string gruendungsJahr { get; set; }  // Gründungsjahr 
    public string handelsReg { get; set; }      // Handelsregisternummer
    public string strasseuHausNr { get; set; }         // Straße und Hausnummer
    public string plz { get; set; }         // Postleitzahl 
    public string ustIdNr { get; set; }         // Umsatzsteuer-Identifikationsnummer
    

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

public class Settings
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; } 
   public string rechnungsNrPräfix { get; set; }
   public string startNr { get; set; }
   public string zahlungsziel { get; set; } 
   public int waehrung { get; set; }
   public int dtmFormat { get; set; }
   public bool ustRechnung { get; set; }
   public bool autoNummer { get; set; }
   public string zahlungshinweis { get; set; }
    public string kontoInhaber { get; set; }
    public string iban { get; set; }
    public string bic { get; set; }
    public string kreditinstitut { get; set; }
    public bool ibanRechnung { get; set; }
    public bool logo { get; set; }
    public bool seitenzahl { get; set; }
    public bool exportpfad { get; set; }
    public int steuersatz { get; set; }
    public bool Begleiter { get; set; }
}
