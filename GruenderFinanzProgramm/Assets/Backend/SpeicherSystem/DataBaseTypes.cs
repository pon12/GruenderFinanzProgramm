using SQLite;
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
    public int plz { get; set; }         // Postleitzahl 
    public string ustIdNr { get; set; }         // Umsatzsteuer-Identifikationsnummer 
    public string email { get; set; }       // E-Mail-Adresse
    public string handyNr   { get; set; }    // Standort
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

[System.Serializable]
public class GruenderpfadEintrag
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int meilenstein { get; set; }
    public string beschreibung { get; set; }
    public bool erledigt { get; set; }

    public System.DateTime lastUpdated { get; set; } = System.DateTime.Now;
}

[System.Serializable]
public class TextDocumentMeta
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int userId { get; set; }

    public string title { get; set; }

    public string originalFileName { get; set; }

    public string storedFileName { get; set; }

    public string filePath { get; set; }

    public string documentType { get; set; } // STANDARD, DIAGRAMM, CHECKLIST

    public System.DateTime createdAt { get; set; } = System.DateTime.Now;

    public System.DateTime lastUpdated { get; set; } = System.DateTime.Now;
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
    public string emailFirma { get; set; }
    public string teleponNrFirma { get; set; }
    public string websitefrima { get; set; }
    public string nutzer { get; set; }
}

public class Finanzdaten
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int monat { get; set; }
    public int ausgaben { get; set; }
    public int einahmenTotal { get; set; }
    public int erstellteAng { get; set; }
    public int angenommenAng { get; set; }
    public int erstellteRech { get; set; }
    public int angenommenRech { get; set; }
}