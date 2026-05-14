using SQLite4Unity3d;
using System;
[System.Serializable]
public class Customer
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    // Allgemeine Kundendaten
    public string name { get; set; }          // Firmenname oder Privatperson
    public string street { get; set; }        // Straße + Hausnummer
    public string postalCode { get; set; }    // PLZ
    public string city { get; set; }
    public string country { get; set; }
    // Kontaktmöglichkeiten (nicht alle müssen gesetzt sein)
    public string email { get; set; }
    public string phone { get; set; }
    public string alternativeContact { get; set; } // z. B. zweite Person / Notiz
    // Zeitpunkt der Erstellung oder letzten Änderung
    public DateTime lastUpdated { get; set; } = DateTime.Now;
}