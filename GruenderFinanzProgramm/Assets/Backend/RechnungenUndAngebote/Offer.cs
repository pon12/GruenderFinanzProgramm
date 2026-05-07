using SQLite4Unity3d;
using System;
[System.Serializable]
public class Offer
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int companyId { get; set; }
    public string offerNumber { get; set; }
    public DateTime date { get; set; }
    public string status { get; set; } // offen, akzeptiert, abgelehnt
    public double subtotal { get; set; }
    public double tax { get; set; }
    public double total { get; set; }
    public string notes { get; set; }
}