using SQLite4Unity3d;
using System;

[System.Serializable]
public class Service
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string name { get; set; }
    public string description { get; set; }
    public double price { get; set; }

    public DateTime lastUpdated { get; set; } = DateTime.Now;
}