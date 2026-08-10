using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RechnungController : BelegScreenController
{
    protected override string BelegTyp      => "Rechnung";
    protected override string NummernPrefix =>
        PlayerPrefs.GetString("settings_rechnr_praefix", "RG-NR");

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
            var db         = UserDatabaseAccess.getCurrentUserDatabase();
            var rechnungen = db.getAllInvoices();
            int naechste   = (rechnungen != null ? rechnungen.Count : 0) + 1;
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
        if (btnBezahlt != null)
            btnBezahlt.RegisterCallback<ClickEvent>(_ => BezahltGeklickt());
        else
            Debug.LogWarning("[Rechnung] Bezahlt-Button nicht gefunden.");
    }

    private void VersendetGeklickt()
    {
        if (!VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten())) return;
        if (!PflichtfelderGefuellt()) return;

        var statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        statusDropdown?.SetValueWithoutNotify("Versendet");

        FeedbackPopup.Show(Root, "Rechnung versendet", FeedbackTyp.Erfolg);
    }

    private void StorniertGeklickt()
    {
        var statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        statusDropdown?.SetValueWithoutNotify("Abgelehnt");

        FeedbackPopup.Show(Root, "Rechnung storniert", FeedbackTyp.Fehler);
    }

    private void BezahltGeklickt()
    {
        if (!VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten())) return;
        if (!PflichtfelderGefuellt()) return;

        var statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        statusDropdown?.SetValueWithoutNotify("Bezahlt");

        UebernimmInsKassenbuch();
        FeedbackPopup.Show(Root, "Rechnung als bezahlt markiert", FeedbackTyp.Erfolg);
    }
}