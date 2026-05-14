using SQLite4Unity3d;
using System;
[System.Serializable]
public class Service
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string name { get; set; }          // Name der Dienstleistung
    public string description { get; set; }   // Beschreibung
    public double price { get; set; }         // Preis (Gesamt oder pro Einheit)
    public DateTime lastUpdated { get; set; } = DateTime.Now;
}