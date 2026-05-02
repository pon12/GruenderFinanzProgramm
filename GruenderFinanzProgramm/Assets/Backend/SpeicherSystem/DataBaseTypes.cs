using SQLite4Unity3d;
using System.Collections.Generic;


[System.Serializable]
public class UserDB
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    
    public string name { get; set; }
    public int passKey { get; set; }
    public int recoveryKey { get; set; }
    public bool isLoggedIn { get; set; }
}

[System.Serializable]
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