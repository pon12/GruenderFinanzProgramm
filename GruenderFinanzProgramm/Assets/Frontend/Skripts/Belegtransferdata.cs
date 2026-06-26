using System.Collections.Generic;

// Überträgt Daten vom Angebots- zum Rechnungsscreen beim Umwandeln.
// Wird in AngebotController befüllt und in BelegScreenController ausgelesen.
public static class BelegTransferData
{
    public static bool         hasTransfer     = false;
    public static int          customerId      = -1;
    public static string       customerName    = "";
    public static string       customerAddress = "";
    public static string       date            = "";
    public static string       dueDate         = "";
    public static string       notes           = "";
    public static float        rabatt          = 0f;
    public static float        skonto          = 0f;
    public static List<TransferItem> items     = new List<TransferItem>();

    public static void Clear()
    {
        hasTransfer     = false;
        customerId      = -1;
        customerName    = "";
        customerAddress = "";
        date            = "";
        dueDate         = "";
        notes           = "";
        rabatt          = 0f;
        skonto          = 0f;
        items           = new List<TransferItem>();
    }
}

public class TransferItem
{
    public string articleNumber;
    public string description;
    public int    quantity;
    public float  unitPrice;
}
