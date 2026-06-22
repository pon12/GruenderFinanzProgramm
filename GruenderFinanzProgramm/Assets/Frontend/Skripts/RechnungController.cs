using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RechnungController : BelegScreenController
{
    protected override string BelegTyp      => "Rechnung";
    protected override string NummernPrefix => "RG-NR";

    protected override List<string> StatusOptionen => new List<string>
    {
        "Entwurf",
        "Versendet",
        "Angenommen",
        "Bezahlt",
        "Abgelehnt"
    };

    // Zählt Rechnungen statt Angebote für die Nummerierung
    protected override string ErzeugeNaechsteNummer()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var rechnungen = db.getAllInvoices();
            int naechste = (rechnungen != null ? rechnungen.Count : 0) + 1;
            return string.Format("{0}-{1:D4}", NummernPrefix, naechste);
        }
        catch
        {
            return string.Format("{0}-{1:D4}", NummernPrefix, 1);
        }
    }

    protected override void RegistriereZusatzButtons()
    {
        var btnBezahlt = Root.Q<Button>("btn-bezahlt");
        if (btnBezahlt == null)
        {
            Debug.LogWarning("[Rechnung] Bezahlt-Button nicht gefunden.");
            return;
        }

        btnBezahlt.RegisterCallback<ClickEvent>(_ => BezahltGeklickt());
    }

    private void BezahltGeklickt()
    {
        var statusDropdown = Root.Q<DropdownField>("dropdown-status");
        if (statusDropdown != null)
            statusDropdown.SetValueWithoutNotify("Bezahlt");

        FeedbackPopup.Show(Root, "Rechnung als bezahlt markiert", FeedbackTyp.Erfolg);
    }
}