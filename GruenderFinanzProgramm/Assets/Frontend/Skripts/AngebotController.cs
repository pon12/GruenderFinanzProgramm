using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

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
            Debug.LogWarning("[Angebot] Umwandeln-Button nicht gefunden.");
            return;
        }

        umwandeln.RegisterCallback<ClickEvent>(_ => WandleLetztesAngebotInRechnungUm());
    }

    private void WandleLetztesAngebotInRechnungUm()
    {
        try
        {
            if (!VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten()))
                return;

            if (!PflichtfelderGefuellt())
                return;

            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                FeedbackPopup.Show(Root, "Keine Datenbank gefunden", FeedbackTyp.Fehler);
                return;
            }

            _statusDropdown?.SetValueWithoutNotify("Angenommen");

            float netto = ParseBetrag(_nettoLabel != null ? _nettoLabel.text : "0");

            float rabattWert = ParseBetrag(_rabattWertFeld != null ? _rabattWertFeld.value : "0");
            string rabattTyp = _rabattTypDropdown != null ? _rabattTypDropdown.value : "Kein Rabatt";

            float rabatt = 0f;

            if (rabattTyp == "Prozent")
                rabatt = netto * rabattWert / 100f;
            else if (rabattTyp == "Festbetrag")
                rabatt = rabattWert;

            float mwstSatz = HoleMwstSatz();
            float steuerBasis = netto - rabatt;
            float steuer = steuerBasis * mwstSatz;
            float zwischenbetrag = steuerBasis + steuer;

            float skontoProzent = ParseBetrag(_skontoWertFeld != null ? _skontoWertFeld.value : "0");
            float skonto = zwischenbetrag * skontoProzent / 100f;

            float finalTotal = zwischenbetrag - skonto;

            Offer offer = new Offer
            {
                customerId = _ausgewaehlterKundeId,
                customerName = _ausgewaehlterKunde,
                customerAddress = _ausgewaehlterKundeAdresse,
                companyName = HoleCompanyName(db),
                companyAddress = HoleCompanyAddress(db),
                offerNumber = _nummerFeld != null ? _nummerFeld.value : "",
                date = _datumFeld != null ? _datumFeld.value : System.DateTime.Now.ToString("dd.MM.yyyy"),
                validUntil = _fristFeld != null ? _fristFeld.value : "",
                status = "Angenommen",
                subtotal = netto,
                discount = rabatt,
                extraCosts = skonto,
                tax = steuer,
                total = finalTotal,
                notes = _notizenFeld != null ? _notizenFeld.value : "",
                bookedToCashbook = false,
                cashbookEntryId = 0,
                bookingDate = ""
            };

            int offerId = db.createOffer(offer);

            BelegTransferData.Clear();

            BelegTransferData.hasTransfer = true;
            BelegTransferData.customerId = _ausgewaehlterKundeId;
            BelegTransferData.customerName = _ausgewaehlterKunde;
            BelegTransferData.customerAddress = _ausgewaehlterKundeAdresse;
            BelegTransferData.date = _datumFeld != null ? _datumFeld.value : "";
            BelegTransferData.dueDate = _fristFeld != null ? _fristFeld.value : "";
            BelegTransferData.notes = _notizenFeld != null ? _notizenFeld.value : "";
            BelegTransferData.rabatt = rabatt;
            BelegTransferData.skonto = skonto;

            foreach (var zeile in _zeilen.ToList())
            {
                OfferItem offerItem = new OfferItem
                {
                    offerId = offerId,
                    articleNumber = zeile.Artikel != null ? zeile.Artikel.text : "",
                    description = zeile.Beschreibung != null ? zeile.Beschreibung.text : "",
                    quantity = BegrenzeMenge(Mathf.RoundToInt(ParseBetrag(zeile.Menge != null ? zeile.Menge.value : "0"))),
                    unitPrice = BegrenzePreis(ParseBetrag(zeile.Preis != null ? zeile.Preis.text : "0"))
                };

                db.createOfferItem(offerItem);

                BelegTransferData.items.Add(new TransferItem
                {
                    articleNumber = offerItem.articleNumber,
                    description = offerItem.description,
                    quantity = offerItem.quantity,
                    unitPrice = offerItem.unitPrice
                });
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Rechnung");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Angebot] Fehler bei Umwandlung: " + e);
            FeedbackPopup.Show(Root, "Fehler bei Umwandlung", FeedbackTyp.Fehler);
        }
    }
}