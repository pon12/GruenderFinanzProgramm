using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SQLite4Unity3d;

public class Dienstleistung
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Titel { get; set; }
    public string Beschreibung { get; set; }
    public string Detail { get; set; }
    public string Betrag { get; set; }
    public string Anzahl { get; set; }
    public string Preismodell { get; set; }
}