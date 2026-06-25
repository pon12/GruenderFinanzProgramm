using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

public abstract class BelegScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    protected const string PositionenListeName = "positionen-liste";
    protected const string KundensucheName = "input-kundensuche";
    protected const string AbsenderLabelName = "label-absender";
    protected const string NummerFeldName = "input-nummer";
    protected const string StatusDropdownName = "dropdown-status";
    protected const string DatumFeldName = "input-datum";
    protected const string FristFeldName = "input-frist";
    protected const string ReferenzFeldName = "input-referenz";
    protected const string RabattTypDropdownName = "dropdown-rabatt-typ";
    protected const string RabattWertFeldName = "input-rabatt-wert";
    protected const string SkontoWertFeldName = "input-skonto-wert";
    protected const string NotizenFeldName = "input-notizen";
    protected const string NettoLabelName = "label-netto";
    protected const string RabattLabelName = "label-rabatt";
    protected const string SkontoLabelName = "label-skonto";
    protected const string GesamtLabelName = "label-gesamt-total";
    protected const string SpeichernButtonName = "btn-speichern";
    protected const string AngenommenButtonName = "btn-angenommen";
    protected const string AbgelehntButtonName = "btn-abgelehnt";
    protected const string UmwandelnButtonName = "btn-umwandeln";
    protected const string PositionAddButtonName = "btn-position-hinzufuegen";
    protected const string AnhangKarteName = "card-anhaenge";

    protected const int ReferenzMaxLaenge = 10;
    protected const int NotizenMaxLaenge = 150;

    protected static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot = new Color(230f / 255f, 57f / 255f, 70f / 255f);
    private static readonly Color FeldFarbe = new Color(70f / 255f, 70f / 255f, 70f / 255f);
    private static readonly Color KartenFarbe = new Color(38f / 255f, 38f / 255f, 38f / 255f);

    protected VisualElement Root;

    private ScrollView _positionenListe;
    protected readonly List<PositionsZeile> _zeilen = new List<PositionsZeile>();

    protected Label _nettoLabel, _rabattLabel, _skontoLabel, _gesamtLabel;
    protected TextField _kundensuche, _nummerFeld, _datumFeld, _fristFeld,
                      _rabattWertFeld, _skontoWertFeld, _notizenFeld;
    protected DropdownField _statusDropdown, _rabattTypDropdown;
    private VisualElement _suchErgebnisListe;
    protected string _ausgewaehlterKunde = "";
    protected int _ausgewaehlterKundeId = 0;
    protected string _ausgewaehlterKundeAdresse = "";
    private Button _umwandelnButton;
    private bool _buttonsRegistriert = false;
    private readonly Dictionary<string, bool> _anhangAusgewaehlt = new Dictionary<string, bool>();
    private VisualElement _anhangBereich;

    private VisualElement _dienstleistungPopup;
    private VisualElement _kalenderPopup;
    private List<Service> _dienstleistungen = new List<Service>();

    private readonly string[] _monatsNamen =
    {
        "Januar", "Februar", "M\u00e4rz", "April", "Mai", "Juni",
        "Juli", "August", "September", "Oktober", "November", "Dezember"
    };

    protected abstract string BelegTyp { get; }
    protected abstract string NummernPrefix { get; }
    protected abstract List<string> StatusOptionen { get; }
    protected virtual void RegistriereZusatzButtons() { }

    protected class PositionsZeile
    {
        public VisualElement Wurzel;
        public Label Artikel, Beschreibung, Einheit, Preis, Gesamt;
        public TextField Menge;
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        Root = uiDocument.rootVisualElement;

        SammleElemente();
        _zeilen.Clear();

        if (_positionenListe != null)
            _positionenListe.Clear();

        LadeAbsenderdaten();
        LadeDienstleistungen();
        RegistriereSummenEingaben();
        RegistriereButtons();
        RegistriereKundensuche();
        RegistriereKalenderButtons();
        RegistriereAnhaenge();
        RegistriereHelpTooltips();

        var session = BelegSessionDaten.Lade(BelegTyp);
        if (session != null)
        {
            WiederherstelleSession(session);
        }
        else
        {
            LeereDemoInhalte();
            SetzeStandardwerte();
        }

        UebernimmTransferDatenFallsVorhanden();

        BerechneSummen();
        AktualisiereUmwandelnButton();
    }


    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(Root, "btn-help-seitentitel",
            BelegTyp == "Angebot"
                ? "Hier erstellst du ein neues Angebot für einen Kunden. " +
                  "Fülle alle Bereiche aus und speichere am Ende. " +
                  "Ein Angebot kann später in eine Rechnung umgewandelt werden."
                : "Hier erstellst du eine neue Rechnung für einen Kunden. " +
                  "Fülle alle Bereiche aus und speichere am Ende. " +
                  "Bezahlte Rechnungen können als „Bezahlt“ markiert werden.");

        HelpTooltip.Registriere(Root, "btn-help-partner",
            BelegTyp == "Angebot"
                ? "Wähle den Kunden aus, für den das Angebot gilt. " +
                  "Links erscheint der Angebotsempfänger, rechts dein Unternehmen."
                : "Wähle den Kunden aus, an den die Rechnung geht. " +
                  "Links erscheint der Rechnungsempfänger, rechts dein Unternehmen.");

        HelpTooltip.Registriere(Root, "btn-help-grunddaten",
            "Vergib hier Nummer, Datum und Status. " +
            "Die Nummer wird automatisch generiert, kann aber angepasst werden. " +
            "Mit dem Kalender-Symbol öffnest du eine Datumsauswahl.");

        HelpTooltip.Registriere(Root, "btn-help-anhaenge",
            "Wähle optionale Anhänge aus, die dem PDF hinzugefügt werden. " +
            "Verfügbare Anhänge (z. B. AGB, Disclaimer, Zahlungsbedingungen) " +
            "werden automatisch aus den hinterlegten Dokumenten geladen.");

        HelpTooltip.Registriere(Root, "btn-help-positionen",
            "Füge Dienstleistungen als Positionen hinzu (+ Button). " +
            "Jede Position enthält Artikel, Beschreibung, Menge, Einheit und Preis. " +
            "Netto, Rabatt, Skonto und MwSt. werden automatisch berechnet.");

        HelpTooltip.Registriere(Root, "btn-help-details",
            "Lege Rabatt und Skonto fest. " +
            "Im Notizfeld kannst du interne Hinweise hinterlegen (max. 150 Zeichen).");
    }
    private void OnDisable()
    {
        SpeichereSession();
        _buttonsRegistriert = false;
    }

    private void SammleElemente()
    {
        _positionenListe = Root.Q<ScrollView>(PositionenListeName) ?? Root.Q<ScrollView>();
        if (_positionenListe != null)
            _positionenListe.verticalScrollerVisibility = ScrollerVisibility.Auto;

        _nettoLabel = Root.Q<Label>(NettoLabelName);
        _rabattLabel = Root.Q<Label>(RabattLabelName);
        _skontoLabel = Root.Q<Label>(SkontoLabelName);
        _gesamtLabel = Root.Q<Label>(GesamtLabelName);

        _kundensuche = Root.Q<TextField>(KundensucheName);
        _nummerFeld = Root.Q<TextField>(NummerFeldName);
        _datumFeld = Root.Q<TextField>(DatumFeldName);
        _fristFeld = Root.Q<TextField>(FristFeldName);
        _notizenFeld = Root.Q<TextField>(NotizenFeldName);

        _statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        _rabattTypDropdown = Root.Q<DropdownField>(RabattTypDropdownName);

        _umwandelnButton = Root.Q<Button>(UmwandelnButtonName);
    }

    private void LeereDemoInhalte()
    {
        _zeilen.Clear();
        _positionenListe?.Clear();
        Root.Query<TextField>().ForEach(f => f.SetValueWithoutNotify(""));

        var boxen = Root.Query(className: "angebot-address-box").ToList();
        NormalisiereAdressBoxen(boxen);
        if (boxen.Count > 0) SetzeAdressePlatzhalter(boxen[0], HoleEmpfaengerBezeichnung());
        if (boxen.Count > 1) SetzeAdressePlatzhalter(boxen[1], HoleSenderBezeichnung());
    }

    private static void NormalisiereAdressBoxen(System.Collections.Generic.List<VisualElement> boxen)
    {
        foreach (var box in boxen)
        {
            box.style.flexGrow = 1f;
            box.style.alignSelf = Align.Stretch;
            box.style.flexShrink = 0f;
        }
    }

    private void SetzeAdressePlatzhalter(VisualElement box, string ueberschrift)
    {
        box.Clear();
        AdressTitel(box, ueberschrift);

        var platzhalter = new Label("Noch nicht ausgew\u00e4hlt");
        platzhalter.style.fontSize = 12;
        platzhalter.style.color = new Color(0.59f, 0.59f, 0.59f);
        platzhalter.style.marginTop = 2;
        box.Add(platzhalter);
    }

    private void SetzeAdresseStrukturiert(
        VisualElement box, string ueberschrift,
        (string bezeichnung, string wert)[] felder)
    {
        box.Clear();
        AdressTitel(box, ueberschrift);

        foreach (var (bezeichnung, wert) in felder)
        {
            if (!string.IsNullOrWhiteSpace(wert))
                AdressZeile(box, bezeichnung, wert);
        }
    }

    private static void AdressTitel(VisualElement box, string text)
    {
        var label = new Label(text);
        label.style.fontSize = 10;
        label.style.color = new Color(0.5f, 0.5f, 0.5f);
        label.style.unityFontStyleAndWeight = FontStyle.Normal;
        label.style.marginBottom = 6;
        label.style.letterSpacing = 0.5f;
        box.Add(label);
    }

    private static void AdressZeile(VisualElement box, string bezeichnung, string wert)
    {
        var zeile = new VisualElement();
        zeile.style.flexDirection = FlexDirection.Row;
        zeile.style.marginBottom = 3;
        zeile.style.alignItems = Align.FlexStart;

        if (!string.IsNullOrEmpty(bezeichnung))
        {
            var key = new Label(bezeichnung);
            key.style.fontSize = 10;
            key.style.color = new Color(0.5f, 0.5f, 0.5f);
            key.style.minWidth = 52;
            key.style.marginRight = 4;
            key.style.marginTop = 1;
            key.style.whiteSpace = WhiteSpace.NoWrap;
            zeile.Add(key);
        }

        var val = new Label(wert);
        val.style.fontSize = 12;
        val.style.color = new Color(0.88f, 0.88f, 0.88f);
        val.style.whiteSpace = WhiteSpace.Normal;
        val.style.flexShrink = 1;
        zeile.Add(val);

        box.Add(zeile);
    }

    private void SetzeAdresse(VisualElement box, string ueberschrift, string inhalt)
    {
        box.Clear();

        var titel = new Label(ueberschrift);
        titel.style.fontSize = 13;
        titel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titel.style.color = Color.white;
        titel.style.marginBottom = 6;
        box.Add(titel);

        var text = new Label(inhalt);
        text.style.fontSize = 12;
        text.style.color = string.IsNullOrEmpty(inhalt) || inhalt == "Kunde ausw\u00e4hlen"
            ? new Color(0.59f, 0.59f, 0.59f)
            : new Color(0.78f, 0.78f, 0.78f);
        text.style.whiteSpace = WhiteSpace.Normal;
        box.Add(text);
    }

    private string HoleEmpfaengerBezeichnung()
    {
        return BelegTyp == "Angebot"
            ? "Angebotsempfänger"
            : "Rechnungsempfänger";
    }

    private string HoleSenderBezeichnung()
    {
        return BelegTyp == "Angebot"
            ? "Angebotssteller"
            : "Rechnungssteller";
    }

    private void SetzeStandardwerte()
    {
        _nummerFeld?.SetValueWithoutNotify(ErzeugeNaechsteNummer());
        _datumFeld?.SetValueWithoutNotify(DateTime.Now.ToString("dd.MM.yyyy"));
        _fristFeld?.SetValueWithoutNotify(DateTime.Now.AddDays(14).ToString("dd.MM.yyyy"));

        NurDatumzeichen(_datumFeld);
        NurDatumzeichen(_fristFeld);

        if (_statusDropdown != null)
        {
            // Verzögert setzen damit das Layout vollständig gebaut ist
            // bevor die Dropdown-Optionen zugewiesen werden
            _statusDropdown.schedule.Execute(() =>
            {
                _statusDropdown.choices = new List<string>(StatusOptionen);
                _statusDropdown.SetValueWithoutNotify(StatusOptionen[0]);
                _statusDropdown.RegisterValueChangedCallback(_ => AktualisiereUmwandelnButton());
            }).ExecuteLater(50);
        }

        if (_rabattTypDropdown != null)
        {
            _rabattTypDropdown.choices = new List<string>
                { "Kein Rabatt", "Prozent", "Festbetrag" };
            _rabattTypDropdown.SetValueWithoutNotify("Kein Rabatt");
            _rabattTypDropdown.RegisterValueChangedCallback(_ => BerechneSummen());
        }
    }

    protected void UebernimmTransferDatenFallsVorhanden()
    {
        if (BelegTyp != "Rechnung")
            return;

        if (!BelegTransferData.hasTransfer)
            return;

        _ausgewaehlterKundeId = BelegTransferData.customerId;
        _ausgewaehlterKunde = BelegTransferData.customerName;
        _ausgewaehlterKundeAdresse = BelegTransferData.customerAddress;

        _kundensuche?.SetValueWithoutNotify(_ausgewaehlterKunde);

        var boxen = Root.Query(className: "angebot-address-box").ToList();

        if (boxen.Count > 0)
            SetzeAdresse(boxen[0], HoleEmpfaengerBezeichnung(), _ausgewaehlterKundeAdresse);

        _datumFeld?.SetValueWithoutNotify(BelegTransferData.date);
        _fristFeld?.SetValueWithoutNotify(BelegTransferData.dueDate);
        _notizenFeld?.SetValueWithoutNotify(BelegTransferData.notes);

        foreach (var item in BelegTransferData.items)
        {
            Service service = new Service
            {
                name = item.articleNumber,
                description = item.description,
                priceModel = "Stück",
                price = item.unitPrice
            };

            FuegeZeileAusDienstleistungHinzu(service, item.quantity);
        }

        BerechneSummen();

        BelegTransferData.Clear();
    }



    // Erzeugt eine fortlaufende Nummer im Format PREFIX-0001
    // Kann in Unterklassen überschrieben werden um eine andere Zählliste zu verwenden
    protected virtual string ErzeugeNaechsteNummer()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var eintraege = db.getAllOffers();
            int naechste = (eintraege != null ? eintraege.Count : 0) + 1;
            return string.Format("{0}-{1:D4}", NummernPrefix, naechste);
        }
        catch
        {
            return string.Format("{0}-{1:D4}", NummernPrefix, 1);
        }
    }

    // Setzt den Umwandeln-Button aktiv nur wenn Status "Angenommen" ist
    private void AktualisiereUmwandelnButton()
    {
        if (_umwandelnButton == null) return;

        bool angenommen = _statusDropdown != null
            && _statusDropdown.value == "Angenommen";

        _umwandelnButton.SetEnabled(angenommen);
        _umwandelnButton.style.backgroundColor = angenommen
            ? Gruen
            : new Color(70f / 255f, 70f / 255f, 70f / 255f);
        _umwandelnButton.style.color = angenommen
            ? new Color(30f / 255f, 30f / 255f, 30f / 255f)
            : new Color(180f / 255f, 180f / 255f, 180f / 255f);
    }

    private void LadeAbsenderdaten()
    {
        var boxen = Root.Query(className: "angebot-address-box").ToList();
        NormalisiereAdressBoxen(boxen);
        var senderBox = boxen.Count > 1 ? boxen[1] : null;

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var firmen = db.getAllCompanies();
            if (firmen != null && firmen.Count > 0)
            {
                Company f = firmen[firmen.Count - 1];

                string name = LiesFeld(f, "name");
                string strasse = LiesFeld(f, "strasseuHausNr", "strasseuHausNR", "strasse", "adresse", "address");
                string plz = LiesFeld(f, "plz");
                string ort = LiesFeld(f, "location", "ort", "stadt");
                string plzOrt = (plz + " " + ort).Trim();

                string[] brancheOptionen =
                {
                "IT & Software", "Handel", "Handwerk", "Beratung", "Marketing & Medien",
                "Finanzen & Versicherung", "Gastronomie", "Gesundheit & Pflege",
                "Bildung", "Immobilien", "Logistik", "Sonstiges"
            };

                string brancheIndex = LiesFeld(f, "industry");
                int brancheIdx = 0;
                int.TryParse(brancheIndex, out brancheIdx);

                string branche = brancheIdx >= 0 && brancheIdx < brancheOptionen.Length
                    ? brancheOptionen[brancheIdx]
                    : "";

                var felder = new (string, string)[]
                {
                ("Name", name),
                ("Straße", strasse),
                ("Ort", plzOrt),
                ("Branche", branche),
                };

                if (senderBox != null)
                    SetzeAdresseStrukturiert(senderBox, HoleSenderBezeichnung(), felder);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Absenderdaten: " + e.Message);
        }
    }

    private void LadeDienstleistungen()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();

            Debug.Log("[Beleg] DB: " + db.getDatabasePath());

            _dienstleistungen = db.getAllServices() ?? new List<Service>();

            Debug.Log("[Beleg] Dienstleistungen geladen: " + _dienstleistungen.Count);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Dienstleistungen: " + e.Message);
            _dienstleistungen = new List<Service>();
        }
    }

    private void RegistriereSummenEingaben()
    {
        _rabattWertFeld = Root.Q<TextField>(RabattWertFeldName);
        _skontoWertFeld = Root.Q<TextField>(SkontoWertFeldName);

        _rabattWertFeld?.SetValueWithoutNotify("0");
        _skontoWertFeld?.SetValueWithoutNotify("0");

        NurZahlenmitKomma(_rabattWertFeld);
        NurZahlenmitKomma(_skontoWertFeld);

        _rabattWertFeld?.RegisterValueChangedCallback(_ => BerechneSummen());
        _skontoWertFeld?.RegisterValueChangedCallback(_ => BerechneSummen());
    }

    private void RegistriereButtons()
    {
        if (_buttonsRegistriert)
            return;
        _buttonsRegistriert = true;
        FindeButton(SpeichernButtonName, "Speichern")?
            .RegisterCallback<ClickEvent>(_ => SpeichernGeklickt());
        FindeButton(AngenommenButtonName, "Angenommen")?
            .RegisterCallback<ClickEvent>(_ => StatusGeklickt(true));
        FindeButton(AbgelehntButtonName, "Abgelehnt")?
            .RegisterCallback<ClickEvent>(_ => StatusGeklickt(false));
        FindeButton("btn-export", "Als PDF")?
            .RegisterCallback<ClickEvent>(_ => ExportierePDF());
        FindeButton(PositionAddButtonName, "+")?
            .RegisterCallback<ClickEvent>(_ => OeffneDienstleistungsPopup());
        RegistriereZusatzButtons();
    }

    protected Button FindeButton(string elementName, string buttonText)
    {
        var button = Root.Q<Button>(elementName);
        if (button != null) return button;
        return Root.Query<Button>().ToList()
                   .FirstOrDefault(b => b.text != null && b.text.Trim().StartsWith(buttonText));
    }

    // ============================================================
    // KALENDER-POPUP
    // ============================================================

    // Kalender-Buttons sind in der UXML bereits als btn-kalender-datum / btn-kalender-frist definiert
    private void RegistriereKalenderButtons()
    {
        var btnDatum = Root.Q<Button>("btn-kalender-datum");
        var btnFrist = Root.Q<Button>("btn-kalender-frist");

        if (btnDatum != null && _datumFeld != null)
        {
            var lokalDatum = _datumFeld;
            btnDatum.RegisterCallback<ClickEvent>(_ => OeffneKalenderPopup(lokalDatum));
            btnDatum.RegisterCallback<MouseEnterEvent>(_ =>
                btnDatum.style.backgroundColor = new Color(90f / 255f, 90f / 255f, 90f / 255f));
            btnDatum.RegisterCallback<MouseLeaveEvent>(_ =>
                btnDatum.style.backgroundColor = FeldFarbe);
        }

        if (btnFrist != null && _fristFeld != null)
        {
            var lokalFrist = _fristFeld;
            btnFrist.RegisterCallback<ClickEvent>(_ => OeffneKalenderPopup(lokalFrist));
            btnFrist.RegisterCallback<MouseEnterEvent>(_ =>
                btnFrist.style.backgroundColor = new Color(90f / 255f, 90f / 255f, 90f / 255f));
            btnFrist.RegisterCallback<MouseLeaveEvent>(_ =>
                btnFrist.style.backgroundColor = FeldFarbe);
        }
    }

    private void OeffneKalenderPopup(TextField zielFeld)
    {
        if (_kalenderPopup != null)
        {
            _kalenderPopup.RemoveFromHierarchy();
            _kalenderPopup = null;
        }

        // Startmonat: Feldwert lesen, sonst Fallback
        DateTime start = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(zielFeld.value))
        {
            if (!DateTime.TryParseExact(zielFeld.value, "dd.MM.yyyy",
                De, DateTimeStyles.None, out start) || start.Year < 2000)
                start = DateTime.Today;
        }

        // Ist dieses Feld das Frist-Feld, soll der Startmonat mindestens
        // beim Datum-Feld liegen (und nie vor heute)
        if (zielFeld == _fristFeld && _datumFeld != null
            && !string.IsNullOrWhiteSpace(_datumFeld.value))
        {
            if (DateTime.TryParseExact(_datumFeld.value, "dd.MM.yyyy",
                De, DateTimeStyles.None, out DateTime datumStart)
                && datumStart.Year >= 2000
                && datumStart > start)
            {
                start = datumStart;
            }
        }

        if (start < DateTime.Today) start = DateTime.Today;

        int jahr = start.Year;
        int monat = start.Month;

        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0;
        overlay.style.top = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var karte = new VisualElement();
        karte.style.width = 322;
        karte.style.backgroundColor = KartenFarbe;
        karte.style.borderTopLeftRadius = 12; karte.style.borderTopRightRadius = 12;
        karte.style.borderBottomLeftRadius = 12; karte.style.borderBottomRightRadius = 12;
        karte.style.borderTopWidth = 2; karte.style.borderRightWidth = 2;
        karte.style.borderBottomWidth = 2; karte.style.borderLeftWidth = 2;
        karte.style.borderTopColor = Gruen; karte.style.borderRightColor = Gruen;
        karte.style.borderBottomColor = Gruen; karte.style.borderLeftColor = Gruen;
        karte.style.paddingTop = 18; karte.style.paddingBottom = 18;
        karte.style.paddingLeft = 18; karte.style.paddingRight = 18;

        // Kopfzeile: Pfeil | Monatsname | Pfeil
        var kopf = new VisualElement();
        kopf.style.flexDirection = FlexDirection.Row;
        kopf.style.justifyContent = Justify.SpaceBetween;
        kopf.style.alignItems = Align.Center;
        kopf.style.marginBottom = 12;

        var btnVorig = new Button { text = "\u2039" };
        StileNavButton(btnVorig);

        var monatLabel = new Label();
        monatLabel.style.color = Color.white;
        monatLabel.style.fontSize = 14;
        monatLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        monatLabel.style.flexGrow = 1;
        monatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        var btnNaechst = new Button { text = "\u203a" };
        StileNavButton(btnNaechst);

        kopf.Add(btnVorig);
        kopf.Add(monatLabel);
        kopf.Add(btnNaechst);
        karte.Add(kopf);

        // Zellgröße: 7 Spalten gleichmäßig in (322 - 36px Padding) = 286px → 40px pro Zelle
        const int zellBreite = 36;
        const int zellHoehe = 36;
        const int zellMargin = 2;

        var wochenHeader = new VisualElement();
        wochenHeader.style.flexDirection = FlexDirection.Row;
        wochenHeader.style.marginBottom = 4;
        string[] wochentage = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
        foreach (var wt in wochentage)
        {
            var wl = new Label(wt);
            wl.style.width = zellBreite + zellMargin * 2;
            wl.style.height = 24;
            wl.style.fontSize = 11;
            wl.style.color = new Color(0.6f, 0.6f, 0.6f);
            wl.style.unityTextAlign = TextAnchor.MiddleCenter;
            wochenHeader.Add(wl);
        }
        karte.Add(wochenHeader);

        // Container für Zeilen
        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Column;
        karte.Add(grid);

        void BaueTagButton(VisualElement zeile, int tag, bool istImMonat)
        {
            var btn = new Button();
            btn.style.width = zellBreite;
            btn.style.height = zellHoehe;
            btn.style.marginTop = zellMargin; btn.style.marginBottom = zellMargin;
            btn.style.marginLeft = zellMargin; btn.style.marginRight = zellMargin;
            btn.style.borderTopWidth = 0; btn.style.borderRightWidth = 0;
            btn.style.borderBottomWidth = 0; btn.style.borderLeftWidth = 0;
            btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
            btn.style.fontSize = 12;
            btn.style.paddingTop = 0; btn.style.paddingBottom = 0;
            btn.style.paddingLeft = 0; btn.style.paddingRight = 0;

            if (!istImMonat)
            {
                btn.text = "";
                btn.style.backgroundColor = Color.clear;
                btn.SetEnabled(false);
            }
            else
            {
                btn.text = tag.ToString();
                var heute = DateTime.Today;
                bool istHeute = tag == heute.Day && monat == heute.Month && jahr == heute.Year;

                btn.style.backgroundColor = istHeute
                    ? new Color(Gruen.r, Gruen.g, Gruen.b, 0.25f) : Color.clear;
                btn.style.color = istHeute ? Gruen : Color.white;

                if (istHeute)
                {
                    btn.style.borderTopWidth = 1; btn.style.borderRightWidth = 1;
                    btn.style.borderBottomWidth = 1; btn.style.borderLeftWidth = 1;
                    btn.style.borderTopColor = Gruen; btn.style.borderRightColor = Gruen;
                    btn.style.borderBottomColor = Gruen; btn.style.borderLeftColor = Gruen;
                }

                int lokalTag = tag, lokalMonat = monat, lokalJahr = jahr;
                btn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (!istHeute)
                        btn.style.backgroundColor = new Color(Gruen.r, Gruen.g, Gruen.b, 0.15f);
                });
                btn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (!istHeute) btn.style.backgroundColor = Color.clear;
                });
                btn.RegisterCallback<ClickEvent>(_ =>
                {
                    zielFeld.SetValueWithoutNotify(
                        new DateTime(lokalJahr, lokalMonat, lokalTag).ToString("dd.MM.yyyy"));
                    SchliessKalender();
                });
            }
            zeile.Add(btn);
        }

        void RenderMonat()
        {
            grid.Clear();
            monatLabel.text = _monatsNamen[monat - 1] + " " + jahr;

            var ersterTag = new DateTime(jahr, monat, 1);
            int tageImMonat = DateTime.DaysInMonth(jahr, monat);
            // Montag-basierter Start: Mo=0 … So=6
            int startSlot = ((int)ersterTag.DayOfWeek + 6) % 7;

            int benotigteSlots = startSlot + tageImMonat;
            int zeilenAnzahl = (int)Math.Ceiling(benotigteSlots / 7.0);

            int tagZaehler = 1;

            for (int z = 0; z < zeilenAnzahl; z++)
            {
                var zeile = new VisualElement();
                zeile.style.flexDirection = FlexDirection.Row;
                grid.Add(zeile);

                for (int s = 0; s < 7; s++)
                {
                    int slot = z * 7 + s;
                    bool istImMonat = slot >= startSlot && tagZaehler <= tageImMonat;
                    int tag = istImMonat ? tagZaehler : 0;
                    if (istImMonat) tagZaehler++;

                    BaueTagButton(zeile, tag, istImMonat);
                }
            }
        }

        btnVorig.RegisterCallback<ClickEvent>(_ =>
        {
            monat--;
            if (monat < 1) { monat = 12; jahr--; }
            RenderMonat();
        });
        btnNaechst.RegisterCallback<ClickEvent>(_ =>
        {
            monat++;
            if (monat > 12) { monat = 1; jahr++; }
            RenderMonat();
        });

        RenderMonat();

        karte.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        overlay.RegisterCallback<ClickEvent>(_ => SchliessKalender());

        overlay.Add(karte);
        Root.Add(overlay);
        _kalenderPopup = overlay;
    }

    private void SchliessKalender()
    {
        if (_kalenderPopup == null) return;
        _kalenderPopup.RemoveFromHierarchy();
        _kalenderPopup = null;
    }

    private static void StileNavButton(Button btn)
    {
        btn.style.width = 28;
        btn.style.height = 28;
        btn.style.backgroundColor = FeldFarbe;
        btn.style.color = Color.white;
        btn.style.fontSize = 16;
        btn.style.borderTopWidth = 0; btn.style.borderRightWidth = 0;
        btn.style.borderBottomWidth = 0; btn.style.borderLeftWidth = 0;
        btn.style.borderTopLeftRadius = 6; btn.style.borderTopRightRadius = 6;
        btn.style.borderBottomLeftRadius = 6; btn.style.borderBottomRightRadius = 6;
        btn.style.paddingTop = 0; btn.style.paddingBottom = 0;
        btn.style.paddingLeft = 0; btn.style.paddingRight = 0;
    }

    // ============================================================
    // PFLICHTFELD-PRÜFUNG
    // ============================================================

    // Prüft ob Pflichtfelder (Kunde + Unternehmensdaten) gefüllt sind
    protected bool PflichtfelderGefuellt()
    {
        if (string.IsNullOrWhiteSpace(_ausgewaehlterKunde))
        {
            FeedbackPopup.Show(Root, "Bitte einen Kunden auswählen.", FeedbackTyp.Fehler);
            return false;
        }

        if (_zeilen == null || _zeilen.Count == 0)
        {
            FeedbackPopup.Show(Root, "Bitte mindestens eine Dienstleistung hinzufügen.", FeedbackTyp.Fehler);
            return false;
        }

        bool hatGueltigePosition = _zeilen.Exists(z =>
            z != null &&
            !string.IsNullOrWhiteSpace(z.Artikel?.text) &&
            ParseBetrag(z.Menge?.value ?? "0") > 0
        );

        if (!hatGueltigePosition)
        {
            FeedbackPopup.Show(Root, "Bitte mindestens eine Dienstleistung auswählen.", FeedbackTyp.Fehler);
            return false;
        }

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                FeedbackPopup.Show(Root, "Keine Datenbank gefunden.", FeedbackTyp.Fehler);
                return false;
            }

            var firmen = db.getAllCompanies();

            if (firmen == null || firmen.Count == 0)
            {
                FeedbackPopup.Show(Root, "Bitte Unternehmensdaten in den Einstellungen hinterlegen.", FeedbackTyp.Fehler);
                return false;
            }

            Company firma = firmen[firmen.Count - 1];

            string firmenname = LiesFeld(firma, "name");
            string adresse = LiesFeld(firma, "location", "ort", "adresse", "address");

            if (string.IsNullOrWhiteSpace(firmenname) || string.IsNullOrWhiteSpace(adresse))
            {
                FeedbackPopup.Show(Root, "Bitte vollständige Unternehmensdaten in den Einstellungen hinterlegen.", FeedbackTyp.Fehler);
                return false;
            }
        }
        catch
        {
            FeedbackPopup.Show(Root, "Unternehmensdaten konnten nicht geprüft werden.", FeedbackTyp.Fehler);
            return false;
        }

        return true;
    }

    // ============================================================
    // DIENSTLEISTUNGS-POPUP
    // ============================================================

    private void OeffneDienstleistungsPopup()
    {
        if (_dienstleistungPopup != null)
        {
            _dienstleistungPopup.RemoveFromHierarchy();
            _dienstleistungPopup = null;
        }

        LadeDienstleistungen();

        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0;
        overlay.style.top = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var karte = new VisualElement();
        karte.style.width = 500;
        karte.style.backgroundColor = KartenFarbe;
        karte.style.borderTopLeftRadius = 12; karte.style.borderTopRightRadius = 12;
        karte.style.borderBottomLeftRadius = 12; karte.style.borderBottomRightRadius = 12;
        karte.style.borderTopWidth = 2; karte.style.borderRightWidth = 2;
        karte.style.borderBottomWidth = 2; karte.style.borderLeftWidth = 2;
        karte.style.borderTopColor = Gruen; karte.style.borderRightColor = Gruen;
        karte.style.borderBottomColor = Gruen; karte.style.borderLeftColor = Gruen;
        karte.style.paddingTop = 24; karte.style.paddingBottom = 24;
        karte.style.paddingLeft = 24; karte.style.paddingRight = 24;

        var titelZeile = new VisualElement();
        titelZeile.style.flexDirection = FlexDirection.Row;
        titelZeile.style.justifyContent = Justify.SpaceBetween;
        titelZeile.style.alignItems = Align.Center;
        titelZeile.style.marginBottom = 16;

        var titel = new Label("Dienstleistung ausw\u00e4hlen");
        titel.style.fontSize = 16;
        titel.style.color = Color.white;
        titel.style.unityFontStyleAndWeight = FontStyle.Bold;

        var schliessen = new Button(() =>
        {
            overlay.RemoveFromHierarchy();
            _dienstleistungPopup = null;
        })
        { text = "\u2715" };
        schliessen.style.backgroundColor = Color.clear;
        schliessen.style.color = new Color(0.7f, 0.7f, 0.7f);
        schliessen.style.fontSize = 14;
        schliessen.style.borderTopWidth = 0; schliessen.style.borderRightWidth = 0;
        schliessen.style.borderBottomWidth = 0; schliessen.style.borderLeftWidth = 0;

        titelZeile.Add(titel);
        titelZeile.Add(schliessen);
        karte.Add(titelZeile);

        if (_dienstleistungen.Count == 0)
        {
            var hinweis = new Label(
                "Keine Dienstleistungen hinterlegt.\n" +
                "Bitte zuerst im Bereich Dienstleistungen anlegen.");
            hinweis.style.color = new Color(0.6f, 0.6f, 0.6f);
            hinweis.style.fontSize = 13;
            hinweis.style.whiteSpace = WhiteSpace.Normal;
            hinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
            hinweis.style.marginTop = 20; hinweis.style.marginBottom = 20;
            karte.Add(hinweis);
        }
        else
        {
            var dropdownLabel = new Label("Dienstleistung");
            dropdownLabel.style.fontSize = 12;
            dropdownLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            dropdownLabel.style.marginBottom = 4;
            karte.Add(dropdownLabel);

            var optionen = _dienstleistungen.Select(d =>
                d.name
                + (string.IsNullOrEmpty(d.priceModel) ? "" : "  |  " + d.priceModel)
                + "  |  " + d.price.ToString("F2", De) + " EUR").ToList();

            var dropdown = new DropdownField { choices = optionen };
            dropdown.SetValueWithoutNotify(optionen[0]);
            dropdown.style.marginBottom = 16;

            var eingabe = dropdown.Q(className: "unity-base-popup-field__input");
            if (eingabe != null)
            {
                eingabe.style.backgroundColor = FeldFarbe;
                eingabe.style.color = Color.white;
                eingabe.style.borderTopWidth = 0; eingabe.style.borderRightWidth = 0;
                eingabe.style.borderBottomWidth = 0; eingabe.style.borderLeftWidth = 0;
                eingabe.style.borderTopLeftRadius = 6; eingabe.style.borderTopRightRadius = 6;
                eingabe.style.borderBottomLeftRadius = 6; eingabe.style.borderBottomRightRadius = 6;
                eingabe.style.paddingLeft = 10;
            }
            karte.Add(dropdown);

            var mengeLabel = new Label("Menge");
            mengeLabel.style.fontSize = 12;
            mengeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            mengeLabel.style.marginBottom = 4;
            karte.Add(mengeLabel);

            var mengeFeld = NeuesTextFeld();
            mengeFeld.SetValueWithoutNotify("1");
            mengeFeld.style.marginBottom = 20;
            NurGanzeZahlen(mengeFeld);
            karte.Add(mengeFeld);

            var btnHinzufuegen = new Button(() =>
            {
                int idx = optionen.IndexOf(dropdown.value);
                if (idx < 0 || idx >= _dienstleistungen.Count) return;

                var service = _dienstleistungen[idx];
                int menge = 1;
                int.TryParse(mengeFeld.value, out menge);
                if (menge < 1) menge = 1;

                FuegeZeileAusDienstleistungHinzu(service, menge);
                overlay.RemoveFromHierarchy();
                _dienstleistungPopup = null;
            })
            { text = "Hinzuf\u00fcgen" };

            btnHinzufuegen.style.height = 40;
            btnHinzufuegen.style.backgroundColor = Gruen;
            btnHinzufuegen.style.color = new Color(0.12f, 0.12f, 0.12f);
            btnHinzufuegen.style.fontSize = 13;
            btnHinzufuegen.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnHinzufuegen.style.borderTopWidth = 0; btnHinzufuegen.style.borderRightWidth = 0;
            btnHinzufuegen.style.borderBottomWidth = 0; btnHinzufuegen.style.borderLeftWidth = 0;
            btnHinzufuegen.style.borderTopLeftRadius = 8; btnHinzufuegen.style.borderTopRightRadius = 8;
            btnHinzufuegen.style.borderBottomLeftRadius = 8; btnHinzufuegen.style.borderBottomRightRadius = 8;
            karte.Add(btnHinzufuegen);
        }

        karte.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        overlay.RegisterCallback<ClickEvent>(_ =>
        {
            overlay.RemoveFromHierarchy();
            _dienstleistungPopup = null;
        });

        overlay.Add(karte);
        Root.Add(overlay);
        _dienstleistungPopup = overlay;
    }

    // ============================================================
    // POSITIONSZEILEN
    // ============================================================

    protected void FuegeZeileAusDienstleistungHinzu(Service service, int menge)
    {
        if (_positionenListe == null) return;

        var zeile = new PositionsZeile();
        var wurzel = new VisualElement();
        wurzel.AddToClassList("angebot-position-row");
        wurzel.style.flexDirection = FlexDirection.Row;
        wurzel.style.alignItems = Align.Center;
        wurzel.style.paddingTop = 6;
        wurzel.style.paddingBottom = 6;

        var punkt = new VisualElement();
        punkt.style.width = 10; punkt.style.height = 10;
        punkt.style.borderTopLeftRadius = 5; punkt.style.borderTopRightRadius = 5;
        punkt.style.borderBottomLeftRadius = 5; punkt.style.borderBottomRightRadius = 5;
        punkt.style.backgroundColor = Gruen;
        punkt.style.flexShrink = 0; punkt.style.marginRight = 8;

        zeile.Artikel = ErstelleZeilenLabel(service.name, true);
        zeile.Artikel.style.flexGrow = 1;
        zeile.Artikel.style.minWidth = 120;

        zeile.Beschreibung = ErstelleZeilenLabel(service.description, false);
        zeile.Beschreibung.style.width = 220;
        zeile.Beschreibung.style.marginRight = 8;

        zeile.Menge = NeuesTextFeld();
        zeile.Menge.style.width = 70;
        zeile.Menge.style.marginRight = 8;
        zeile.Menge.SetValueWithoutNotify(menge.ToString());
        NurGanzeZahlen(zeile.Menge);

        zeile.Einheit = ErstelleZeilenLabel(
            string.IsNullOrEmpty(service.priceModel) ? "St\u00fcck" : service.priceModel, false);
        zeile.Einheit.style.width = 110;
        zeile.Einheit.style.marginRight = 8;

        zeile.Preis = ErstelleZeilenLabel(((float)service.price).ToString("F2", De), false);
        zeile.Preis.style.width = 110;
        zeile.Preis.style.marginRight = 8;

        float gesamt = menge * (float)service.price;
        zeile.Gesamt = new Label(FormatBetrag(gesamt));
        zeile.Gesamt.style.width = 110;
        zeile.Gesamt.style.fontSize = 13;
        zeile.Gesamt.style.color = new Color(0.86f, 0.86f, 0.86f);
        zeile.Gesamt.style.unityTextAlign = TextAnchor.MiddleRight;

        var loeschen = new Button { text = "\u2715" };
        loeschen.style.width = 26; loeschen.style.height = 26;
        loeschen.style.marginLeft = 8;
        loeschen.style.backgroundColor = Color.clear;
        loeschen.style.color = new Color(0.6f, 0.6f, 0.6f);
        loeschen.style.fontSize = 12;
        loeschen.style.borderTopWidth = 0; loeschen.style.borderRightWidth = 0;
        loeschen.style.borderBottomWidth = 0; loeschen.style.borderLeftWidth = 0;
        loeschen.RegisterCallback<MouseEnterEvent>(_ => loeschen.style.color = Rot);
        loeschen.RegisterCallback<MouseLeaveEvent>(_ =>
            loeschen.style.color = new Color(0.6f, 0.6f, 0.6f));
        loeschen.RegisterCallback<ClickEvent>(_ =>
        {
            wurzel.RemoveFromHierarchy();
            _zeilen.Remove(zeile);
            BerechneSummen();
        });

        float preisProEinheit = (float)service.price;
        zeile.Menge.RegisterValueChangedCallback(_ =>
        {
            int m = 1;
            int.TryParse(zeile.Menge.value, out m);
            if (m < 1) m = 1;
            zeile.Gesamt.text = FormatBetrag(m * preisProEinheit);
            BerechneSummen();
        });

        wurzel.Add(punkt);
        wurzel.Add(zeile.Artikel);
        wurzel.Add(zeile.Beschreibung);
        wurzel.Add(zeile.Menge);
        wurzel.Add(zeile.Einheit);
        wurzel.Add(zeile.Preis);
        wurzel.Add(zeile.Gesamt);
        wurzel.Add(loeschen);

        zeile.Wurzel = wurzel;
        _zeilen.Add(zeile);
        _positionenListe.Add(wurzel);
        _positionenListe.schedule.Execute(() => _positionenListe.ScrollTo(wurzel));
        BerechneSummen();
    }

    protected Label ErstelleZeilenLabel(string text, bool bold)
    {
        var label = new Label(text);
        label.style.fontSize = 13;
        label.style.color = new Color(0.86f, 0.86f, 0.86f);
        label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        label.style.overflow = Overflow.Hidden;
        label.style.whiteSpace = WhiteSpace.NoWrap;
        return label;
    }

    // ============================================================
    // SUMMEN-BERECHNUNG  –  Rabatt und Skonto getrennt
    // ============================================================

    protected void BerechneSummen()
    {
        float netto = 0f;
        foreach (var zeile in _zeilen)
        {
            int menge = 1;
            float preis = 0f;
            int.TryParse(zeile.Menge.value, out menge);
            if (menge < 1) menge = 1;
            preis = ParseBetrag(zeile.Preis.text);
            float gz = menge * preis;
            zeile.Gesamt.text = FormatBetrag(gz);
            netto += gz;
        }

        // Rabatt: Prozent oder Festbetrag
        float rabattWert = ParseBetrag(_rabattWertFeld != null ? _rabattWertFeld.value : "0");
        string typ = _rabattTypDropdown != null ? _rabattTypDropdown.value : "Kein Rabatt";
        float rabatt = 0f;
        if (typ == "Prozent") rabatt = netto * rabattWert / 100f;
        else if (typ == "Festbetrag") rabatt = rabattWert;

        // Skonto: Prozentwert auf den Betrag nach Rabatt
        float nettoNachRabatt = netto - rabatt;
        float skontoWert = ParseBetrag(_skontoWertFeld != null ? _skontoWertFeld.value : "0");
        float skonto = nettoNachRabatt * skontoWert / 100f;

        float gesamtSumme = nettoNachRabatt - skonto;

        if (_nettoLabel != null) _nettoLabel.text = FormatBetrag(netto);
        if (_rabattLabel != null) _rabattLabel.text = FormatBetrag(rabatt);
        if (_skontoLabel != null) _skontoLabel.text = FormatBetrag(skonto);
        if (_gesamtLabel != null) _gesamtLabel.text = FormatBetrag(gesamtSumme);
    }

    // ============================================================
    // KUNDENSUCHE
    // ============================================================

    private void RegistriereKundensuche()
    {
        if (_kundensuche == null) return;

        _suchErgebnisListe = new VisualElement();
        _suchErgebnisListe.style.display = DisplayStyle.None;
        _suchErgebnisListe.style.backgroundColor =
            new Color(45f / 255f, 45f / 255f, 45f / 255f);
        _suchErgebnisListe.style.borderTopWidth = 1; _suchErgebnisListe.style.borderRightWidth = 1;
        _suchErgebnisListe.style.borderBottomWidth = 1; _suchErgebnisListe.style.borderLeftWidth = 1;
        _suchErgebnisListe.style.borderTopColor = Gruen; _suchErgebnisListe.style.borderRightColor = Gruen;
        _suchErgebnisListe.style.borderBottomColor = Gruen; _suchErgebnisListe.style.borderLeftColor = Gruen;
        _suchErgebnisListe.style.borderTopLeftRadius = 6; _suchErgebnisListe.style.borderTopRightRadius = 6;
        _suchErgebnisListe.style.borderBottomLeftRadius = 6; _suchErgebnisListe.style.borderBottomRightRadius = 6;
        _suchErgebnisListe.style.marginTop = 4;

        var eltern = _kundensuche.parent;
        eltern.Insert(eltern.IndexOf(_kundensuche) + 1, _suchErgebnisListe);
        _kundensuche.RegisterValueChangedCallback(evt => AktualisiereKundensuche(evt.newValue));
    }

    private void AktualisiereKundensuche(string suchtext)
    {
        _suchErgebnisListe.Clear();
        if (string.IsNullOrWhiteSpace(suchtext) || suchtext.Trim().Length < 2)
        {
            _suchErgebnisListe.style.display = DisplayStyle.None;
            return;
        }

        List<Customer> kunden;
        try { kunden = UserDatabaseAccess.getCurrentUserDatabase().getAllCustomers(); }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kundensuche: " + e.Message);
            return;
        }

        string suche = suchtext.Trim().ToLowerInvariant();
        var treffer = kunden
            .Where(k => KundenAnzeige(k).ToLowerInvariant().Contains(suche))
            .Take(6).ToList();

        if (treffer.Count == 0)
        {
            var hinweis = new Label("Kein Kunde gefunden");
            hinweis.style.color = new Color(0.6f, 0.6f, 0.6f);
            hinweis.style.fontSize = 12;
            hinweis.style.paddingLeft = 10;
            hinweis.style.paddingTop = 6;
            hinweis.style.paddingBottom = 6;
            _suchErgebnisListe.Add(hinweis);
        }
        else
        {
            foreach (var kunde in treffer)
            {
                var aktuellerKunde = kunde;
                var eintrag = new Label(KundenAnzeige(aktuellerKunde));
                eintrag.style.color = Color.white;
                eintrag.style.fontSize = 13;
                eintrag.style.paddingLeft = 10;
                eintrag.style.paddingRight = 10;
                eintrag.style.paddingTop = 6;
                eintrag.style.paddingBottom = 6;
                eintrag.RegisterCallback<MouseEnterEvent>(_ =>
                    eintrag.style.backgroundColor =
                        new Color(Gruen.r, Gruen.g, Gruen.b, 0.2f));
                eintrag.RegisterCallback<MouseLeaveEvent>(_ =>
                    eintrag.style.backgroundColor = Color.clear);
                eintrag.RegisterCallback<ClickEvent>(_ => WaehleKunde(aktuellerKunde));
                _suchErgebnisListe.Add(eintrag);
            }
        }
        _suchErgebnisListe.style.display = DisplayStyle.Flex;
    }

    private void WaehleKunde(Customer kunde)
    {
        _ausgewaehlterKundeId = kunde.id;
        _ausgewaehlterKundeAdresse = KundenAdresse(kunde);
        _ausgewaehlterKunde = KundenAnzeige(kunde);
        _kundensuche.SetValueWithoutNotify(_ausgewaehlterKunde);
        _suchErgebnisListe.style.display = DisplayStyle.None;
        var boxen = Root.Query(className: "angebot-address-box").ToList();
        NormalisiereAdressBoxen(boxen);
        if (boxen.Count > 0)
        {
            SetzeAdresse(boxen[0], "Kunde:", _ausgewaehlterKundeAdresse);
        }
        //SetzeAdresse(boxen[0], "Rechnungsempf\u00e4nger:", KundenAdresse(kunde));
    }

    // ============================================================
    // ANHÄNGE
    // ============================================================

    private void RegistriereAnhaenge()
    {
        _anhangBereich = Root.Q<VisualElement>("anhaenge-inhalt");
        if (_anhangBereich == null) return;

        _anhangBereich.Clear();
        _anhangAusgewaehlt.Clear();

        var verfuegbar = BelegAnhangController.HoleVerfuegbareAnhaenge();
        var alleTitel  = BelegAnhangController.HoleAlleBezahlweiseTitel();

        foreach (string key in alleTitel)
        {
            bool vorhanden = verfuegbar.ContainsKey(key) && verfuegbar[key];
            _anhangAusgewaehlt[key] = false;
            string lokalerKey = key;

            var zeile = new VisualElement();
            zeile.style.flexDirection = FlexDirection.Row;
            zeile.style.alignItems = Align.Center;
            zeile.style.marginBottom = 4;
            zeile.style.paddingTop = 4; zeile.style.paddingBottom = 4;
            zeile.style.paddingLeft = 8; zeile.style.paddingRight = 8;
            zeile.style.borderTopLeftRadius = 6; zeile.style.borderTopRightRadius = 6;
            zeile.style.borderBottomLeftRadius = 6; zeile.style.borderBottomRightRadius = 6;
            zeile.style.backgroundColor = vorhanden
                ? new Color(55f / 255f, 55f / 255f, 55f / 255f)
                : new Color(40f / 255f, 40f / 255f, 40f / 255f);

            var box = new VisualElement();
            box.style.width = 18; box.style.height = 18;
            box.style.flexShrink = 0; box.style.marginRight = 8;
            box.style.borderTopLeftRadius = 4; box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4; box.style.borderBottomRightRadius = 4;
            box.style.borderTopWidth = 1; box.style.borderRightWidth = 1;
            box.style.borderBottomWidth = 1; box.style.borderLeftWidth = 1;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;

            var haken = new Label("");
            haken.style.fontSize = 11;
            haken.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(haken);

            AktualisiereCheckboxOptik(box, haken, false, vorhanden);

            var label = new Label(key);
            label.style.fontSize = 12;
            label.style.color = vorhanden ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            label.style.flexGrow = 1;

            zeile.Add(box);
            zeile.Add(label);

            if (!vorhanden)
            {
                var fehlt = new Label("(nicht im Pool)");
                fehlt.style.fontSize = 10;
                fehlt.style.color = new Color(0.4f, 0.4f, 0.4f);
                fehlt.style.marginLeft = 4;
                zeile.Add(fehlt);
            }

            if (vorhanden)
            {
                zeile.RegisterCallback<ClickEvent>(_ =>
                {
                    bool neuerWert = !_anhangAusgewaehlt[lokalerKey];
                    _anhangAusgewaehlt[lokalerKey] = neuerWert;
                    AktualisiereCheckboxOptik(box, haken, neuerWert, true);
                    zeile.style.backgroundColor = neuerWert
                        ? new Color(128f / 255f, 207f / 255f, 149f / 255f, 0.15f)
                        : new Color(55f / 255f, 55f / 255f, 55f / 255f);
                });
                zeile.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (!_anhangAusgewaehlt[lokalerKey])
                        zeile.style.backgroundColor =
                            new Color(65f / 255f, 65f / 255f, 65f / 255f);
                });
                zeile.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    zeile.style.backgroundColor = _anhangAusgewaehlt[lokalerKey]
                        ? new Color(128f / 255f, 207f / 255f, 149f / 255f, 0.15f)
                        : new Color(55f / 255f, 55f / 255f, 55f / 255f);
                });
            }

            _anhangBereich.Add(zeile);
        }
    }

    private static void AktualisiereCheckboxOptik(
        VisualElement box, Label haken, bool ausgewaehlt, bool vorhanden)
    {
        if (!vorhanden)
        {
            box.style.backgroundColor = new Color(35f / 255f, 35f / 255f, 35f / 255f);
            box.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            haken.text = "";
            haken.style.color = Color.clear;
            return;
        }

        if (ausgewaehlt)
        {
            box.style.backgroundColor = Gruen;
            box.style.borderTopColor = Gruen;
            box.style.borderRightColor = Gruen;
            box.style.borderBottomColor = Gruen;
            box.style.borderLeftColor = Gruen;
            haken.text = "\u2713";
            haken.style.color = new Color(38f / 255f, 38f / 255f, 38f / 255f);
        }
        else
        {
            box.style.backgroundColor = new Color(70f / 255f, 70f / 255f, 70f / 255f);
            box.style.borderTopColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderRightColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderBottomColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderLeftColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            haken.text = "";
            haken.style.color = Color.clear;
        }
    }

    public List<string> HoleAusgewaehlteAnhaenge()
    {
        return _anhangAusgewaehlt
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    // ============================================================
    // STATUS & SPEICHERN
    // ============================================================

    private void SpeichernGeklickt()
    {
        if (!VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten()))
            return;

        if (!PflichtfelderGefuellt())
            return;

        try
        {
            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                FeedbackPopup.Show(Root, "Keine Datenbank gefunden", FeedbackTyp.Fehler);
                return;
            }

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

            PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();
            string rawUserId = currentUser.userId.Replace("user_", "");
            int userId = int.Parse(rawUserId);

            if (BelegTyp == "Angebot")
            {
                Offer offer = new Offer
                {
                    customerId = _ausgewaehlterKundeId,
                    customerName = _ausgewaehlterKunde,
                    customerAddress = _ausgewaehlterKundeAdresse,
                    companyName = HoleCompanyName(db),
                    companyAddress = HoleCompanyAddress(db),
                    offerNumber = _nummerFeld != null ? _nummerFeld.value : "",
                    date = _datumFeld != null ? _datumFeld.value : DateTime.Now.ToString("dd.MM.yyyy"),
                    validUntil = _fristFeld != null ? _fristFeld.value : "",
                    status = _statusDropdown != null ? _statusDropdown.value : "Entwurf",
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

                List<OfferItem> items = new List<OfferItem>();

                foreach (var zeile in _zeilen.ToList())
                {
                    OfferItem item = new OfferItem
                    {
                        offerId = offerId,
                        articleNumber = zeile.Artikel != null ? zeile.Artikel.text : "",
                        description = zeile.Beschreibung != null ? zeile.Beschreibung.text : "",
                        quantity = Mathf.RoundToInt(ParseBetrag(zeile.Menge != null ? zeile.Menge.value : "0")),
                        unitPrice = ParseBetrag(zeile.Preis != null ? zeile.Preis.text : "0")
                    };

                    db.createOfferItem(item);
                    items.Add(item);
                }

                OfferPdfExporter.ExportOfferToPdf(offer, items, userId, db, HoleAusgewaehlteAnhaenge());
            }
            else if (BelegTyp == "Rechnung")
            {
                Invoice invoice = new Invoice
                {
                    customerId = _ausgewaehlterKundeId,
                    customerName = _ausgewaehlterKunde,
                    customerAddress = _ausgewaehlterKundeAdresse,
                    companyName = HoleCompanyName(db),
                    companyAddress = HoleCompanyAddress(db),
                    invoiceNumber = _nummerFeld != null ? _nummerFeld.value : "",
                    date = _datumFeld != null ? _datumFeld.value : DateTime.Now.ToString("dd.MM.yyyy"),
                    dueDate = _fristFeld != null ? _fristFeld.value : "",
                    status = _statusDropdown != null ? _statusDropdown.value : "Entwurf",
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

                int invoiceId = db.createInvoice(invoice);

                foreach (var zeile in _zeilen.ToList())
                {
                    InvoiceItem item = new InvoiceItem
                    {
                        invoiceId = invoiceId,
                        articleNumber = zeile.Artikel != null ? zeile.Artikel.text : "",
                        description = zeile.Beschreibung != null ? zeile.Beschreibung.text : "",
                        quantity = Mathf.RoundToInt(ParseBetrag(zeile.Menge != null ? zeile.Menge.value : "0")),
                        unitPrice = ParseBetrag(zeile.Preis != null ? zeile.Preis.text : "0")
                    };

                    db.createInvoiceItem(item);
                }

                List<InvoiceItem> items = db.getItemsByInvoice(invoiceId);
                InvoicePdfExporter.ExportInvoiceToPdf(invoice, items, userId, db, HoleAusgewaehlteAnhaenge());
            }

            ResetBelegFormular();
            FeedbackPopup.Show(Root, BelegTyp + " gespeichert", FeedbackTyp.Erfolg);
        }
        catch (Exception e)
        {
            Debug.LogError("[" + BelegTyp + "] Speicherfehler: " + e);
            FeedbackPopup.Show(Root, "Speichern fehlgeschlagen", FeedbackTyp.Fehler);
        }
    }

    protected virtual void StatusGeklickt(bool angenommen)
    {
        if (angenommen && !VoraussetzungsPopup.Pruefen(Root, PruefePflichtdaten()))
            return;

        if (angenommen && !PflichtfelderGefuellt())
            return;

        string neuerStatus = angenommen ? "Angenommen" : "Abgelehnt";

        _statusDropdown?.SetValueWithoutNotify(neuerStatus);
        AktualisiereUmwandelnButton();

        if (angenommen)
            UebernimmInsKassenbuch();

        FeedbackPopup.Show(
            Root,
            angenommen ? BelegTyp + " bestätigt" : BelegTyp + " abgelehnt",
            angenommen ? FeedbackTyp.Erfolg : FeedbackTyp.Fehler
        );
    }

    protected void UebernimmInsKassenbuch()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            float gesamt = ParseBetrag(_gesamtLabel != null ? _gesamtLabel.text : "0");
            string nummer = _nummerFeld != null ? _nummerFeld.value : "";
            string kunde = !string.IsNullOrEmpty(_ausgewaehlterKunde)
                            ? " - " + _ausgewaehlterKunde : "";
            string datum = _datumFeld != null && !string.IsNullOrWhiteSpace(_datumFeld.value)
                            ? _datumFeld.value
                            : DateTime.Now.ToString("dd.MM.yyyy");

            db.createEinkommen(gesamt, BelegTyp + " " + nummer + kunde, datum);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kassenbuch: " + e.Message);
        }
    }

    // ============================================================
    // PDF-EXPORT
    // ============================================================

    private void ExportierePDF()
    {
        if (!PflichtfelderGefuellt()) return;

        try
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string zeitstempel = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string nummer = _nummerFeld != null ? _nummerFeld.value : BelegTyp;
            string zielPfad = System.IO.Path.Combine(
                desktopPath, nummer + "_" + zeitstempel + ".pdf");

            using (var fs = new System.IO.FileStream(
                zielPfad, System.IO.FileMode.Create,
                System.IO.FileAccess.Write, System.IO.FileShare.None))
            {
                var document = new iTextSharp.text.Document();
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
                document.Open();

                var titelFont = iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_BOLD, 16);
                var textFont = iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA, 11);
                var subFont = iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_OBLIQUE, 9);
                var fettFont = iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_BOLD, 13);

                var linie = new iTextSharp.text.pdf.draw.LineSeparator();

                document.Add(new iTextSharp.text.Paragraph(
                    BelegTyp + "  " + nummer, titelFont));
                document.Add(new iTextSharp.text.Paragraph(
                    "Datum: " + (_datumFeld != null ? _datumFeld.value : "") +
                    "   Kunde: " + _ausgewaehlterKunde, subFont));
                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Chunk(linie));
                document.Add(new iTextSharp.text.Paragraph(" "));

                document.Add(new iTextSharp.text.Paragraph("Positionen", titelFont));
                document.Add(new iTextSharp.text.Paragraph(" "));

                foreach (var zeile in _zeilen)
                {
                    int menge = 1;
                    int.TryParse(zeile.Menge.value, out menge);
                    float preis = ParseBetrag(zeile.Preis.text);
                    float gesamt = menge * preis;

                    string zeileText = zeile.Artikel.text
                        + "   " + zeile.Beschreibung.text
                        + "   Menge: " + menge
                        + "   " + zeile.Einheit.text
                        + "   " + FormatBetrag(preis)
                        + "   Netto: " + FormatBetrag(gesamt);

                    document.Add(new iTextSharp.text.Paragraph(zeileText, textFont));
                }

                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Chunk(linie));
                document.Add(new iTextSharp.text.Paragraph(" "));

                document.Add(new iTextSharp.text.Paragraph(
                    "Netto (EUR):       " +
                    (_nettoLabel != null ? _nettoLabel.text : ""), textFont));
                document.Add(new iTextSharp.text.Paragraph(
                    "Rabatt:            " +
                    (_rabattLabel != null ? _rabattLabel.text : ""), textFont));
                document.Add(new iTextSharp.text.Paragraph(
                    "Skonto:            " +
                    (_skontoLabel != null ? _skontoLabel.text : ""), textFont));
                document.Add(new iTextSharp.text.Paragraph(" "));
                document.Add(new iTextSharp.text.Paragraph(
                    "Gesamtpreis (EUR): " +
                    (_gesamtLabel != null ? _gesamtLabel.text : ""), fettFont));

                BelegAnhangController.SchreibeAnhaenge(document, HoleAusgewaehlteAnhaenge());

                document.Close();
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = zielPfad,
                UseShellExecute = true
            });

            FeedbackPopup.Show(Root, "PDF exportiert", FeedbackTyp.Erfolg);
        }
        catch (Exception e)
        {
            Debug.LogError("[" + BelegTyp + "] PDF-Export fehlgeschlagen: " + e.Message);
            FeedbackPopup.Show(Root, "Export fehlgeschlagen", FeedbackTyp.Fehler);
        }
    }

    // ============================================================
    // EINGABE-HILFSMETHODEN
    // ============================================================

    protected static void NurGanzeZahlen(TextField feld)
    {
        if (feld == null) return;
        feld.RegisterCallback<KeyDownEvent>(evt =>
        {
            bool erlaubt = char.IsDigit(evt.character)
                        || evt.keyCode == KeyCode.Backspace
                        || evt.keyCode == KeyCode.Delete
                        || evt.keyCode == KeyCode.LeftArrow
                        || evt.keyCode == KeyCode.RightArrow;
            if (!erlaubt) { evt.StopPropagation(); evt.PreventDefault(); }
        }, TrickleDown.TrickleDown);
    }

    private static void NurZahlenmitKomma(TextField feld)
    {
        if (feld == null) return;
        feld.RegisterCallback<KeyDownEvent>(evt =>
        {
            bool erlaubt = char.IsDigit(evt.character)
                        || evt.character == ','
                        || evt.character == '.'
                        || evt.keyCode == KeyCode.Backspace
                        || evt.keyCode == KeyCode.Delete
                        || evt.keyCode == KeyCode.LeftArrow
                        || evt.keyCode == KeyCode.RightArrow;
            if (!erlaubt) { evt.StopPropagation(); evt.PreventDefault(); }
        }, TrickleDown.TrickleDown);
    }

    private static void NurDatumzeichen(TextField feld)
    {
        if (feld == null) return;

        feld.RegisterCallback<KeyDownEvent>(evt =>
        {
            bool erlaubt = char.IsDigit(evt.character)
                        || evt.character == '.'
                        || evt.keyCode == KeyCode.Backspace
                        || evt.keyCode == KeyCode.Delete
                        || evt.keyCode == KeyCode.LeftArrow
                        || evt.keyCode == KeyCode.RightArrow
                        || evt.keyCode == KeyCode.Home
                        || evt.keyCode == KeyCode.End;
            if (!erlaubt) { evt.StopPropagation(); evt.PreventDefault(); }
        }, TrickleDown.TrickleDown);

        feld.RegisterValueChangedCallback(evt =>
        {
            string neu = evt.newValue ?? "";
            string alt = evt.previousValue ?? "";
            if (neu.Length <= alt.Length) return;

            string nurZiffern = "";
            foreach (char c in neu)
                if (char.IsDigit(c)) nurZiffern += c;

            if (nurZiffern.Length > 8)
                nurZiffern = nurZiffern.Substring(0, 8);

            string formatiert = nurZiffern;
            if (nurZiffern.Length > 4)
                formatiert = nurZiffern.Substring(0, 2) + "."
                           + nurZiffern.Substring(2, 2) + "."
                           + nurZiffern.Substring(4);
            else if (nurZiffern.Length > 2)
                formatiert = nurZiffern.Substring(0, 2) + "." + nurZiffern.Substring(2);

            if (formatiert == neu) return;

            int cursorVorher = feld.cursorIndex;
            int cursorNachher = cursorVorher + (formatiert.Length - neu.Length);
            if (cursorNachher < 0) cursorNachher = 0;
            if (cursorNachher > formatiert.Length) cursorNachher = formatiert.Length;

            feld.SetValueWithoutNotify(formatiert);
            feld.schedule.Execute(() =>
            {
                feld.cursorIndex = cursorNachher;
                feld.selectIndex = cursorNachher;
            });
        });
    }

    protected TextField NeuesTextFeld()
    {
        var feld = new TextField();
        feld.AddToClassList("angebot-input");
        feld.style.marginRight = 8;

        var eingabe = feld.Q(className: "unity-base-field__input");
        if (eingabe != null)
        {
            eingabe.style.backgroundColor = FeldFarbe;
            eingabe.style.color = Color.white;
            eingabe.style.borderTopWidth = 0; eingabe.style.borderRightWidth = 0;
            eingabe.style.borderBottomWidth = 0; eingabe.style.borderLeftWidth = 0;
            eingabe.style.borderTopLeftRadius = 6; eingabe.style.borderTopRightRadius = 6;
            eingabe.style.borderBottomLeftRadius = 6; eingabe.style.borderBottomRightRadius = 6;
            eingabe.style.paddingLeft = 8;
        }
        return feld;
    }

    protected float ParseBetrag(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        string bereinigt = text
            .Replace("EUR", "")
            .Replace("€", "")
            .Replace("\u00A0", "")
            .Trim();

        if (float.TryParse(
            bereinigt,
            NumberStyles.Number,
            De,
            out float wert
        ))
        {
            return wert;
        }

        bereinigt = bereinigt
    .Replace(".", "")
    .Replace(",", ".");

        if (float.TryParse(
            bereinigt,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out wert
        ))
        {
            return wert;
        }

        return 0f;
    }

    protected string FormatBetrag(float wert)
    {
        return wert.ToString("N2", De) + " EUR";
    }

    protected string LiesFeld(object objekt, params string[] feldNamen)
    {
        if (objekt == null) return "";
        var typ = objekt.GetType();
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        foreach (string name in feldNamen)
        {
            var eigenschaft = typ.GetProperty(name, flags);
            if (eigenschaft != null)
            {
                var wert = eigenschaft.GetValue(objekt);
                if (wert != null && wert.ToString().Length > 0) return wert.ToString();
            }
            var feld = typ.GetField(name, flags);
            if (feld != null)
            {
                var wert = feld.GetValue(objekt);
                if (wert != null && wert.ToString().Length > 0) return wert.ToString();
            }
        }
        return "";
    }

    protected string KundenAnzeige(object kunde)
    {
        string vorname = LiesFeld(kunde, "vorname", "firstName");
        string nachname = LiesFeld(kunde, "nachname", "lastName", "name", "firma");
        string anzeige = (vorname + " " + nachname).Trim();
        return string.IsNullOrEmpty(anzeige) ? "Unbenannter Kunde" : anzeige;
    }

    private string KundenAdresse(object kunde)
    {
        var zeilen = new List<string> { KundenAnzeige(kunde) };

        string strasse = LiesFeld(kunde, "strasse", "adresse", "address", "street");
        if (!string.IsNullOrEmpty(strasse)) zeilen.Add(strasse);

        string plzOrt = (LiesFeld(kunde, "plz", "postleitzahl", "zip") + " " +
                         LiesFeld(kunde, "ort", "stadt", "city", "location")).Trim();
        if (!string.IsNullOrEmpty(plzOrt)) zeilen.Add(plzOrt);

        string email = LiesFeld(kunde, "email", "mail");
        if (!string.IsNullOrEmpty(email)) zeilen.Add(email);

        return string.Join("\n", zeilen);
    }

    protected string HoleCompanyName(DataBase db)
    {
        List<Company> companies = db.getAllCompanies();

        if (companies == null || companies.Count == 0)
            return "Keine Firmendaten";

        Company company = companies[companies.Count - 1];

        return LiesFeld(company, "name");
    }

    protected string HoleCompanyAddress(DataBase db)
    {
        List<Company> companies = db.getAllCompanies();

        if (companies == null || companies.Count == 0)
            return "";

        Company company = companies[companies.Count - 1];

        string strasse = LiesFeld(company, "strasseuHausNR", "street", "strasse", "adresse", "address");
        string plz = LiesFeld(company, "zip", "plz", "postleitzahl");
        string ort = LiesFeld(company, "city", "stadt", "ort", "location");

        List<string> zeilen = new List<string>();

        if (!string.IsNullOrWhiteSpace(strasse))
            zeilen.Add(strasse);

        string plzOrt = (plz + " " + ort).Trim();

        if (!string.IsNullOrWhiteSpace(plzOrt))
            zeilen.Add(plzOrt);

        return string.Join("\n", zeilen);
    }

    private void SpeichereSession()
    {
        var snap = new BelegSessionDaten.BelegSnapshot
        {
            Nummer = _nummerFeld != null ? _nummerFeld.value : "",
            Status = _statusDropdown != null ? _statusDropdown.value : "",
            Datum = _datumFeld != null ? _datumFeld.value : "",
            Frist = _fristFeld != null ? _fristFeld.value : "",
            Referenz = Root.Q<TextField>(ReferenzFeldName)?.value ?? "",
            RabattTyp = _rabattTypDropdown != null ? _rabattTypDropdown.value : "",
            RabattWert = _rabattWertFeld != null ? _rabattWertFeld.value : "",
            SkontoWert = _skontoWertFeld != null ? _skontoWertFeld.value : "",
            Notiz = _notizenFeld != null ? _notizenFeld.value : "",
            KundeId = _ausgewaehlterKundeId,
            KundeName = _ausgewaehlterKunde,
            KundeAdresse = _ausgewaehlterKundeAdresse,
            AusgewaehlteAnhaenge = HoleAusgewaehlteAnhaenge(),
        };

        foreach (var zeile in _zeilen)
        {
            snap.Positionen.Add(new BelegSessionDaten.PositionsEintrag
            {
                Artikel = zeile.Artikel != null ? zeile.Artikel.text : "",
                Beschreibung = zeile.Beschreibung != null ? zeile.Beschreibung.text : "",
                Menge = zeile.Menge != null ? zeile.Menge.value : "1",
                Einheit = zeile.Einheit != null ? zeile.Einheit.text : "",
                Preis = zeile.Preis != null ? zeile.Preis.text : "",
            });
        }

        BelegSessionDaten.Speichere(BelegTyp, snap);
    }

    private void WiederherstelleSession(BelegSessionDaten.BelegSnapshot snap)
    {
        // Felder befüllen
        _nummerFeld?.SetValueWithoutNotify(snap.Nummer);
        _datumFeld?.SetValueWithoutNotify(snap.Datum);
        _fristFeld?.SetValueWithoutNotify(snap.Frist);
        Root.Q<TextField>(ReferenzFeldName)?.SetValueWithoutNotify(snap.Referenz);
        _notizenFeld?.SetValueWithoutNotify(snap.Notiz);

        if (_statusDropdown != null && !string.IsNullOrEmpty(snap.Status)
            && StatusOptionen.Contains(snap.Status))
            _statusDropdown.schedule.Execute(() =>
                _statusDropdown.SetValueWithoutNotify(snap.Status)).ExecuteLater(80);

        if (_rabattTypDropdown != null && !string.IsNullOrEmpty(snap.RabattTyp))
            _rabattTypDropdown.SetValueWithoutNotify(snap.RabattTyp);

        _rabattWertFeld?.SetValueWithoutNotify(
            string.IsNullOrEmpty(snap.RabattWert) ? "0" : snap.RabattWert);
        _skontoWertFeld?.SetValueWithoutNotify(
            string.IsNullOrEmpty(snap.SkontoWert) ? "0" : snap.SkontoWert);

        // Anhänge-Auswahl wiederherstellen
        foreach (var key in snap.AusgewaehlteAnhaenge)
            if (_anhangAusgewaehlt.ContainsKey(key))
                _anhangAusgewaehlt[key] = true;

        // Positionen wiederherstellen
        foreach (var eintrag in snap.Positionen)
        {
            if (_positionenListe == null) break;

            var zeile = new PositionsZeile();
            var wurzel = new VisualElement();
            wurzel.AddToClassList("angebot-position-row");
            wurzel.style.flexDirection = FlexDirection.Row;
            wurzel.style.alignItems = Align.Center;
            wurzel.style.paddingTop = 6;
            wurzel.style.paddingBottom = 6;

            var punkt = new VisualElement();
            punkt.style.width = 10; punkt.style.height = 10;
            punkt.style.borderTopLeftRadius = 5; punkt.style.borderTopRightRadius = 5;
            punkt.style.borderBottomLeftRadius = 5; punkt.style.borderBottomRightRadius = 5;
            punkt.style.backgroundColor = Gruen;
            punkt.style.flexShrink = 0;
            punkt.style.marginRight = 8;

            zeile.Artikel = ErstelleZeilenLabel(eintrag.Artikel, true);
            zeile.Artikel.style.flexGrow = 1;
            zeile.Artikel.style.minWidth = 120;

            zeile.Beschreibung = ErstelleZeilenLabel(eintrag.Beschreibung, false);
            zeile.Beschreibung.style.width = 220;
            zeile.Beschreibung.style.marginRight = 8;

            zeile.Menge = NeuesTextFeld();
            zeile.Menge.style.width = 70;
            zeile.Menge.style.marginRight = 8;
            zeile.Menge.SetValueWithoutNotify(eintrag.Menge);
            NurGanzeZahlen(zeile.Menge);

            zeile.Einheit = ErstelleZeilenLabel(eintrag.Einheit, false);
            zeile.Einheit.style.width = 110;
            zeile.Einheit.style.marginRight = 8;

            zeile.Preis = ErstelleZeilenLabel(eintrag.Preis, false);
            zeile.Preis.style.width = 110;
            zeile.Preis.style.marginRight = 8;

            float preisWert = ParseBetrag(eintrag.Preis);
            int mengeWert = 1;
            int.TryParse(eintrag.Menge, out mengeWert);
            if (mengeWert < 1) mengeWert = 1;

            zeile.Gesamt = new Label(FormatBetrag(mengeWert * preisWert));
            zeile.Gesamt.style.width = 110;
            zeile.Gesamt.style.fontSize = 13;
            zeile.Gesamt.style.color = new Color(0.86f, 0.86f, 0.86f);
            zeile.Gesamt.style.unityTextAlign = TextAnchor.MiddleRight;

            var loeschen = new Button { text = "\u2715" };
            loeschen.style.width = 26; loeschen.style.height = 26;
            loeschen.style.marginLeft = 8;
            loeschen.style.backgroundColor = Color.clear;
            loeschen.style.color = new Color(0.6f, 0.6f, 0.6f);
            loeschen.style.fontSize = 12;
            loeschen.style.borderTopWidth = 0; loeschen.style.borderRightWidth = 0;
            loeschen.style.borderBottomWidth = 0; loeschen.style.borderLeftWidth = 0;
            loeschen.RegisterCallback<MouseEnterEvent>(_ => loeschen.style.color = Rot);
            loeschen.RegisterCallback<MouseLeaveEvent>(_ =>
                loeschen.style.color = new Color(0.6f, 0.6f, 0.6f));

            var lokalZeile = zeile;
            var lokalWurzel = wurzel;
            loeschen.RegisterCallback<ClickEvent>(_ =>
            {
                lokalWurzel.RemoveFromHierarchy();
                _zeilen.Remove(lokalZeile);
                BerechneSummen();
            });

            float preisProEinheit = preisWert;
            zeile.Menge.RegisterValueChangedCallback(_ =>
            {
                int m = 1;
                int.TryParse(zeile.Menge.value, out m);
                if (m < 1) m = 1;
                zeile.Gesamt.text = FormatBetrag(m * preisProEinheit);
                BerechneSummen();
            });

            wurzel.Add(punkt);
            wurzel.Add(zeile.Artikel);
            wurzel.Add(zeile.Beschreibung);
            wurzel.Add(zeile.Menge);
            wurzel.Add(zeile.Einheit);
            wurzel.Add(zeile.Preis);
            wurzel.Add(zeile.Gesamt);
            wurzel.Add(loeschen);

            zeile.Wurzel = wurzel;
            _zeilen.Add(zeile);
            _positionenListe.Add(wurzel);
        }

        // Kunden-Adressbox wiederherstellen
        if (!string.IsNullOrEmpty(snap.KundeName))
        {
            _ausgewaehlterKunde = snap.KundeName;
            _ausgewaehlterKundeId = snap.KundeId;
            _ausgewaehlterKundeAdresse = snap.KundeAdresse;
            _kundensuche?.SetValueWithoutNotify(snap.KundeName);

            try
            {
                var boxen = Root.Query(className: "angebot-address-box").ToList();
                NormalisiereAdressBoxen(boxen);
                if (boxen.Count > 0)
                {
                    var db = UserDatabaseAccess.getCurrentUserDatabase();
                    var kunden = db.getAllCustomers();
                    var kunde = kunden?.Find(k => k.id == snap.KundeId);
                    if (kunde != null)
                        SetzeAdresseStrukturiert(boxen[0], HoleEmpfaengerBezeichnung(),
                            new (string, string)[]
                            {
                                ("Name",    KundenAnzeige(kunde)),
                                ("Stra\u00dfe",  LiesFeld(kunde, "strasse", "adresse", "address", "street")),
                                ("Ort",     (LiesFeld(kunde, "plz", "postleitzahl", "zip") + " " +
                                             LiesFeld(kunde, "ort", "stadt", "city", "location")).Trim()),
                                ("E-Mail",  LiesFeld(kunde, "email", "mail")),
                            });
                }
            }
            catch { }
        }
    }

    private void ResetBelegFormular()
    {
        Debug.Log("[Reset-Test] ResetBelegFormular ausgeführt für: " + BelegTyp);

        BelegSessionDaten.Leere(BelegTyp);

        _zeilen.Clear();
        _positionenListe?.Clear();

        Root.Query<TextField>().ForEach(feld => feld.SetValueWithoutNotify(""));

        _ausgewaehlterKunde = "";
        _ausgewaehlterKundeId = 0;
        _ausgewaehlterKundeAdresse = "";

        var boxen = Root.Query(className: "angebot-address-box").ToList();
        NormalisiereAdressBoxen(boxen);

        if (boxen.Count > 0)
            SetzeAdressePlatzhalter(boxen[0], HoleEmpfaengerBezeichnung());

        if (boxen.Count > 1)
            LadeAbsenderdaten();

        SetzeStandardwerte();
        RegistriereSummenEingaben();
        BerechneSummen();

        if (_positionenListe != null)
            Debug.Log("[Reset-Test] PositionenListe nach Reset Count: " + _positionenListe.childCount);
    }

    protected float HoleMwstSatz()
    {
        int steuersatz = PlayerPrefs.GetInt("settings_steuersatz", 19);
        return steuersatz / 100f;
    }


    protected List<VoraussetzungsBereich> PruefePflichtdaten()
    {
        var fehlend = new List<VoraussetzungsBereich>();

        try
        {
            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                fehlend.Add(VoraussetzungsBereich.Unternehmensdaten);
                return fehlend;
            }

            var firmen = db.getAllCompanies();

            if (firmen == null || firmen.Count == 0)
            {
                fehlend.Add(VoraussetzungsBereich.Unternehmensdaten);
            }
            else
            {
                Company firma = firmen[firmen.Count - 1];

                string name = LiesFeld(firma, "name");
                string adresse = LiesFeld(firma, "location", "ort", "adresse", "address");

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(adresse))
                    fehlend.Add(VoraussetzungsBereich.Unternehmensdaten);
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_iban", "")) ||
                string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_bic", "")) ||
                string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_kontoinhaber", "")))
            {
                fehlend.Add(VoraussetzungsBereich.Bankverbindung);
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_rechnr_praefix", "")) ||
                string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_startnummer", "")) ||
                string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_zahlungsziel", "")))
            {
                fehlend.Add(VoraussetzungsBereich.Rechnungsformat);
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString("settings_zahlungshinweis", "")))
            {
                fehlend.Add(VoraussetzungsBereich.Bezahlweise);
            }
        }
        catch
        {
            fehlend.Add(VoraussetzungsBereich.Unternehmensdaten);
        }

        return fehlend;
    }



}
