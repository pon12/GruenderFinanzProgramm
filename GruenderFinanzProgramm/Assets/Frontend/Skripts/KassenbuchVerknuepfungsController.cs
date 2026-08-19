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

        float bruttoBetrag = (float)Math.Round(invoice.total, 2, MidpointRounding.AwayFromZero);
        float steuerBetrag = (float)Math.Round(invoice.tax, 2, MidpointRounding.AwayFromZero);
        string datum = DateTime.Now.ToString("dd.MM.yyyy");

        string beschreibungsPraefix = "Rechnung " + invoice.invoiceNumber;
        string beschreibung = beschreibungsPraefix +
            (!string.IsNullOrEmpty(invoice.customerName) ? " - " + invoice.customerName : "");

        // FIX: enthaltene Umsatzsteuer wurde bisher komplett ignoriert - nur
        // der Bruttobetrag landete als "Umsatzerlöse" im Kassenbuch, die
        // Steuer tauchte nirgends in der Finanzen-Auswertung auf. Jetzt wird
        // sie zusätzlich als eigene "Steuern"-Ausgabe gebucht, mit
        // eindeutigem Präfix "USt. <Rechnungsnummer>" zum Wiederfinden.
        string steuerPraefix = "USt. " + invoice.invoiceNumber;
        string steuerBeschreibung = steuerPraefix +
            (!string.IsNullOrEmpty(invoice.customerName) ? " - " + invoice.customerName : "");

        var vorhandenerEintrag = db.getAllEinkommenEntries()
            ?.Find(e => e.Description != null && e.Description.StartsWith(beschreibungsPraefix));
        // updateAusgaben() gibt es im Backend nicht (nur create/delete) -
        // deshalb bei Änderungen: alten Steuer-Eintrag löschen, neuen anlegen.
        var vorhandenerSteuerEintrag = db.getAllAusgabenEntries()
            ?.Find(a => a.Description != null && a.Description.StartsWith(steuerPraefix));

        if (invoice.status == "Bezahlt")
        {
            if (vorhandenerEintrag != null)
            {
                vorhandenerEintrag.Amount = bruttoBetrag;
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
                    bruttoBetrag, beschreibung, datum, "Umsatzerlöse");

                invoice.bookedToCashbook = true;
                invoice.cashbookEntryId = neueId;
                db.updateInvoice(invoice);
            }

            // Steueranteil (falls vorhanden) als eigene Ausgabe buchen/erneuern
            if (vorhandenerSteuerEintrag != null) db.deleteAusgaben(vorhandenerSteuerEintrag.Id);
            if (steuerBetrag > 0)
                db.createAusgaben(steuerBetrag, steuerBeschreibung, datum, "Steuern");
        }
        else
        {
            if (vorhandenerEintrag != null) db.deleteEinkommen(vorhandenerEintrag.Id);
            if (vorhandenerSteuerEintrag != null) db.deleteAusgaben(vorhandenerSteuerEintrag.Id);

            invoice.bookedToCashbook = false;
            invoice.cashbookEntryId = 0;
            db.updateInvoice(invoice);
        }
    }
}