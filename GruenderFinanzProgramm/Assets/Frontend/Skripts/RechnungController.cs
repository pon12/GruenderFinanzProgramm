using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RechnungController : BelegScreenController
{
    protected override string BelegTyp      => "Rechnung";
    protected override string NummernPrefix => "RG-NR";

    // Verhindert doppelte Kassenbucheinträge innerhalb einer Session
    private bool _insKassenbuchGebucht = false;

    protected override List<string> StatusOptionen => new List<string>
    {
        "Versendet",
        "Bezahlt",
        "Storniert"
    };

    protected override void OnEnable()
    {
        _insKassenbuchGebucht = false;
        base.OnEnable();
    }

    // Zählt Rechnungen für die Nummerierung
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

    // Wird vom Speichern-Button in der Basisklasse nach erfolgreichem Speichern aufgerufen.
    // Trägt den Betrag ins Kassenbuch ein, falls Status "Bezahlt" und noch nicht gebucht.
    protected override void NachSpeichernHook()
    {
        var statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        if (statusDropdown?.value == "Bezahlt" && !_insKassenbuchGebucht)
        {
            UebernimmInsKassenbuch();
            _insKassenbuchGebucht = true;
        }
    }

    protected override void AktualisiereStatusButtons(string status)
    {
        base.AktualisiereStatusButtons(status);
        SetzeButtonAktiv(Root.Q<Button>("btn-bezahlt"), status != "Bezahlt" && status != "Storniert" && status != "Abgelehnt");
    }

    private void BezahltGeklickt()
    {
        if (!VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten())) return;
        if (!PflichtfelderGefuellt()) return;

        var statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        statusDropdown?.SetValueWithoutNotify("Bezahlt");

        if (!_insKassenbuchGebucht)
        {
            UebernimmInsKassenbuch();
            _insKassenbuchGebucht = true;
        }

        FeedbackPopup.Show(Root, "Rechnung als bezahlt markiert", FeedbackTyp.Erfolg);
    }
}