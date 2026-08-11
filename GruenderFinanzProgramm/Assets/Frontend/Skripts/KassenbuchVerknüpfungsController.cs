using System;
using System.Linq;

// Verknüpft eine Rechnung mit einem Kassenbucheintrag: bucht bei Status "Bezahlt",
// aktualisiert den Betrag bei Änderungen und entfernt den Eintrag bei jedem anderen
// Status (z. B. "Storniert"). Sucht bewusst aktiv nach einem vorhandenen Eintrag
// über die Rechnungsnummer, statt sich allein auf das Feld bookedToCashbook zu
// verlassen - das Feld kann beim Bearbeiten-Speichern zurückgesetzt werden,
// ohne dass der eigentliche Kassenbucheintrag verschwindet.
public static class KassenbuchVerknuepfungController
{
    public static void Aktualisiere(Invoice invoice, DataBase db)
    {
        if (invoice == null || db == null) return;

        float betrag = (float)Math.Round(invoice.total, 2, MidpointRounding.AwayFromZero);
        string beschreibungsPraefix = "Rechnung " + invoice.invoiceNumber;
        string beschreibung = beschreibungsPraefix +
            (!string.IsNullOrEmpty(invoice.customerName) ? " - " + invoice.customerName : "");

        var vorhandenerEintrag = db.getAllEinkommenEntries()
            ?.Find(e => e.Description != null && e.Description.StartsWith(beschreibungsPraefix));

        if (invoice.status == "Bezahlt")
        {
            if (vorhandenerEintrag != null)
            {
                vorhandenerEintrag.Amount = betrag;
                vorhandenerEintrag.Description = beschreibung;
                db.updateEinkommen(vorhandenerEintrag);

                if (!invoice.bookedToCashbook || invoice.cashbookEntryId != vorhandenerEintrag.Id)
                {
                    invoice.bookedToCashbook = true;
                    invoice.cashbookEntryId = vorhandenerEintrag.Id;
                    db.updateInvoice(invoice);
                }
            }
            else
            {
                int neueId = db.createEinkommen(
                    betrag, beschreibung, DateTime.Now.ToString("dd.MM.yyyy"), "Umsatzerlöse");

                invoice.bookedToCashbook = true;
                invoice.cashbookEntryId = neueId;
                db.updateInvoice(invoice);
            }
        }
        else if (vorhandenerEintrag != null)
        {
            db.deleteEinkommen(vorhandenerEintrag.Id);
            invoice.bookedToCashbook = false;
            invoice.cashbookEntryId = 0;
            db.updateInvoice(invoice);
        }
    }
}