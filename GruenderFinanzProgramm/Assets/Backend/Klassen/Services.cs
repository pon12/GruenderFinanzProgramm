using SQLite;
using System;

[System.Serializable]
public class Service
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string name { get; set; }
    public string description { get; set; }
    public double price { get; set; }

    public string priceModel {get; set; }
}