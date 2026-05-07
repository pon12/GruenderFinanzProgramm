using SQLite4Unity3d;
[System.Serializable]
public class InvoiceItem
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int invoiceId { get; set; }
    public string description { get; set; }
    public int quantity { get; set; }
    public double unitPrice { get; set; }
    public double total => quantity * unitPrice;
}