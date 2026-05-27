using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SQLite4Unity3d;

public class Einkommen
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public float Amount { get; set; }
    public string Description { get; set; }
}

public class Ausgaben
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public float Amount { get; set; }
    public string Description { get; set; }
}