using System.Collections.Generic;

public static class BelegTransferData
{
    public static bool hasTransfer = false;

    public static int customerId;
    public static string customerName;
    public static string customerAddress;

    public static string date;
    public static string dueDate;
    public static string notes;

    public static float rabatt;
    public static float skonto;

    public static List<TransferItem> items = new List<TransferItem>();

    public static void Clear()
    {
        hasTransfer = false;
        customerId = 0;
        customerName = "";
        customerAddress = "";
        date = "";
        dueDate = "";
        notes = "";
        rabatt = 0;
        skonto = 0;
        items.Clear();
    }
}

public class TransferItem
{
    public string articleNumber;
    public string description;
    public int quantity;
    public double unitPrice;
}