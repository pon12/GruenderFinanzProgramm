using SQLite4Unity3d;
using System;
[System.Serializable]
public class Service
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int companyId { get; set; }
   
    // Grundinformationen
    [NotNull]
    public string name { get; set; }//Name der Dienstleistung
    public string description { get; set; }//Beschreibung der Dienstleistung (optional)
   
    // Preisangaben
     [NotNull]
    public double unitPrice { get; set; }     // Preis pro Einheit
     [NotNull]
    public int quantity { get; set; }         // Anzahl
     [NotNull]
    public string unitType { get; set; }      // z. B. "Stunden", "Stück"
    
    [Ignore]
    public double totalPrice => unitPrice * quantity;// Berechneter Gesamtpreis
    [Ignore]
    public string DisplayText =>
        $"{quantity} {unitType} × {unitPrice:0.00} € = {totalPrice:0.00} €";
    [NotNull]
    public string category { get; set; }//Kategorie der Dienstleistung z. B. "Beratung", "Entwicklung"
     [NotNull]
    public DateTime lastUpdated { get; set; } = DateTime.Now; //Letzte Aktualisierung 
}