using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class BelegScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    protected const string PositionenListeName = "positionen-liste";
    protected const string KundensucheName = "input-kundensuche";
    protected const string AbsenderLabelName = "label-absender";
    protected const string NummerFeldName = "input-nummer";
    protected const string StatusDropdownName = "dropdown-status";
    protected const string DatumFeldName = "input-datum";
    protected const string WaehrungDropdownName = "dropdown-waehrung";
    protected const string FristFeldName = "input-frist";
    protected const string RabattTypDropdownName = "dropdown-rabatt-typ";
    protected const string RabattWertFeldName = "input-rabatt-wert";
    protected const string SonstigeFeldName = "input-sonstige-kosten";
    protected const string NettoLabelName = "label-netto";
    protected const string RabattLabelName = "label-rabatt";
    protected const string SonstigeLabelName = "label-sonstige";
    protected const string GesamtLabelName = "label-gesamt-total";
    protected const string SpeichernButtonName = "btn-speichern";
    protected const string AngenommenButtonName = "btn-angenommen";
    protected const string AbgelehntButtonName = "btn-abgelehnt";
    protected const string PositionAddButtonName = "btn-position-hinzufuegen";

    protected static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color FeldFarbe = new Color(70f / 255f, 70f / 255f, 70f / 255f);

    protected VisualElement Root;

    private ScrollView _positionenListe;
    private readonly List<PositionsZeile> _zeilen = new List<PositionsZeile>();
    private Label _nettoLabel, _rabattLabel, _sonstigeLabel, _gesamtLabel;
    private TextField _kundensuche, _nummerFeld, _datumFeld, _fristFeld, _rabattWertFeld, _sonstigeFeld;
    private DropdownField _statusDropdown, _waehrungDropdown, _rabattTypDropdown;
    private VisualElement _suchErgebnisListe;
    private string _ausgewaehlterKunde = "";

    protected abstract string BelegTyp { get; }
    protected abstract string NummernPrefix { get; }
    protected abstract List<string> StatusOptionen { get; }
    protected virtual void RegistriereZusatzButtons() { }

    private class PositionsZeile
    {
        public VisualElement Wurzel;
        public TextField Artikel, Beschreibung, Menge, Preis;
        public DropdownField Einheit;
        public Label Gesamt;
    }

    private void OnEnable()
{
    if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    Root = uiDocument.rootVisualElement;

    SammleElemente();
    LeereDemoInhalte();
    SetzeStandardwerte();
    LadeAbsenderdaten();
    RegistriereSummenEingaben();
    RegistriereButtons();
    RegistriereKundensuche();
    FuegePositionHinzu();
    BerechneSummen();
}

    private void SammleElemente()
    {
        _positionenListe = Root.Q<ScrollView>(PositionenListeName) ?? Root.Q<ScrollView>();
        if (_positionenListe == null)
            Debug.LogWarning("[" + BelegTyp + "] Keine ScrollView für Positionen gefunden.");
        else
            _positionenListe.verticalScrollerVisibility = ScrollerVisibility.Auto;

        _nettoLabel = Root.Q<Label>(NettoLabelName);
        _rabattLabel = Root.Q<Label>(RabattLabelName);
        _sonstigeLabel = Root.Q<Label>(SonstigeLabelName);
        _gesamtLabel = Root.Q<Label>(GesamtLabelName);

        _kundensuche = Root.Q<TextField>(KundensucheName);
        _nummerFeld = Root.Q<TextField>(NummerFeldName);
        _datumFeld = Root.Q<TextField>(DatumFeldName);
        _fristFeld = Root.Q<TextField>(FristFeldName);

        _statusDropdown = Root.Q<DropdownField>(StatusDropdownName);
        _waehrungDropdown = Root.Q<DropdownField>(WaehrungDropdownName);
        _rabattTypDropdown = Root.Q<DropdownField>(RabattTypDropdownName);
    }

    private void LeereDemoInhalte()
    {
        _positionenListe?.Clear();
        Root.Query<TextField>().ForEach(feld => feld.SetValueWithoutNotify(""));

        var boxen = Root.Query(className: "angebot-address-box").ToList();
        if (boxen.Count > 0) SetzeAdresse(boxen[0], "Rechnung für", "Noch kein Kunde ausgewählt");
        if (boxen.Count > 1) SetzeAdresse(boxen[1], "Versenden an", "Noch kein Kunde ausgewählt");
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
        text.style.color = new Color(0.78f, 0.78f, 0.78f);
        text.style.whiteSpace = WhiteSpace.Normal;
        box.Add(text);
    }

    private void SetzeStandardwerte()
    {
        _datumFeld?.SetValueWithoutNotify(DateTime.Now.ToString("dd.MM.yyyy"));
        _fristFeld?.SetValueWithoutNotify(DateTime.Now.AddDays(14).ToString("dd.MM.yyyy"));
        _nummerFeld?.SetValueWithoutNotify(string.Format("{0}-{1:yyyyMMdd-HHmm}", NummernPrefix, DateTime.Now));

        NurDatumzeichen(_datumFeld);
        NurDatumzeichen(_fristFeld);

        if (_statusDropdown != null)
        {
            _statusDropdown.choices = StatusOptionen;
            _statusDropdown.SetValueWithoutNotify(StatusOptionen[0]);
        }
        if (_waehrungDropdown != null)
        {
            _waehrungDropdown.choices = new List<string> { "EUR", "USD", "CHF" };
            _waehrungDropdown.SetValueWithoutNotify("EUR");
        }
        if (_rabattTypDropdown != null)
        {
            _rabattTypDropdown.choices = new List<string> { "Kein Rabatt", "Prozent", "Festbetrag" };
            _rabattTypDropdown.SetValueWithoutNotify("Kein Rabatt");
            _rabattTypDropdown.RegisterValueChangedCallback(_ => BerechneSummen());
        }
    }

    private void LadeAbsenderdaten()
    {
        var absenderLabel = Root.Q<Label>(AbsenderLabelName);
        if (absenderLabel == null) return;

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var firmen = db.getAllCompanies();
            if (firmen != null && firmen.Count > 0)
            {
                var firma = firmen[0];
                string name = LiesFeld(firma, "name");
                string ort = LiesFeld(firma, "location", "ort", "adresse");
                absenderLabel.text = string.IsNullOrEmpty(ort) ? name : name + "\n" + ort;
            }
            else
            {
                absenderLabel.text = "Keine Firmendaten vorhanden.\nBitte in den Einstellungen hinterlegen.";
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Absenderdaten konnten nicht geladen werden: " + e.Message);
        }
    }

    private void RegistriereSummenEingaben()
    {
        _rabattWertFeld = Root.Q<TextField>(RabattWertFeldName) ?? ErstelleSummenFeld("Rabatt (Wert)", RabattWertFeldName);
        _sonstigeFeld = Root.Q<TextField>(SonstigeFeldName) ?? ErstelleSummenFeld("Sonstige Kosten", SonstigeFeldName);

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

    private void RegistriereButtons()
    {
        FindeButton(SpeichernButtonName, "Speichern")?.RegisterCallback<ClickEvent>(_ => SpeichernGeklickt());
        FindeButton(AngenommenButtonName, "Angenommen")?.RegisterCallback<ClickEvent>(_ => StatusGeklickt(true));
        FindeButton(AbgelehntButtonName, "Abgelehnt")?.RegisterCallback<ClickEvent>(_ => StatusGeklickt(false));

        var hinzufuegen = FindeButton(PositionAddButtonName, "+ Position");
        if (hinzufuegen == null) hinzufuegen = ErstellePositionHinzufuegenButton();
        hinzufuegen?.RegisterCallback<ClickEvent>(_ => FuegePositionHinzu());

        RegistriereZusatzButtons();
    }

    protected Button FindeButton(string elementName, string buttonText)
    {
        var button = Root.Q<Button>(elementName);
        if (button != null) return button;
        return Root.Query<Button>().ToList()
                   .FirstOrDefault(b => b.text != null && b.text.Trim().StartsWith(buttonText));
    }

    private Button ErstellePositionHinzufuegenButton()
    {
        if (_positionenListe == null) return null;

        var button = new Button { text = "+ Position hinzufügen" };
        button.style.alignSelf = Align.FlexStart;
        button.style.marginTop = 8;
        button.style.backgroundColor = Color.clear;
        button.style.color = Gruen;
        button.style.fontSize = 13;
        button.style.borderTopWidth = 1;
        button.style.borderRightWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderTopColor = Gruen;
        button.style.borderRightColor = Gruen;
        button.style.borderBottomColor = Gruen;
        button.style.borderLeftColor = Gruen;
        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
        button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new Color(Gruen.r, Gruen.g, Gruen.b, 0.15f));
        button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = Color.clear);

        var eltern = _positionenListe.parent;
        eltern.Insert(eltern.IndexOf(_positionenListe) + 1, button);
        return button;
    }

    private void FuegePositionHinzu()
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
        punkt.style.width = 10;
        punkt.style.height = 10;
        punkt.style.borderTopLeftRadius = 5;
        punkt.style.borderTopRightRadius = 5;
        punkt.style.borderBottomLeftRadius = 5;
        punkt.style.borderBottomRightRadius = 5;
        punkt.style.backgroundColor = Gruen;
        punkt.style.flexShrink = 0;
        punkt.style.marginRight = 8;

        zeile.Artikel = NeuesTextFeld();
        zeile.Artikel.style.flexGrow = 1;
        zeile.Artikel.style.minWidth = 120;

        zeile.Beschreibung = NeuesTextFeld();
        zeile.Beschreibung.AddToClassList("angebot-col-beschreibung");
        zeile.Beschreibung.style.width = 220;

        zeile.Menge = NeuesTextFeld();
        zeile.Menge.AddToClassList("angebot-col-menge");
        zeile.Menge.style.width = 70;
        zeile.Menge.SetValueWithoutNotify("1");
        NurGanzeZahlen(zeile.Menge);

        zeile.Einheit = NeuesDropdown(new List<string> { "Stück", "Stunde", "Tag", "Pauschal" });
        zeile.Einheit.AddToClassList("angebot-col-einheit");
        zeile.Einheit.style.width = 110;

        zeile.Preis = NeuesTextFeld();
        zeile.Preis.AddToClassList("angebot-col-preis");
        zeile.Preis.style.width = 110;
        zeile.Preis.SetValueWithoutNotify("0,00");
        NurZahlenmitKomma(zeile.Preis);

        zeile.Gesamt = new Label("0,00 EUR");
        zeile.Gesamt.AddToClassList("angebot-col-gesamt");
        zeile.Gesamt.style.width = 110;
        zeile.Gesamt.style.fontSize = 13;
        zeile.Gesamt.style.color = new Color(0.86f, 0.86f, 0.86f);
        zeile.Gesamt.style.unityTextAlign = TextAnchor.MiddleRight;

        var loeschen = new Button { text = "\u2715" };
        loeschen.style.width = 26;
        loeschen.style.height = 26;
        loeschen.style.marginLeft = 8;
        loeschen.style.backgroundColor = Color.clear;
        loeschen.style.color = new Color(0.6f, 0.6f, 0.6f);
        loeschen.style.fontSize = 12;
        loeschen.style.borderTopWidth = 0;
        loeschen.style.borderRightWidth = 0;
        loeschen.style.borderBottomWidth = 0;
        loeschen.style.borderLeftWidth = 0;
        loeschen.RegisterCallback<MouseEnterEvent>(_ => loeschen.style.color = new Color(230f / 255f, 57f / 255f, 70f / 255f));
        loeschen.RegisterCallback<MouseLeaveEvent>(_ => loeschen.style.color = new Color(0.6f, 0.6f, 0.6f));
        loeschen.RegisterCallback<ClickEvent>(_ =>
        {
            wurzel.RemoveFromHierarchy();
            _zeilen.Remove(zeile);
            BerechneSummen();
        });

        zeile.Menge.RegisterValueChangedCallback(_ => BerechneSummen());
        zeile.Preis.RegisterValueChangedCallback(_ => BerechneSummen());

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
    }

    private TextField NeuesTextFeld()
    {
        var feld = new TextField();
        feld.AddToClassList("angebot-input");
        feld.style.marginRight = 8;

        var eingabe = feld.Q(className: "unity-base-field__input");
        if (eingabe != null)
        {
            eingabe.style.backgroundColor = FeldFarbe;
            eingabe.style.color = Color.white;
            eingabe.style.borderTopWidth = 0;
            eingabe.style.borderRightWidth = 0;
            eingabe.style.borderBottomWidth = 0;
            eingabe.style.borderLeftWidth = 0;
            eingabe.style.borderTopLeftRadius = 6;
            eingabe.style.borderTopRightRadius = 6;
            eingabe.style.borderBottomLeftRadius = 6;
            eingabe.style.borderBottomRightRadius = 6;
            eingabe.style.paddingLeft = 8;
        }
        return feld;
    }

    private DropdownField NeuesDropdown(List<string> optionen)
    {
        var feld = new DropdownField { choices = optionen };
        feld.AddToClassList("angebot-dropdown");
        feld.SetValueWithoutNotify(optionen[0]);
        feld.style.marginRight = 8;

        var eingabe = feld.Q(className: "unity-base-popup-field__input");
        if (eingabe != null)
        {
            eingabe.style.backgroundColor = FeldFarbe;
            eingabe.style.color = Color.white;
            eingabe.style.borderTopWidth = 0;
            eingabe.style.borderRightWidth = 0;
            eingabe.style.borderBottomWidth = 0;
            eingabe.style.borderLeftWidth = 0;
            eingabe.style.borderTopLeftRadius = 6;
            eingabe.style.borderTopRightRadius = 6;
            eingabe.style.borderBottomLeftRadius = 6;
            eingabe.style.borderBottomRightRadius = 6;
        }
        return feld;
    }

    protected void BerechneSummen()
    {
        float netto = 0f;
        foreach (var zeile in _zeilen)
        {
            float gesamt = ParseBetrag(zeile.Menge.value) * ParseBetrag(zeile.Preis.value);
            zeile.Gesamt.text = FormatBetrag(gesamt);
            netto += gesamt;
        }

        float rabattWert = ParseBetrag(_rabattWertFeld != null ? _rabattWertFeld.value : "0");
        string typ = _rabattTypDropdown != null ? _rabattTypDropdown.value : "Kein Rabatt";
        float rabatt = 0f;
        if (typ == "Prozent") rabatt = netto * rabattWert / 100f;
        else if (typ == "Festbetrag") rabatt = rabattWert;

        float sonstige = ParseBetrag(_sonstigeFeld != null ? _sonstigeFeld.value : "0");
        float gesamtSumme = netto - rabatt + sonstige;

        if (_nettoLabel != null) _nettoLabel.text = FormatBetrag(netto);
        if (_rabattLabel != null) _rabattLabel.text = FormatBetrag(rabatt);
        if (_sonstigeLabel != null) _sonstigeLabel.text = FormatBetrag(sonstige);
        if (_gesamtLabel != null) _gesamtLabel.text = FormatBetrag(gesamtSumme);
    }

    private void RegistriereKundensuche()
    {
        if (_kundensuche == null)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kundensuchfeld '" + KundensucheName + "' nicht gefunden.");
            return;
        }

        _suchErgebnisListe = new VisualElement();
        _suchErgebnisListe.style.display = DisplayStyle.None;
        _suchErgebnisListe.style.backgroundColor = new Color(45f / 255f, 45f / 255f, 45f / 255f);
        _suchErgebnisListe.style.borderTopWidth = 1;
        _suchErgebnisListe.style.borderRightWidth = 1;
        _suchErgebnisListe.style.borderBottomWidth = 1;
        _suchErgebnisListe.style.borderLeftWidth = 1;
        _suchErgebnisListe.style.borderTopColor = Gruen;
        _suchErgebnisListe.style.borderRightColor = Gruen;
        _suchErgebnisListe.style.borderBottomColor = Gruen;
        _suchErgebnisListe.style.borderLeftColor = Gruen;
        _suchErgebnisListe.style.borderTopLeftRadius = 6;
        _suchErgebnisListe.style.borderTopRightRadius = 6;
        _suchErgebnisListe.style.borderBottomLeftRadius = 6;
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
        try
        {
            kunden = UserDatabaseAccess.getCurrentUserDatabase().getAllCustomers();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kundensuche fehlgeschlagen: " + e.Message);
            return;
        }

        string suche = suchtext.Trim().ToLowerInvariant();
        var treffer = kunden.Where(k => KundenAnzeige(k).ToLowerInvariant().Contains(suche)).Take(6).ToList();

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
                eintrag.RegisterCallback<MouseEnterEvent>(_ => eintrag.style.backgroundColor = new Color(Gruen.r, Gruen.g, Gruen.b, 0.2f));
                eintrag.RegisterCallback<MouseLeaveEvent>(_ => eintrag.style.backgroundColor = Color.clear);
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

        string adresse = KundenAdresse(kunde);
        var boxen = Root.Query(className: "angebot-address-box").ToList();
        if (boxen.Count > 0) SetzeAdresse(boxen[0], "Rechnung für", adresse);
        if (boxen.Count > 1) SetzeAdresse(boxen[1], "Versenden an", adresse);
    }

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
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            float gesamt = ParseBetrag(_gesamtLabel != null ? _gesamtLabel.text : "0");
            string nummer = _nummerFeld != null ? _nummerFeld.value : "";
            string kunde = !string.IsNullOrEmpty(_ausgewaehlterKunde) ? " - " + _ausgewaehlterKunde : "";
            string datum = _datumFeld != null && !string.IsNullOrWhiteSpace(_datumFeld.value)
                ? _datumFeld.value
                : DateTime.Now.ToString("dd.MM.yyyy");

            db.createEinkommen(gesamt, BelegTyp + " " + nummer + kunde, datum);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[" + BelegTyp + "] Kassenbuch-Eintrag fehlgeschlagen: " + e.Message);
        }
    }

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
            if (!erlaubt)
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
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
            if (!erlaubt)
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
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
        if (!erlaubt)
        {
            evt.StopPropagation();
            evt.PreventDefault();
        }
    }, TrickleDown.TrickleDown);

    feld.RegisterValueChangedCallback(evt =>
{
    string neu = evt.newValue ?? "";
    string alt = evt.previousValue ?? "";

    // Beim Löschen nicht eingreifen
    if (neu.Length <= alt.Length) return;

    // Nur Ziffern zählen
    string nurZiffern = "";
    foreach (char c in neu)
        if (char.IsDigit(c)) nurZiffern += c;

    // Hart auf 8 Ziffern begrenzen
    if (nurZiffern.Length > 8)
        nurZiffern = nurZiffern.Substring(0, 8);

    // Punkt automatisch nach 2. und 4. Ziffer einfügen
    string formatiert = nurZiffern;
    if (nurZiffern.Length > 4)
        formatiert = nurZiffern.Substring(0, 2) + "." + nurZiffern.Substring(2, 2) + "." + nurZiffern.Substring(4);
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
}