using System;
using SQLite4Unity3d;

#region Invoice

[Serializable]
public class Invoice
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int companyId { get; set; }

    public string invoiceNumber { get; set; }

    // SQLite speichert string zuverlässiger als DateTime
    public string date { get; set; }

    public string dueDate { get; set; }

    // offen, bezahlt, storniert
    public string status { get; set; }

    public double subtotal { get; set; }

    public double tax { get; set; }

    public double total { get; set; }

    public string notes { get; set; }
}

#endregion

#region InvoiceItem

[Serializable]
public class InvoiceItem
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int invoiceId { get; set; }

    public string description { get; set; }

    public int quantity { get; set; }

    public double unitPrice { get; set; }

    // Nicht in SQLite speichern
    [Ignore]
    public double calculatedTotal
    {
        get { return quantity * unitPrice; }
    }
}

#endregion

#region Offer

[Serializable]
public class Offer
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int companyId { get; set; }

    public string offerNumber { get; set; }

    public string date { get; set; }

    // offen, akzeptiert, abgelehnt
    public string status { get; set; }

    public double subtotal { get; set; }

    public double tax { get; set; }

    public double total { get; set; }

    public string notes { get; set; }
}

#endregion

#region OfferItem

[Serializable]
public class OfferItem
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int offerId { get; set; }

    public string description { get; set; }

    public int quantity { get; set; }

    public double unitPrice { get; set; }

    [Ignore]
    public double calculatedTotal
    {
        get { return quantity * unitPrice; }
    }
}

#endregion