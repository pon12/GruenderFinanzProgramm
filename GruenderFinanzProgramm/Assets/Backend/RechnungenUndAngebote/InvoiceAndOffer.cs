using System;
using SQLite;
using System.Collections.Generic;
using System.Linq;

#region Invoice

[Serializable]
public class Invoice
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int customerId { get; set; }
    public string invoiceNumber { get; set; }
    public string date { get; set; }
    public string dueDate { get; set; }
    public string status { get; set; }
    public double subtotal { get; set; }
    public double tax { get; set; }
    public double total { get; set; }
    public string notes { get; set; }
    [Ignore]
    public string companyName { get; set; }
    [Ignore]
    public string companyAddress { get; set; }
    [Ignore]
    public string customerName { get; set; }
    [Ignore]
    public string customerAddress { get; set; }
    public double discount { get; set; }
    public double extraCosts { get; set; }
    // AUTOMATISCHE BERECHNUNG
    public void CalculateTotals(List<InvoiceItem> items)
    {
        subtotal = items.Sum(i => i.calculatedTotal);
        tax = subtotal * 0.19;
        total = subtotal + tax;
    }
    // Kassenbuch-Anbindung
    public bool bookedToCashbook { get; set; }
    public int cashbookEntryId { get; set; }
    public string bookingDate { get; set; }
}

#endregion

#region InvoiceItem

[Serializable]
public class InvoiceItem
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int invoiceId { get; set; }
    public string articleNumber { get; set; }
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
    public int customerId { get; set; } 
    public string offerNumber { get; set; }
    public string date { get; set; }
    public string validUntil { get; set; }
    // offen, akzeptiert, abgelehnt
    public string status { get; set; }
    public double subtotal { get; set; }
    public double tax { get; set; }
    public double total { get; set; }
    public string notes { get; set; }
    public double discount { get; set; }
    public double extraCosts { get; set; }
   
    // Kassenbuch-Anbindung
    public bool bookedToCashbook { get; set; }
    public int cashbookEntryId { get; set; }
    public string bookingDate { get; set; }
    [Ignore]
   
    public string customerName { get; set; }
    [Ignore]
    public string customerAddress { get; set; }
    [Ignore]
    public string companyName { get; set; }
    [Ignore]
    public string companyAddress { get; set; }
}
#endregion

#region OfferItem
[Serializable]
public class OfferItem
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public int offerId { get; set; }
    public string articleNumber { get; set; }
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