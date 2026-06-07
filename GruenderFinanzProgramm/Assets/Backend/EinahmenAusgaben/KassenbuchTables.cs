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
    public string Datum { get; set; }

    public int getId()
    {
        return Id;
    }

    public string getDescription()
    {
        return Description;
    }
    public string getAmount()
    {
        return Amount.ToString();
    }
    public string getDatum()
    {
        return Datum;
    }
}

public class Ausgaben
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public float Amount { get; set; }
    public string Description { get; set; }
    public string Datum { get; set; }

    public int getId()
    {
        return Id;
    }

    public string getDescription()
    {
        return Description;
    }
    public string getAmount()
    {
        return Amount.ToString();
    }
    public string getDatum()
    {
        return Datum;
    }
}