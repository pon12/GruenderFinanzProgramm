using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AngebotController : BelegScreenController
{
    protected override string BelegTyp => "Angebot";
    protected override string NummernPrefix => "AN";

    protected override List<string> StatusOptionen => new List<string>
    {
        "Entwurf",
        "Versendet",
        "Angenommen",
        "Abgelehnt"
    };

    protected override void RegistriereZusatzButtons()
    {
        Button umwandeln = FindeButton(
            "btn-umwandeln",
            "Angebot in Rechnung umwandeln"
        );

        if (umwandeln == null)
        {
            Debug.LogWarning("[Angebot] Button 'Angebot in Rechnung umwandeln' nicht gefunden.");
            return;
        }

        umwandeln.RegisterCallback<ClickEvent>(_ =>
        {
            try
            {
                DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

                if (db == null)
                {
                    FeedbackPopup.Show(
                        Root,
                        "Keine Datenbank gefunden",
                        FeedbackTyp.Fehler
                    );

                    return;
                }

                List<Offer> offers = db.getAllOffers();

                if (offers == null || offers.Count == 0)
                {
                    FeedbackPopup.Show(
                        Root,
                        "Kein Angebot gefunden",
                        FeedbackTyp.Fehler
                    );

                    return;
                }

                Offer letztesOffer = offers[offers.Count - 1];

                List<OfferItem> items =
                    db.getItemsByOffer(letztesOffer.id);

                bool success =
                    OfferInvoiceService.ConvertOfferToInvoice(
                        letztesOffer,
                        items,
                        db
                    );

                FeedbackPopup.Show(
                    Root,
                    success
                        ? "Angebot in Rechnung umgewandelt"
                        : "Umwandlung fehlgeschlagen",
                    success
                        ? FeedbackTyp.Erfolg
                        : FeedbackTyp.Fehler
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Angebot] Fehler bei Umwandlung: " + e);

                FeedbackPopup.Show(
                    Root,
                    "Fehler bei Umwandlung",
                    FeedbackTyp.Fehler
                );
            }
        });
    }
}