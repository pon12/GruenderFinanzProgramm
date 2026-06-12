using System.Collections.Generic;
using UnityEngine;

public static class OfferInvoiceService
{
    public static bool ConvertOfferToInvoice(
        Offer offer,
        List<OfferItem> offerItems,
        DataBase db
    )
    {
        if (offer == null)
        {
            Debug.LogError("Offer ist null.");
            return false;
        }

        Invoice invoice = new Invoice
        {
            companyId = offer.companyId,
            customerId = offer.customerId,

            invoiceNumber = "RE-" + System.DateTime.Now.ToString("yyyyMMdd-HHmm"),

            date = System.DateTime.Now.ToString("dd.MM.yyyy"),

            dueDate = System.DateTime.Now
                .AddDays(14)
                .ToString("dd.MM.yyyy"),

            status = "Entwurf",

            subtotal = offer.subtotal,
            tax = offer.tax,
            total = offer.total,

            notes = offer.notes
        };

        int invoiceId = db.createInvoice(invoice);

        foreach (var offerItem in offerItems)
        {
            InvoiceItem invoiceItem = new InvoiceItem
            {
                invoiceId = invoiceId,

                description = offerItem.description,

                quantity = offerItem.quantity,

                unitPrice = offerItem.unitPrice
            };

            db.createInvoiceItem(invoiceItem);
        }

        offer.status = "Angenommen";

        db.updateOffer(offer);

        Debug.Log("Angebot erfolgreich in Rechnung umgewandelt.");

        return true;
    }
}