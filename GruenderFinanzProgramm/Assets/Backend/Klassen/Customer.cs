using SQLite;
using System;

[System.Serializable]
public class Customer
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string name { get; set; }
    public string street { get; set; }
    public string postalCode { get; set; }
    public string city { get; set; }
    public string email { get; set; }
    public string phone { get; set; }

    public DateTime lastUpdated { get; set; } = DateTime.Now;
}