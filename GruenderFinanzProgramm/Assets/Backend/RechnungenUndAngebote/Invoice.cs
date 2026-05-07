using SQLite4Unity3d;
using System;
[System.Serializable]
public class Invoice
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int companyId { get; set; }
    public string invoiceNumber { get; set; }
    public DateTime date { get; set; }
    public string dueDate { get; set; }
    public string status { get; set; } // offen, bezahlt, storniert
    public double subtotal { get; set; }
    public double tax { get; set; }
    public double total { get; set; }
    public string notes { get; set; }
}