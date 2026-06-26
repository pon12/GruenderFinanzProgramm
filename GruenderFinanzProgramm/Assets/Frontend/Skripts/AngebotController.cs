using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class AngebotController : BelegScreenController
{
    protected override string BelegTyp      => "Angebot";
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
        Button umwandeln = FindeButton("btn-umwandeln", "Angebot in Rechnung umwandeln");
        if (umwandeln == null)
        {
            Debug.LogWarning("[Angebot] Umwandeln-Button nicht gefunden.");
            return;
        }
        umwandeln.RegisterCallback<ClickEvent>(_ => WandleInRechnungUm());
    }

    private void WandleInRechnungUm()
    {
        try
        {
            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                FeedbackPopup.Show(Root, "Keine Datenbank gefunden", FeedbackTyp.Fehler);
                return;
            }

            // Aktuellen Rabatt berechnen
            float netto      = ParseBetrag(_nettoLabel != null ? _nettoLabel.text : "0");
            string rabattTyp = _rabattTypDropdown != null ? _rabattTypDropdown.value : "Kein Rabatt";
            float rabattWert = ParseBetrag(_rabattWertFeld != null ? _rabattWertFeld.value : "0");
            float rabatt     = 0f;
            if (rabattTyp == "Prozent")      rabatt = netto * rabattWert / 100f;
            else if (rabattTyp == "Festbetrag") rabatt = rabattWert;

            float mwstSatz      = HoleMwstSatz();
            float steuerBasis   = netto - rabatt;
            float steuer        = steuerBasis * mwstSatz;
            float zwischenbetrag = steuerBasis + steuer;
            float skontoProzent  = ParseBetrag(_skontoWertFeld != null ? _skontoWertFeld.value : "0");
            float skonto         = zwischenbetrag * skontoProzent / 100f;

            // Angebot in DB speichern
            Offer offer = new Offer
            {
                customerId      = _ausgewaehlterKundeId,
                customerName    = _ausgewaehlterKunde,
                customerAddress = _ausgewaehlterKundeAdresse,
                companyName     = HoleCompanyName(db),
                companyAddress  = HoleCompanyAddress(db),
                offerNumber     = _nummerFeld != null ? _nummerFeld.value : "",
                date            = _datumFeld != null ? _datumFeld.value : System.DateTime.Now.ToString("dd.MM.yyyy"),
                validUntil      = _fristFeld != null ? _fristFeld.value : "",
                status          = "Angenommen",
                subtotal        = (double)netto,
                discount        = (double)rabatt,
                extraCosts      = (double)skonto,
                tax             = (double)steuer,
                total           = (double)(zwischenbetrag - skonto),
                notes           = _notizenFeld != null ? _notizenFeld.value : "",
                bookedToCashbook = false,
                cashbookEntryId  = 0,
                bookingDate      = ""
            };

            int offerId = db.createOffer(offer);

            // BelegTransferData befüllen — wird im Rechnungsscreen ausgelesen
            BelegTransferData.Clear();
            BelegTransferData.hasTransfer     = true;
            BelegTransferData.customerId      = _ausgewaehlterKundeId;
            BelegTransferData.customerName    = _ausgewaehlterKunde;
            BelegTransferData.customerAddress = _ausgewaehlterKundeAdresse;
            BelegTransferData.date            = _datumFeld != null ? _datumFeld.value : "";
            BelegTransferData.dueDate         = _fristFeld != null ? _fristFeld.value : "";
            BelegTransferData.notes           = _notizenFeld != null ? _notizenFeld.value : "";
            BelegTransferData.rabatt          = rabatt;
            BelegTransferData.skonto          = skonto;

            foreach (var zeile in _zeilen.ToList())
            {
                OfferItem offerItem = new OfferItem
                {
                    offerId       = offerId,
                    articleNumber = zeile.Artikel      != null ? zeile.Artikel.text    : "",
                    description   = zeile.Beschreibung != null ? zeile.Beschreibung.text : "",
                    quantity      = Mathf.RoundToInt(ParseBetrag(zeile.Menge != null ? zeile.Menge.value : "0")),
                    unitPrice     = ParseBetrag(zeile.Preis != null ? zeile.Preis.text : "0")
                };

                db.createOfferItem(offerItem);

                BelegTransferData.items.Add(new TransferItem
                {
                    articleNumber = offerItem.articleNumber,
                    description   = offerItem.description,
                    quantity      = offerItem.quantity,
                    unitPrice     = (float)offerItem.unitPrice
                });
            }

            // Status setzen und sofort Scene wechseln
            _statusDropdown?.SetValueWithoutNotify("Angenommen");
            SceneManager.LoadScene("Rechnung");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Angebot] Fehler bei Umwandlung: " + e);
            FeedbackPopup.Show(Root, "Fehler bei Umwandlung", FeedbackTyp.Fehler);
        }
    }
}