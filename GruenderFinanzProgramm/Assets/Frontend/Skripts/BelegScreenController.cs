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

    protected const string PositionenListeName    = "positionen-liste";
    protected const string KundensucheName        = "input-kundensuche";
    protected const string AbsenderLabelName      = "label-absender";
    protected const string NummerFeldName         = "input-nummer";
    protected const string StatusDropdownName     = "dropdown-status";
    protected const string DatumFeldName          = "input-datum";
    protected const string FristFeldName          = "input-frist";
    protected const string RabattTypDropdownName  = "dropdown-rabatt-typ";
    protected const string RabattWertFeldName     = "input-rabatt-wert";
    protected const string SonstigeFeldName       = "input-sonstige-kosten";
    protected const string NettoLabelName         = "label-netto";
    protected const string RabattLabelName        = "label-rabatt";
    protected const string SonstigeLabelName      = "label-sonstige";
    protected const string GesamtLabelName        = "label-gesamt-total";
    protected const string SpeichernButtonName    = "btn-speichern";
    protected const string AngenommenButtonName   = "btn-angenommen";
    protected const string AbgelehntButtonName    = "btn-abgelehnt";
    protected const string PositionAddButtonName  = "btn-position-hinzufuegen";
    protected const string AnhangKarteName        = "card-anhaenge";

    protected static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly Color Gruen     = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot       = new Color(230f / 255f, 57f  / 255f, 70f  / 255f);
    private static readonly Color FeldFarbe = new Color(70f  / 255f, 70f  / 255f, 70f  / 255f);
    private static readonly Color KartenFarbe = new Color(38f / 255f, 38f  / 255f, 38f  / 255f);

    protected VisualElement Root;

    private ScrollView _positionenListe;
    private readonly List<PositionsZeile> _zeilen = new List<PositionsZeile>();
    private Label _nettoLabel, _rabattLabel, _sonstigeLabel, _gesamtLabel;
    private TextField _kundensuche, _nummerFeld, _datumFeld, _fristFeld,
                      _rabattWertFeld, _sonstigeFeld;
    private DropdownField _statusDropdown, _rabattTypDropdown;
    private VisualElement _suchErgebnisListe;
    private string _ausgewaehlterKunde = "";

    private readonly Dictionary<string, bool> _anhangAusgewaehlt = new Dictionary<string, bool>();
    private VisualElement _anhangBereich;

    private VisualElement _dienstleistungPopup;
    private List<Service> _dienstleistungen = new List<Service>();

    protected abstract string       BelegTyp       { get; }
    protected abstract string       NummernPrefix  { get; }
    protected abstract List<string> StatusOptionen { get; }
    protected virtual  void         RegistriereZusatzButtons() { }

    private class PositionsZeile
    {
        public VisualElement Wurzel;
        public Label         Artikel, Beschreibung, Einheit, Preis, Gesamt;
        public TextField     Menge;
    }

    // ─────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        Root = uiDocument.rootVisualElement;

        SammleElemente();
        LeereDemoInhalte();
        SetzeStandardwerte();
        LadeAbsenderdaten();
        LadeDienstleistungen();
        RegistriereSummenEingaben();
        RegistriereButtons();
        RegistriereKundensuche();
        RegistriereAnhaenge();
        BerechneSummen();
    }

    // ─────────────────────────────────────────
    // ELEMENTE SAMMELN
    // ─────────────────────────────────────────

    private void SammleElemente()
    {
        _positionenListe = Root.Q<ScrollView>(PositionenListeName) ?? Root.Q<ScrollView>();
        if (_positionenListe != null)
            _positionenListe.verticalScrollerVisibility = ScrollerVisibility.Auto;

        _nettoLabel    = Root.Q<Label>(NettoLabelName);
        _rabattLabel   = Root.Q<Label>(RabattLabelName);
        _sonstigeLabel = Root.Q<Label>(SonstigeLabelName);
        _gesamtLabel   = Root.Q<Label>(GesamtLabelName);

        _kundensuche  = Root.Q<TextField>(KundensucheName);
        _nummerFeld   = Root.Q<TextField>(NummerFeldName);
        _datumFeld    = Root.Q<TextField>(DatumFeldName);
        _fristFeld    = Root.Q<TextField>(FristFeldName);

        _statusDropdown    = Root.Q<DropdownField>(StatusDropdownName);
        _rabattTypDropdown = Root.Q<DropdownField>(RabattTypDropdownName);
    }

    // ─────────────────────────────────────────
    // DEMO-INHALTE LEEREN
    // ─────────────────────────────────────────

    private void LeereDemoInhalte()
    {
        _positionenListe?.Clear();
        Root.Query<TextField>().ForEach(f => f.SetValueWithoutNotify(""));

        var boxen = Root.Query(className: "angebot-address-box").ToList();
        if (boxen.Count > 0) SetzeAdresse(boxen[0], "Kunde:", "Kunde auswählen");
        if (boxen.Count > 1) SetzeAdresse(boxen[1], "Von:",   "");
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
        text.style.color = string.IsNullOrEmpty(inhalt) || inhalt == "Kunde auswählen"
            ? new Color(0.59f, 0.59f, 0.59f)
            : new Color(0.78f, 0.78f, 0.78f);
        text.style.whiteSpace = WhiteSpace.Normal;
        box.Add(text);
    }

    // ─────────────────────────────────────────
    // STANDARDWERTE
    // ─────────────────────────────────────────

    private void SetzeStandardwerte()
    {
        _datumFeld?.SetValueWithoutNotify(DateTime.Now.ToString("dd.MM.yyyy"));
        _fristFeld?.SetValueWithoutNotify(DateTime.Now.AddDays(14).ToString("dd.MM.yyyy"));
        _nummerFeld?.SetValueWithoutNotify(
            string.Format("{0}-{1:yyyyMMdd-HHmm}", NummernPrefix, DateTime.Now));

        NurDatumzeichen(_datumFeld);
        NurDatumzeichen(_fristFeld);

        if (_statusDropdown != null)
        {
            _statusDropdown.choices = StatusOptionen;
            _statusDropdown.SetValueWithoutNotify(StatusOptionen[0]);
        }
        if (_rabattTypDropdown != null)
        {
            _rabattTypDropdown.choices = new List<string> { "Kein Rabatt", "Prozent", "Festbetrag" };
            _rabattTypDropdown.SetValueWithoutNotify("Kein Rabatt");
            _rabattTypDropdown.RegisterValueChangedCallback(_ => BerechneSummen());
        }
    }

    // ─────────────────────────────────────────
    // ABSENDERDATEN
    // ─────────────────────────────────────────

    private void LadeAbsenderdaten()
    {
        var boxen = Root.Query(className: "angebot-address-box").ToList();
        VisualElement vonBox = boxen.Count > 1 ? boxen[1] : null;

        try
        {
            var db     = UserDatabaseAccess.getCurrentUserDatabase();
            var firmen = db.getAllCompanies();
            if (firmen != null && firmen.Count > 0)
            {
                string name    = LiesFeld(firmen[0], "name");
                string ort     = LiesFeld(firmen[0], "location", "ort", "adresse");
                string anzeige = string.IsNullOrEmpty(ort) ? name : name + "\n" + ort;

                var absenderLabel = Root.Q<Label>(AbsenderLabelName);
                if (absenderLabel != null) absenderLabel.text = anzeige;
                if (vonBox != null) SetzeAdresse(vonBox, "Von:", anzeige);
            }
            else
            {
                if (vonBox != null)
                    SetzeAdresse(vonBox, "Von:", "Firmendaten in den Einstellungen hinterlegen.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Absenderdaten: " + e.Message);
        }
    }

    // ─────────────────────────────────────────
    // DIENSTLEISTUNGEN LADEN
    // ─────────────────────────────────────────

    private void LadeDienstleistungen()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            _dienstleistungen = db.getAllServices() ?? new List<Service>();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Dienstleistungen: " + e.Message);
            _dienstleistungen = new List<Service>();
        }
    }

    // ─────────────────────────────────────────
    // SUMMEN-EINGABEN
    // ─────────────────────────────────────────

    private void RegistriereSummenEingaben()
    {
        _rabattWertFeld = Root.Q<TextField>(RabattWertFeldName)
                          ?? ErstelleSummenFeld("Rabatt (Wert)", RabattWertFeldName);
        _sonstigeFeld   = Root.Q<TextField>(SonstigeFeldName)
                          ?? ErstelleSummenFeld("Sonstige Kosten", SonstigeFeldName);

        _rabattWertFeld?.SetValueWithoutNotify("0");
        _sonstigeFeld?.SetValueWithoutNotify("0");

        NurZahlenmitKomma(_rabattWertFeld);
        NurZahlenmitKomma(_sonstigeFeld);

        _rabattWertFeld?.RegisterValueChangedCallback(_ => BerechneSummen());
        _sonstigeFeld?.RegisterValueChangedCallback(_ => BerechneSummen());
    }

    private TextField ErstelleSummenFeld(string beschriftung, string elementName)
    {
        VisualElement container = _rabattTypDropdown?.parent ?? _nettoLabel?.parent?.parent;
        if (container == null) return null;

        var titel = new Label(beschriftung);
        titel.style.fontSize = 12;
        titel.style.color = new Color(0.7f, 0.7f, 0.7f);
        titel.style.marginTop = 8;
        titel.style.marginBottom = 2;

        var feld = NeuesTextFeld();
        feld.name = elementName;
        container.Add(titel);
        container.Add(feld);
        return feld;
    }

    // ─────────────────────────────────────────
    // BUTTONS
    // ─────────────────────────────────────────

    private void RegistriereButtons()
    {
        FindeButton(SpeichernButtonName,  "Speichern") ?.RegisterCallback<ClickEvent>(_ => SpeichernGeklickt());
        FindeButton(AngenommenButtonName, "Angenommen")?.RegisterCallback<ClickEvent>(_ => StatusGeklickt(true));
        FindeButton(AbgelehntButtonName,  "Abgelehnt") ?.RegisterCallback<ClickEvent>(_ => StatusGeklickt(false));

        var hinzufuegen = FindeButton(PositionAddButtonName, "+");
        hinzufuegen?.RegisterCallback<ClickEvent>(_ => OeffneDienstleistungsPopup());

        RegistriereZusatzButtons();
    }

    protected Button FindeButton(string elementName, string buttonText)
    {
        var button = Root.Q<Button>(elementName);
        if (button != null) return button;
        return Root.Query<Button>().ToList()
                   .FirstOrDefault(b => b.text != null && b.text.Trim().StartsWith(buttonText));
    }

    // ─────────────────────────────────────────
    // DIENSTLEISTUNGS-POPUP
    // ─────────────────────────────────────────

    private void OeffneDienstleistungsPopup()
    {
        if (_dienstleistungPopup != null)
        {
            _dienstleistungPopup.RemoveFromHierarchy();
            _dienstleistungPopup = null;
        }

        LadeDienstleistungen();

        var overlay = new VisualElement();
        overlay.style.position       = Position.Absolute;
        overlay.style.left           = 0;
        overlay.style.right          = 0;
        overlay.style.top            = 0;
        overlay.style.bottom         = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        overlay.style.alignItems     = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var karte = new VisualElement();
        karte.style.width                   = 500;
        karte.style.backgroundColor         = KartenFarbe;
        karte.style.borderTopLeftRadius     = 12;
        karte.style.borderTopRightRadius    = 12;
        karte.style.borderBottomLeftRadius  = 12;
        karte.style.borderBottomRightRadius = 12;
        karte.style.borderTopWidth          = 2;
        karte.style.borderRightWidth        = 2;
        karte.style.borderBottomWidth       = 2;
        karte.style.borderLeftWidth         = 2;
        karte.style.borderTopColor          = Gruen;
        karte.style.borderRightColor        = Gruen;
        karte.style.borderBottomColor       = Gruen;
        karte.style.borderLeftColor         = Gruen;
        karte.style.paddingTop              = 24;
        karte.style.paddingBottom           = 24;
        karte.style.paddingLeft             = 24;
        karte.style.paddingRight            = 24;

        var titelZeile = new VisualElement();
        titelZeile.style.flexDirection  = FlexDirection.Row;
        titelZeile.style.justifyContent = Justify.SpaceBetween;
        titelZeile.style.alignItems     = Align.Center;
        titelZeile.style.marginBottom   = 16;

        var titel = new Label("Dienstleistung auswählen");
        titel.style.fontSize = 16;
        titel.style.color    = Color.white;
        titel.style.unityFontStyleAndWeight = FontStyle.Bold;

        var schliessen = new Button(() =>
        {
            overlay.RemoveFromHierarchy();
            _dienstleistungPopup = null;
        }) { text = "\u2715" };
        schliessen.style.backgroundColor  = Color.clear;
        schliessen.style.color            = new Color(0.7f, 0.7f, 0.7f);
        schliessen.style.fontSize         = 14;
        schliessen.style.borderTopWidth   = 0;
        schliessen.style.borderRightWidth = 0;
        schliessen.style.borderBottomWidth = 0;
        schliessen.style.borderLeftWidth  = 0;

        titelZeile.Add(titel);
        titelZeile.Add(schliessen);
        karte.Add(titelZeile);

        if (_dienstleistungen.Count == 0)
        {
            var hinweis = new Label("Keine Dienstleistungen hinterlegt.\nBitte zuerst im Bereich Dienstleistungen anlegen.");
            hinweis.style.color          = new Color(0.6f, 0.6f, 0.6f);
            hinweis.style.fontSize       = 13;
            hinweis.style.whiteSpace     = WhiteSpace.Normal;
            hinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
            hinweis.style.marginTop      = 20;
            hinweis.style.marginBottom   = 20;
            karte.Add(hinweis);
        }
        else
        {
            var dropdownLabel = new Label("Dienstleistung");
            dropdownLabel.style.fontSize     = 12;
            dropdownLabel.style.color        = new Color(0.7f, 0.7f, 0.7f);
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
                eingabe.style.backgroundColor       = FeldFarbe;
                eingabe.style.color                 = Color.white;
                eingabe.style.borderTopWidth        = 0;
                eingabe.style.borderRightWidth      = 0;
                eingabe.style.borderBottomWidth     = 0;
                eingabe.style.borderLeftWidth       = 0;
                eingabe.style.borderTopLeftRadius   = 6;
                eingabe.style.borderTopRightRadius  = 6;
                eingabe.style.borderBottomLeftRadius  = 6;
                eingabe.style.borderBottomRightRadius = 6;
                eingabe.style.paddingLeft           = 10;
            }
            karte.Add(dropdown);

            var mengeLabel = new Label("Menge");
            mengeLabel.style.fontSize     = 12;
            mengeLabel.style.color        = new Color(0.7f, 0.7f, 0.7f);
            mengeLabel.style.marginBottom = 4;
            karte.Add(mengeLabel);

            var mengeFeld = NeuesTextFeld();
            mengeFeld.SetValueWithoutNotify("1");
            mengeFeld.style.marginBottom = 20;
            NurGanzeZahlen(mengeFeld);
            karte.Add(mengeFeld);

            var btnHinzufuegen = new Button(() =>
            {
                int ausgewaehlterIndex = optionen.IndexOf(dropdown.value);
                if (ausgewaehlterIndex < 0 || ausgewaehlterIndex >= _dienstleistungen.Count)
                    return;

                var service = _dienstleistungen[ausgewaehlterIndex];
                int menge   = 1;
                int.TryParse(mengeFeld.value, out menge);
                if (menge < 1) menge = 1;

                FuegeZeileAusDienstleistungHinzu(service, menge);
                overlay.RemoveFromHierarchy();
                _dienstleistungPopup = null;
            }) { text = "Hinzufügen" };

            btnHinzufuegen.style.height                   = 40;
            btnHinzufuegen.style.backgroundColor          = Gruen;
            btnHinzufuegen.style.color                    = new Color(0.12f, 0.12f, 0.12f);
            btnHinzufuegen.style.fontSize                 = 13;
            btnHinzufuegen.style.unityFontStyleAndWeight  = FontStyle.Bold;
            btnHinzufuegen.style.borderTopWidth           = 0;
            btnHinzufuegen.style.borderRightWidth         = 0;
            btnHinzufuegen.style.borderBottomWidth        = 0;
            btnHinzufuegen.style.borderLeftWidth          = 0;
            btnHinzufuegen.style.borderTopLeftRadius      = 8;
            btnHinzufuegen.style.borderTopRightRadius     = 8;
            btnHinzufuegen.style.borderBottomLeftRadius   = 8;
            btnHinzufuegen.style.borderBottomRightRadius  = 8;
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

    private void FuegeZeileAusDienstleistungHinzu(Service service, int menge)
    {
        if (_positionenListe == null) return;

        var zeile  = new PositionsZeile();
        var wurzel = new VisualElement();
        wurzel.AddToClassList("angebot-position-row");
        wurzel.style.flexDirection = FlexDirection.Row;
        wurzel.style.alignItems    = Align.Center;
        wurzel.style.paddingTop    = 6;
        wurzel.style.paddingBottom = 6;

        var punkt = new VisualElement();
        punkt.style.width                   = 10;
        punkt.style.height                  = 10;
        punkt.style.borderTopLeftRadius     = 5;
        punkt.style.borderTopRightRadius    = 5;
        punkt.style.borderBottomLeftRadius  = 5;
        punkt.style.borderBottomRightRadius = 5;
        punkt.style.backgroundColor = Gruen;
        punkt.style.flexShrink  = 0;
        punkt.style.marginRight = 8;

        zeile.Artikel = ErstelleZeilenLabel(service.name, true);
        zeile.Artikel.style.flexGrow = 1;
        zeile.Artikel.style.minWidth = 120;

        zeile.Beschreibung = ErstelleZeilenLabel(service.description, false);
        zeile.Beschreibung.style.width       = 220;
        zeile.Beschreibung.style.marginRight = 8;

        zeile.Menge = NeuesTextFeld();
        zeile.Menge.style.width       = 70;
        zeile.Menge.style.marginRight = 8;
        zeile.Menge.SetValueWithoutNotify(menge.ToString());
        NurGanzeZahlen(zeile.Menge);

        zeile.Einheit = ErstelleZeilenLabel(
            string.IsNullOrEmpty(service.priceModel) ? "Stück" : service.priceModel, false);
        zeile.Einheit.style.width       = 110;
        zeile.Einheit.style.marginRight = 8;

        zeile.Preis = ErstelleZeilenLabel(((float)service.price).ToString("F2", De), false);
        zeile.Preis.style.width       = 110;
        zeile.Preis.style.marginRight = 8;

        float gesamt = menge * (float)service.price;
        zeile.Gesamt = new Label(FormatBetrag(gesamt));
        zeile.Gesamt.style.width          = 110;
        zeile.Gesamt.style.fontSize       = 13;
        zeile.Gesamt.style.color          = new Color(0.86f, 0.86f, 0.86f);
        zeile.Gesamt.style.unityTextAlign = TextAnchor.MiddleRight;

        var loeschen = new Button { text = "\u2715" };
        loeschen.style.width           = 26;
        loeschen.style.height          = 26;
        loeschen.style.marginLeft      = 8;
        loeschen.style.backgroundColor = Color.clear;
        loeschen.style.color           = new Color(0.6f, 0.6f, 0.6f);
        loeschen.style.fontSize        = 12;
        loeschen.style.borderTopWidth  = 0;
        loeschen.style.borderRightWidth = 0;
        loeschen.style.borderBottomWidth = 0;
        loeschen.style.borderLeftWidth = 0;
        loeschen.RegisterCallback<MouseEnterEvent>(_ => loeschen.style.color = Rot);
        loeschen.RegisterCallback<MouseLeaveEvent>(_ => loeschen.style.color = new Color(0.6f, 0.6f, 0.6f));
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

    private Label ErstelleZeilenLabel(string text, bool bold)
    {
        var label = new Label(text);
        label.style.fontSize = 13;
        label.style.color    = new Color(0.86f, 0.86f, 0.86f);
        label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        label.style.overflow   = Overflow.Hidden;
        label.style.whiteSpace = WhiteSpace.NoWrap;
        return label;
    }

    // ─────────────────────────────────────────
    // SUMMEN BERECHNEN
    // ─────────────────────────────────────────

    protected void BerechneSummen()
    {
        float netto = 0f;
        foreach (var zeile in _zeilen)
        {
            int   menge = 1;
            float preis = 0f;
            int.TryParse(zeile.Menge.value, out menge);
            if (menge < 1) menge = 1;
            preis = ParseBetrag(zeile.Preis.text);
            float gesamt = menge * preis;
            zeile.Gesamt.text = FormatBetrag(gesamt);
            netto += gesamt;
        }

        float  rabattWert = ParseBetrag(_rabattWertFeld != null ? _rabattWertFeld.value : "0");
        string typ        = _rabattTypDropdown != null ? _rabattTypDropdown.value : "Kein Rabatt";
        float  rabatt     = 0f;
        if (typ == "Prozent")         rabatt = netto * rabattWert / 100f;
        else if (typ == "Festbetrag") rabatt = rabattWert;

        float sonstige    = ParseBetrag(_sonstigeFeld != null ? _sonstigeFeld.value : "0");
        float gesamtSumme = netto - rabatt + sonstige;

        if (_nettoLabel    != null) _nettoLabel.text    = FormatBetrag(netto);
        if (_rabattLabel   != null) _rabattLabel.text   = FormatBetrag(rabatt);
        if (_sonstigeLabel != null) _sonstigeLabel.text = FormatBetrag(sonstige);
        if (_gesamtLabel   != null) _gesamtLabel.text   = FormatBetrag(gesamtSumme);
    }

    // ─────────────────────────────────────────
    // KUNDENSUCHE
    // ─────────────────────────────────────────

    private void RegistriereKundensuche()
    {
        if (_kundensuche == null) return;

        _suchErgebnisListe = new VisualElement();
        _suchErgebnisListe.style.display           = DisplayStyle.None;
        _suchErgebnisListe.style.backgroundColor   = new Color(45f / 255f, 45f / 255f, 45f / 255f);
        _suchErgebnisListe.style.borderTopWidth    = 1;
        _suchErgebnisListe.style.borderRightWidth  = 1;
        _suchErgebnisListe.style.borderBottomWidth = 1;
        _suchErgebnisListe.style.borderLeftWidth   = 1;
        _suchErgebnisListe.style.borderTopColor    = Gruen;
        _suchErgebnisListe.style.borderRightColor  = Gruen;
        _suchErgebnisListe.style.borderBottomColor = Gruen;
        _suchErgebnisListe.style.borderLeftColor   = Gruen;
        _suchErgebnisListe.style.borderTopLeftRadius     = 6;
        _suchErgebnisListe.style.borderTopRightRadius    = 6;
        _suchErgebnisListe.style.borderBottomLeftRadius  = 6;
        _suchErgebnisListe.style.borderBottomRightRadius = 6;
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
        catch (Exception e) { Debug.LogWarning("[" + BelegTyp + "] Kundensuche: " + e.Message); return; }

        string suche   = suchtext.Trim().ToLowerInvariant();
        var    treffer = kunden.Where(k => KundenAnzeige(k).ToLowerInvariant().Contains(suche))
                               .Take(6).ToList();

        if (treffer.Count == 0)
        {
            var hinweis = new Label("Kein Kunde gefunden");
            hinweis.style.color       = new Color(0.6f, 0.6f, 0.6f);
            hinweis.style.fontSize    = 12;
            hinweis.style.paddingLeft = 10;
            hinweis.style.paddingTop  = 6;
            hinweis.style.paddingBottom = 6;
            _suchErgebnisListe.Add(hinweis);
        }
        else
        {
            foreach (var kunde in treffer)
            {
                var aktuellerKunde = kunde;
                var eintrag = new Label(KundenAnzeige(aktuellerKunde));
                eintrag.style.color         = Color.white;
                eintrag.style.fontSize      = 13;
                eintrag.style.paddingLeft   = 10;
                eintrag.style.paddingRight  = 10;
                eintrag.style.paddingTop    = 6;
                eintrag.style.paddingBottom = 6;
                eintrag.RegisterCallback<MouseEnterEvent>(_ =>
                    eintrag.style.backgroundColor = new Color(Gruen.r, Gruen.g, Gruen.b, 0.2f));
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
        _ausgewaehlterKunde = KundenAnzeige(kunde);
        _kundensuche.SetValueWithoutNotify(_ausgewaehlterKunde);
        _suchErgebnisListe.style.display = DisplayStyle.None;

        var boxen = Root.Query(className: "angebot-address-box").ToList();
        if (boxen.Count > 0) SetzeAdresse(boxen[0], "Kunde:", KundenAdresse(kunde));
    }

    // ─────────────────────────────────────────
    // ANHÄNGE
    // ─────────────────────────────────────────

    private void RegistriereAnhaenge()
    {
        _anhangBereich = Root.Q<VisualElement>(AnhangKarteName);
        if (_anhangBereich == null) return;

        _anhangBereich.Clear();
        _anhangAusgewaehlt.Clear();

        var ueberschrift = new Label("Anhänge");
        ueberschrift.style.fontSize = 13;
        ueberschrift.style.unityFontStyleAndWeight = FontStyle.Bold;
        ueberschrift.style.color        = Color.white;
        ueberschrift.style.marginBottom = 6;
        _anhangBereich.Add(ueberschrift);

        var verfuegbar = BelegAnhangController.HoleVerfuegbareAnhaenge();

        foreach (string key in BelegAnhangController.AnhangSchluessel)
        {
            bool vorhanden = verfuegbar.ContainsKey(key) && verfuegbar[key];
            _anhangAusgewaehlt[key] = false;

            string lokalerKey = key;

            var zeile = new VisualElement();
            zeile.style.flexDirection      = FlexDirection.Row;
            zeile.style.alignItems         = Align.Center;
            zeile.style.marginBottom       = 4;
            zeile.style.paddingTop         = 4;
            zeile.style.paddingBottom      = 4;
            zeile.style.paddingLeft        = 8;
            zeile.style.paddingRight       = 8;
            zeile.style.borderTopLeftRadius     = 6;
            zeile.style.borderTopRightRadius    = 6;
            zeile.style.borderBottomLeftRadius  = 6;
            zeile.style.borderBottomRightRadius = 6;
            zeile.style.backgroundColor = vorhanden
                ? new Color(55f / 255f, 55f / 255f, 55f / 255f)
                : new Color(40f / 255f, 40f / 255f, 40f / 255f);

            var box = new VisualElement();
            box.style.width  = 18;
            box.style.height = 18;
            box.style.flexShrink  = 0;
            box.style.marginRight = 8;
            box.style.borderTopLeftRadius     = 4;
            box.style.borderTopRightRadius    = 4;
            box.style.borderBottomLeftRadius  = 4;
            box.style.borderBottomRightRadius = 4;
            box.style.borderTopWidth    = 1;
            box.style.borderRightWidth  = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth   = 1;
            box.style.alignItems     = Align.Center;
            box.style.justifyContent = Justify.Center;

            var haken = new Label("");
            haken.style.fontSize       = 11;
            haken.style.unityTextAlign = TextAnchor.MiddleCenter;
            box.Add(haken);

            AktualisiereCheckboxOptik(box, haken, false, vorhanden);

            var label = new Label(key);
            label.style.fontSize = 12;
            label.style.color    = vorhanden ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            label.style.flexGrow = 1;

            zeile.Add(box);
            zeile.Add(label);

            if (!vorhanden)
            {
                var fehlt = new Label("(nicht im Pool)");
                fehlt.style.fontSize   = 10;
                fehlt.style.color      = new Color(0.4f, 0.4f, 0.4f);
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
                        zeile.style.backgroundColor = new Color(65f / 255f, 65f / 255f, 65f / 255f);
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
            box.style.borderTopColor    = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderRightColor  = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            box.style.borderLeftColor   = new Color(0.3f, 0.3f, 0.3f);
            haken.text        = "";
            haken.style.color = Color.clear;
            return;
        }

        if (ausgewaehlt)
        {
            box.style.backgroundColor = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            box.style.borderTopColor    = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            box.style.borderRightColor  = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            box.style.borderBottomColor = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            box.style.borderLeftColor   = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            haken.text        = "\u2713";
            haken.style.color = new Color(38f / 255f, 38f / 255f, 38f / 255f);
        }
        else
        {
            box.style.backgroundColor = new Color(70f / 255f, 70f / 255f, 70f / 255f);
            box.style.borderTopColor    = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderRightColor  = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderBottomColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            box.style.borderLeftColor   = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            haken.text        = "";
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

    // ─────────────────────────────────────────
    // SPEICHERN / STATUS
    // ─────────────────────────────────────────

    private void SpeichernGeklickt()
    {
        FeedbackPopup.Show(Root, "Einträge gespeichert", FeedbackTyp.Erfolg);
    }

    private void StatusGeklickt(bool angenommen)
    {
        string neuerStatus = angenommen ? "Angenommen" : "Abgelehnt";
        _statusDropdown?.SetValueWithoutNotify(neuerStatus);

        if (angenommen) UebernimmInsKassenbuch();

        FeedbackPopup.Show(Root,
            angenommen ? BelegTyp + " bestätigt" : BelegTyp + " abgelehnt",
            angenommen ? FeedbackTyp.Erfolg : FeedbackTyp.Fehler);
    }

    private void UebernimmInsKassenbuch()
    {
        try
        {
            var    db     = UserDatabaseAccess.getCurrentUserDatabase();
            float  gesamt = ParseBetrag(_gesamtLabel != null ? _gesamtLabel.text : "0");
            string nummer = _nummerFeld != null ? _nummerFeld.value : "";
            string kunde  = !string.IsNullOrEmpty(_ausgewaehlterKunde)
                            ? " - " + _ausgewaehlterKunde : "";
            string datum  = _datumFeld != null && !string.IsNullOrWhiteSpace(_datumFeld.value)
                            ? _datumFeld.value
                            : DateTime.Now.ToString("dd.MM.yyyy");

            db.createEinkommen(gesamt, BelegTyp + " " + nummer + kunde, datum);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kassenbuch: " + e.Message);
        }
    }

    // ─────────────────────────────────────────
    // EINGABE-FILTER
    // ─────────────────────────────────────────

    private static void NurGanzeZahlen(TextField feld)
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
                formatiert = nurZiffern.Substring(0, 2) + "." + nurZiffern.Substring(2, 2) + "." + nurZiffern.Substring(4);
            else if (nurZiffern.Length > 2)
                formatiert = nurZiffern.Substring(0, 2) + "." + nurZiffern.Substring(2);

            if (formatiert == neu) return;

            int cursorVorher  = feld.cursorIndex;
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

    // ─────────────────────────────────────────
    // HILFS-METHODEN
    // ─────────────────────────────────────────

    private TextField NeuesTextFeld()
    {
        var feld = new TextField();
        feld.AddToClassList("angebot-input");
        feld.style.marginRight = 8;

        var eingabe = feld.Q(className: "unity-base-field__input");
        if (eingabe != null)
        {
            eingabe.style.backgroundColor         = FeldFarbe;
            eingabe.style.color                   = Color.white;
            eingabe.style.borderTopWidth          = 0;
            eingabe.style.borderRightWidth        = 0;
            eingabe.style.borderBottomWidth       = 0;
            eingabe.style.borderLeftWidth         = 0;
            eingabe.style.borderTopLeftRadius     = 6;
            eingabe.style.borderTopRightRadius    = 6;
            eingabe.style.borderBottomLeftRadius  = 6;
            eingabe.style.borderBottomRightRadius = 6;
            eingabe.style.paddingLeft             = 8;
        }
        return feld;
    }

    protected float ParseBetrag(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0f;
        string bereinigt = text.Replace("EUR", "").Replace("\u20AC", "").Trim();
        if (float.TryParse(bereinigt, NumberStyles.Float, De, out float wert)) return wert;
        if (float.TryParse(bereinigt, NumberStyles.Float, CultureInfo.InvariantCulture, out wert)) return wert;
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
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

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

    private string KundenAnzeige(object kunde)
    {
        string vorname  = LiesFeld(kunde, "vorname", "firstName");
        string nachname = LiesFeld(kunde, "nachname", "lastName", "name", "firma");
        string anzeige  = (vorname + " " + nachname).Trim();
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
}