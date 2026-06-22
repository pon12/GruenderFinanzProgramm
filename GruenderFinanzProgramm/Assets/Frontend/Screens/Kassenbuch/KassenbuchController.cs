using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;
using System.Collections.Generic;
using iTextSharp.text; // Für den PDF-Export benötigt
using System.Linq;
using System.Globalization;


public class KassenbuchController : MonoBehaviour
{
    private VisualElement _overlay;
    private DropdownField dropJahrField;
    private VisualElement tableInput; // Repräsentiert deinen 'tableBody'
    private VisualTreeAsset outputTemplate;


    private Label balanceLabel;
    private Label letzteAktualLabel;

    private Label fehlerLabel;
   
    private VisualElement datumKlickLabel;          
    private VisualElement meinUxmlKalender;


    // --- DASHBOARD STATISTIK LABELS ---
    private Label labelKundenAnzahl;
    private Label labelAngeboteAnzahl;
    private Label labelRechnungenAnzahl;


    // --- DASHBOARD BUTTONS ---
    private Button btnAngebotErstellen;
    private Button btnRechnungErstellen;
    private Button btnFinanzenDashboard;


    private int _currentYear;
    private int _currentMonth;
    private readonly string[] _monthNames =
    {
        "Januar","Februar","März","April","Mai","Juni",
        "Juli","August","September","Oktober","November","Dezember"
    };


    private string _aktuellerTyp;
    private DataBase db;

    private DropdownField artDropdown;


    // ==========================================
    // UNSERE PLATZHALTER-TEXTE
    // ==========================================
    private const string PLACEHOLDER_BETRAG = "0,00";
    private const string PLACEHOLDER_ZWECK = "z. B. Einkauf, Tanken, Gehalt...";


    void OnEnable()
    {

        
        // Holt die aktive Nutzer-Datenbank laut Dokumentation
        db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            Debug.LogError("[Kassenbuch] Keine aktive Nutzer-Datenbank gefunden.");
            return;
        }

        db.setupKassenbuchTable();

        outputTemplate = Resources.Load<VisualTreeAsset>("Kassenbuch_Field");


        var root = GetComponent<UIDocument>().rootVisualElement;


        _overlay = root.Q<VisualElement>("popup-overlay");
       
        // Greift sich das Element "tableBody" für deine Dokumente/Einträge
        tableInput = root.Q<VisualElement>("tableBody") ?? root.Q<VisualElement>("unity-content-container");
       
        balanceLabel = root.Q<Label>("balanceLabel") ?? root.Q<Label>("label-kontostand");
        letzteAktualLabel = root.Q<Label>("letzteAktual");

        if (_overlay != null)
{
    fehlerLabel = _overlay.Q<Label>("FehlerText");

    if (fehlerLabel != null)
    {
        fehlerLabel.text = "";
        fehlerLabel.style.display = DisplayStyle.None;
        fehlerLabel.style.color = UnityEngine.Color.red;
    }
}


        // --- DASHBOARD LABELS ZUWEISEN ---
        labelKundenAnzahl = root.Q<Label>("label-kunden-anzahl") ?? root.Q<Label>("KundenStat");
        labelAngeboteAnzahl = root.Q<Label>("label-angebote-anzahl") ?? root.Q<Label>("AngeboteStat");
        labelRechnungenAnzahl = root.Q<Label>("label-rechnungen-anzahl") ?? root.Q<Label>("RechnungenStat");


        // --- DASHBOARD BUTTONS ZUWEISEN ---
        btnAngebotErstellen = root.Q<Button>("btn-angebot-neu") ?? root.Q<Button>("Angebot");
        btnRechnungErstellen = root.Q<Button>("btn-rechnung-neu") ?? root.Q<Button>("Rechnung");
        btnFinanzenDashboard = root.Q<Button>("btn-finanzen") ?? root.Q<Button>("Finanzen");


        datumKlickLabel = root.Q("label-datum") ?? root.Q("input-datum");
        meinUxmlKalender = root.Q("KalenderPopUp");


        // ==========================================
        // INITIALISIERUNG DER PLATZHALTER
        // ==========================================
        if (_overlay != null)
        {
            var inputBetrag = _overlay.Q<TextField>("input-betrag");
            SetupBetragInputAnpassung(inputBetrag, PLACEHOLDER_BETRAG);


            var inputZweck = _overlay.Q<TextField>("input-verwendungzweck");
            SetupPlaceholderSimulation(inputZweck, PLACEHOLDER_ZWECK);
        }


        var today     = DateTime.Today;
        _currentYear  = today.Year;
        _currentMonth = today.Month;


        SetupDashboardCalendar(root);


        // ==========================================
        // INITIALISIERUNG DES EXPORT-DROPDOWNS (100 JAHRE)
        // ==========================================
        dropJahrField = root.Q<DropdownField>("dropJahr");
        if (dropJahrField != null)
        {
            List<string> jahre = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                jahre.Add((today.Year - i).ToString());
            }
            dropJahrField.choices = jahre;
            dropJahrField.value = today.Year.ToString();

            // FIX: Ohne diesen Callback passierte beim Jahreswechsel
            // im Dropdown nichts – Tabelle und Kontostand blieben statisch.
            dropJahrField.RegisterValueChangedCallback(_ => createList());
        }


        if (meinUxmlKalender != null)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }


        if (datumKlickLabel != null)
        {
            datumKlickLabel.pickingMode = PickingMode.Position;
            datumKlickLabel.RegisterCallback<ClickEvent>(OnDatumLabelClicked);
        }


        if (balanceLabel != null)
        {
            balanceLabel.RegisterCallback<ClickEvent>(OnBalanceLabelClicked);
        }


        if (_overlay != null)
        {
            _overlay.RemoveFromHierarchy();
            root.Add(_overlay);
            _overlay.style.display = DisplayStyle.None;
        }


        var btnAusgaben = root.Q<Button>("btnAusgaben");
        if (btnAusgaben != null) btnAusgaben.clicked += () => OpenPopup("Ausgabe");


        var btnEinnahmen = root.Q<Button>("btnEinnahmen");
        if (btnEinnahmen != null) btnEinnahmen.clicked += () => OpenPopup("Einnahme");


        var btnSpeichern = root.Q<Button>("btn-speichern");
        if (btnSpeichern != null) btnSpeichern.clicked += OnSpeichern;


        var btnAbbrechen = root.Q<Button>("btn-abbrechen");
        if (btnAbbrechen != null) btnAbbrechen.clicked += ClosePopup;


        // --- EVENT-VERKNÜPFUNGEN FÜR DASHBOARD BUTTONS ---
        if (btnAngebotErstellen != null) btnAngebotErstellen.clicked += () => Debug.Log("[Dashboard] Erstelle neues Angebot...");
        if (btnRechnungErstellen != null) btnRechnungErstellen.clicked += () => Debug.Log("[Dashboard] Erstelle neue Rechnung...");
        if (btnFinanzenDashboard != null) btnFinanzenDashboard.clicked += () => Debug.Log("[Dashboard] Tabellen-Ansicht Finanzen aktualisiert.");


        // ==========================================
        // EXPORT BUTTON EVENT VERKNÜPFEN
        // ==========================================
        var btnExport = root.Q<Button>("FinanzExport") ?? root.Q<Button>("btn-export") ?? root.Q<Button>("Export");
        Debug.Log($"[DEBUG-Export] Export-Button gefunden: {btnExport != null}");
        if (btnExport != null)
        {
            btnExport.clicked += () =>
            {
                Debug.Log("[DEBUG-Export] Button wurde geklickt!");
                var dj = root.Q<DropdownField>("dropJahr");
                Debug.Log($"[DEBUG-Export] dropJahr gefunden: {dj != null}, Wert: '{dj?.value}'");
                if (dj != null)
                {
                    ExportJahrAlsPdf(dj.value);
                }
            };
        }

        artDropdown = _overlay.Q<DropdownField>("Art");

    if (artDropdown != null)
    {
        artDropdown.choices = new List<string>()
        {
            "Marketing",
            "Reisekosten",
            "Sonstige Kosten",
            "Barentnahme / Privatentnahme",
            "Privateinzahlung",
            "Sonstige Einzahlung",
            "Darlehen",
            "Tilgungsraten",
            "Finanzamt",
            "Steuern",
            "Gehälter"
        };

        artDropdown.value = "Marketing";
    }


        createList();
    }


    void OnDisable()
    {
        if (balanceLabel != null)
            balanceLabel.UnregisterCallback<ClickEvent>(OnBalanceLabelClicked);


        if (datumKlickLabel != null)
            datumKlickLabel.UnregisterCallback<ClickEvent>(OnDatumLabelClicked);
    }


    // ==========================================
    // FEHLERANZEIGE IM POPUP
    //
    // Zeigt 'nachricht' im fehlerLabel an (im Erstell-Popup).
    // Blendet sich nach 3 Sekunden automatisch wieder aus.
    // ==========================================
    private void ShowFehler(string nachricht)
    {
        if (fehlerLabel == null) return;

        fehlerLabel.text = nachricht;
        fehlerLabel.style.display = DisplayStyle.Flex;

        fehlerLabel.schedule.Execute(() =>
        {
            fehlerLabel.style.display = DisplayStyle.None;
        }).ExecuteLater(3000);
    }


    private void SetupBetragInputAnpassung(TextField field, string placeholder)
    {
        if (field == null) return;


        field.maxLength = 50;


        if (string.IsNullOrEmpty(field.value) || field.value == placeholder)
        {
            field.value = placeholder;
        }


       field.RegisterValueChangedCallback(evt =>
{
    string original = evt.newValue;

    // Cursorposition merken
    int cursorPos = field.cursorIndex;

    // Nur Zahlen behalten
    string zahlen = "";
    foreach (char c in original)
    {
        if (char.IsDigit(c))
            zahlen += c;
    }

    if (string.IsNullOrEmpty(zahlen))
    {
        field.SetValueWithoutNotify("");
        return;
    }

    if (long.TryParse(zahlen, out long wert))
    {
        string formatiert = wert.ToString("N0")
            .Replace(",", ".");

        field.SetValueWithoutNotify(formatiert);

        // Cursor ans Ende setzen
        field.schedule.Execute(() =>
        {
            field.cursorIndex = formatiert.Length;
            field.selectIndex = formatiert.Length;
        });
    }
});

        field.RegisterCallback<FocusInEvent>(evt => {
            if (field.value == placeholder)
            {
                field.value = "";
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
            else
            {
                field.value = field.value.Replace("€", "").Trim();
            }
        });


        field.RegisterCallback<FocusOutEvent>(evt => {
            string aktuellerText = field.value.Trim();


            if (string.IsNullOrEmpty(aktuellerText))
            {
                field.value = placeholder;
            }
            else
            {
                if (!aktuellerText.Contains("€"))
                {
                    field.value = aktuellerText + " €";
                }
            }
        });
    }


    private void SetupPlaceholderSimulation(TextField field, string placeholder)
    {
        if (field == null) return;


        if (string.IsNullOrEmpty(field.value) || field.value == placeholder)
        {
            field.value = placeholder;
        }


        field.RegisterCallback<FocusInEvent>(evt => {
            if (field.value == placeholder)
            {
                field.value = "";
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
        });


        field.RegisterCallback<FocusOutEvent>(evt => {
            if (string.IsNullOrEmpty(field.value.Trim()))
            {
                field.value = placeholder;
            }
        });
    }


    private void OnBalanceLabelClicked(ClickEvent evt)
    {
        Debug.Log("Das Kontostand-Label wurde angeklickt!");
    }


    private void OnDatumLabelClicked(ClickEvent evt)
    {
        if (meinUxmlKalender == null) return;


        if (meinUxmlKalender.style.display == DisplayStyle.Flex)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }
        else
        {
            meinUxmlKalender.style.display = DisplayStyle.Flex;
            RenderKalender(GetComponent<UIDocument>().rootVisualElement);
        }
    }


    void SetupDashboardCalendar(VisualElement root)
    {
        var dropMonat = root.Q<DropdownField>("dropdown-monat");
        var dropJahr  = root.Q<DropdownField>("dropdown-jahr");


        if (dropMonat != null)
        {
            dropMonat.choices = new List<string>(_monthNames);
            dropMonat.index   = _currentMonth - 1;
            dropMonat.RegisterValueChangedCallback(_ =>
            {
                _currentMonth = dropMonat.index + 1;
                RenderKalender(root);
            });
        }


        if (dropJahr != null)
        {
            var jahre = new List<string>();
            for (int y = _currentYear - 99; y <= _currentYear; y++)
            {
                jahre.Add(y.ToString());
            }
            jahre.Reverse();


            dropJahr.choices = jahre;
            dropJahr.value   = _currentYear.ToString();
            dropJahr.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int y))
                { _currentYear = y; RenderKalender(root); }
            });
        }


        root.Q<Button>("btn-prev-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(root, -1));
        root.Q<Button>("btn-next-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(root, +1));


        var grid = root.Q<VisualElement>("kalender-grid");
        if (grid != null) grid.style.flexGrow = 1;


        RenderKalender(root);
    }


    void WechsleMonat(VisualElement root, int delta)
    {
        _currentMonth += delta;
        if (_currentMonth < 1)  { _currentMonth = 12; _currentYear--; }
        if (_currentMonth > 12) { _currentMonth = 1;  _currentYear++; }


        var dropMonat = root.Q<DropdownField>("dropdown-monat");
        var dropJahr  = root.Q<DropdownField>("dropdown-jahr");
        if (dropMonat != null) dropMonat.index = _currentMonth - 1;
        if (dropJahr  != null) dropJahr.value  = _currentYear.ToString();


        RenderKalender(root);
    }


    void RenderKalender(VisualElement root)
    {
        var today          = DateTime.Today;
        var ersterTag      = new DateTime(_currentYear, _currentMonth, 1);
        int tageImMonat    = DateTime.DaysInMonth(_currentYear, _currentMonth);
       
        int startWochentag = ((int)ersterTag.DayOfWeek + 6) % 7;


        for (int i = 0; i < 42; i++)
        {
            var btn = root.Q<Button>($"cal-day-{i}");
            if (btn == null) continue;


            btn.RemoveFromClassList("cal-day-today");
            btn.style.backgroundColor = new StyleColor(StyleKeyword.Null);
            btn.style.color = new StyleColor(StyleKeyword.Null);
           
            btn.style.display = DisplayStyle.Flex;


            if (i < startWochentag)
            {
                btn.style.display = DisplayStyle.None;
            }
            else if (i - startWochentag < tageImMonat)
            {
                int tag = i - startWochentag + 1;
                btn.text = tag.ToString();


                if (tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year)
                {
                    btn.AddToClassList("cal-day-today");
                    btn.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.12f, 0.58f, 0.95f, 0.4f));
                    btn.style.color = new StyleColor(UnityEngine.Color.white);
                }


                int clickTag = tag;
                btn.clickable = new Clickable(() => OnTagAusgewaehlt(clickTag, _currentMonth, _currentYear));
            }
            else
            {
                btn.style.display = DisplayStyle.None;
            }
        }
    }


    private void OnTagAusgewaehlt(int tag, int monat, int jahr)
    {
        DateTime ausgewaehltesDatum = new DateTime(jahr, monat, tag);
       
        if (_overlay != null)
        {
            var inputDatum = _overlay.Q<TextField>("input-datum");
            if (inputDatum != null) inputDatum.value = ausgewaehltesDatum.ToString("dd.MM.yyyy");
        }
       
        if (meinUxmlKalender != null)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }
    }


    private void OpenPopup(string typ)
    {
        if (_overlay == null) return;


        _aktuellerTyp = typ;
        _overlay.Q<Label>("popup-title").text = $"{typ} hinzufügen";
        _overlay.Q<TextField>("input-datum").value = DateTime.Now.ToString("dd.MM.yyyy");
       
        var inputBetrag = _overlay.Q<TextField>("input-betrag");
        SetupBetragInputAnpassung(inputBetrag, PLACEHOLDER_BETRAG);


        var inputZweck = _overlay.Q<TextField>("input-verwendungzweck");
        SetupPlaceholderSimulation(inputZweck, PLACEHOLDER_ZWECK);


        if (meinUxmlKalender != null)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }


        _overlay.style.display = DisplayStyle.Flex;
    }

       private void ClosePopup()
    {
        if (_overlay == null) return;


        var inputBetrag = _overlay.Q<TextField>("input-betrag");
        if (inputBetrag != null)
        {
            inputBetrag.value = PLACEHOLDER_BETRAG;
        }


        var inputZweck = _overlay.Q<TextField>("input-verwendungzweck");
        if (inputZweck != null)
        {
            inputZweck.value = PLACEHOLDER_ZWECK;
        }


        _overlay.Q<TextField>("input-datum").value = "";
       
        if (meinUxmlKalender != null)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }


        _overlay.style.display = DisplayStyle.None;
    }

 private void Fehleranzeigen(string text)
    {
        if (fehlerLabel == null)
            return;

        fehlerLabel.text = text;
        fehlerLabel.style.display = DisplayStyle.Flex;
    }

    private void OnSpeichern()
{
    if (fehlerLabel != null)
    {
        fehlerLabel.text = "";
        fehlerLabel.style.display = DisplayStyle.None;
    }

    if (_overlay == null)
        return;

    if (fehlerLabel != null)
    {
        fehlerLabel.text = "";
        fehlerLabel.style.display = DisplayStyle.None;
    }

    string betragText = _overlay.Q<TextField>("input-betrag").value.Trim();
    string zweck = _overlay.Q<TextField>("input-verwendungzweck").value.Trim();
    string datum = _overlay.Q<TextField>("input-datum").value.Trim();

    if (betragText == PLACEHOLDER_BETRAG)
        betragText = "";

    if (zweck == PLACEHOLDER_ZWECK)
        zweck = "";

    betragText = betragText.Replace("€", "").Trim();

    // ==========================
    // BETRAG PRÜFEN
    // ==========================

    if (string.IsNullOrWhiteSpace(betragText))
    {
        Fehleranzeigen("Bitte einen Betrag eingeben.");
        return;
    }

    betragText = betragText.Replace(".", "");
betragText = betragText.Replace("€", "").Trim();

    if (!float.TryParse(
            betragText.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out float betrag))
    {
        ShowFehler("Ungültiger Betrag.");
        return;
    }

    // ==========================
    // ZWECK PRÜFEN
    // ==========================

    if (string.IsNullOrWhiteSpace(zweck))
    {
        Fehleranzeigen("Bitte einen Verwendungszweck eingeben.");
        return;
    }

    // ==========================
    // DATUM PRÜFEN
    // ==========================

    if (string.IsNullOrWhiteSpace(datum))
    {
        ShowFehler("Bitte ein Datum auswählen.");
        return;
    }

    if (!TryParseUndNormalisiereDatum(datum, out string normalisiertesDatum))
    {
        ShowFehler("Ungültiges Datum. Bitte im Format TT.MM.JJJJ eingeben (z.B. 22.01.2026).");
        return;
    }
    datum = normalisiertesDatum;

    // ==========================
    // SPEICHERN
    // ==========================

    var eintrag = new KassenbuchEintrag(
        _aktuellerTyp,
        betrag,
        zweck,
        datum
    );

    Debug.Log("Betrag: '" + betragText + "'");
Debug.Log("Zweck: '" + zweck + "'");

    if (betragText == PLACEHOLDER_BETRAG)
    betragText = "";

    if (zweck == PLACEHOLDER_ZWECK)
    zweck = "";

    // Beide fehlen
    if (string.IsNullOrWhiteSpace(betragText) &&
        string.IsNullOrWhiteSpace(zweck))
    {
        ShowFehler("Bitte Betrag und Verwendungszweck eingeben.");
        return;
    }

    // Nur Betrag fehlt
    if (string.IsNullOrWhiteSpace(betragText))
    {
        ShowFehler("Bitte einen Betrag eingeben.");
        return;
    }

    // Nur Zweck fehlt
    if (string.IsNullOrWhiteSpace(zweck))
    {
        ShowFehler("Bitte einen Verwendungszweck eingeben.");
        return;
    }

    SpeichereEintrag(eintrag);

    ClosePopup();
    createList();
}

// ==========================================
// DATUM-VALIDIERUNG
//
// Prüft ob 'eingabe' ein gültiges Datum ist, und schreibt es
// normalisiert im Format "dd.MM.yyyy" zurück. Verhindert
// Tippfehler wie "22.012.2026" (3-stelliger Monat) die sonst
// unbemerkt in der DB landen und spätere Berechnungen verfälschen.
// ==========================================
private bool TryParseUndNormalisiereDatum(string eingabe, out string normalisiert)
{
    normalisiert = "";

    string[] erlaubteFormate = { "dd.MM.yyyy", "d.M.yyyy", "dd.M.yyyy", "d.MM.yyyy" };
    var inv  = System.Globalization.CultureInfo.InvariantCulture;
    var none = System.Globalization.DateTimeStyles.None;

    if (!DateTime.TryParseExact(eingabe, erlaubteFormate, inv, none, out DateTime ergebnis))
        return false;

    // Zusätzliche Plausibilitäts-Prüfung: Jahr in sinnvollem Bereich
    if (ergebnis.Year < 2000 || ergebnis.Year > 2100)
        return false;

    normalisiert = ergebnis.ToString("dd.MM.yyyy");
    return true;
}


    private void SpeichereEintrag(KassenbuchEintrag eintrag)
    {
        if (db == null) return;


        if (eintrag.Typ == "Einnahme") {
            db.createEinkommen(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);
        }
        else {
            db.createAusgaben(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum);
    }
    }


    public void Loeschen(string typ, int id)
    {
        if (db == null) return;


        if (typ == "Einnahme") db.deleteEinkommen(id);
        else db.deleteAusgaben(id);
        createList();
    }


    public void createList()
    {
        if (db == null || tableInput == null) return;

        // Aktuell gewähltes Jahr aus dem Export/Filter-Dropdown lesen.
        // Falls noch kein Wert gesetzt ist, alle Jahre zeigen (kein Filter).
        bool jahresFilterAktiv = dropJahrField != null && !string.IsNullOrEmpty(dropJahrField.value)
                                  && int.TryParse(dropJahrField.value, out _);
        int gewaehltesJahr = jahresFilterAktiv ? int.Parse(dropJahrField.value) : DateTime.Today.Year;

        // Kontostand: gefiltert auf das gewählte Jahr statt immer die Gesamtdifferenz
        float differenz = jahresFilterAktiv
            ? BerechneKontostandFuerJahr(gewaehltesJahr)
            : db.getDifferenz();


        if (balanceLabel != null)
        {
            // Erhält das Format aus dem 2. Code bei
            balanceLabel.text = differenz.ToString() + "€";
            balanceLabel.style.color = differenz < 0
                ? new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f))   // Rot #E63946
                : new StyleColor(new UnityEngine.Color(128f/255f, 207f/255f, 149f/255f)); // Gruen #80CF95
        }

        // FIX: 'Letzte Aktualisierung' war bisher ein fester Platzhalter
        // ("02.02.2222") und wurde nie aktualisiert. Zeigt jetzt das
        // aktuelle Datum/Uhrzeit, sobald die Liste neu geladen wird.
        if (letzteAktualLabel != null)
        {
            letzteAktualLabel.text = "Letzte Aktualisierung: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }


        // ==========================================
        // APP-EVENT-MANAGER LOGIK FÜR DAS DASHBOARD (Aus Code 2)
        // Bleibt bewusst beim heutigen Jahr, unabhängig vom Kassenbuch-
        // Filter-Dropdown – das Dashboard hat seinen eigenen Jahres-
        // Kalender und soll davon nicht beeinflusst werden.
        // ==========================================
        {
            var heute = System.DateTime.Today;
            float umsatzJahr = 0f;
            float[] monate   = new float[12];


            var einkommenListe = db.getAllEinkommenEntries();
            if (einkommenListe != null)
            {
                foreach (var e in einkommenListe)
                {
                    // Versucht das Datum zu parsen. Je nach dem ob es eine Eigenschaft oder ein Getter ist.
                    if (System.DateTime.TryParse(e.getDatum(), out System.DateTime datum) && datum.Year == heute.Year)
                    {
                        // Falls getAmount() als String zurückgegeben wird, bereinigen wir es, um Fehler zu vermeiden.
                        if (float.TryParse(e.getAmount().Replace("€", "").Trim(), out float amount))
                        {
                            umsatzJahr += amount;
                            monate[datum.Month - 1] += amount;
                        }
                    }
                }
            }
            // Event feuern, damit das Dashboard (Charts etc.) aktualisiert wird
            AppEventManager.KassenbuchGeaendert(umsatzJahr, differenz, monate);
        }


        // Kunden-Einträge zählen (Aus Code 1)
        var kundenListe = db.getAllCustomers();
        if (labelKundenAnzahl != null && kundenListe != null)
        {
            labelKundenAnzahl.text = $"{kundenListe.Count} Kunden hinterlegt";
        }


        if (labelAngeboteAnzahl != null) labelAngeboteAnzahl.text = "0 Angebote";
        if (labelRechnungenAnzahl != null) labelRechnungenAnzahl.text = "0 Rechnungen";


        tableInput.Clear();
        if (outputTemplate == null) { Debug.LogError("outputTemplate is null!"); return; }


        List<Einkommen> einkommenList = db.getAllEinkommenEntries();
        List<Ausgaben>  ausgabenList  = db.getAllAusgabenEntries();

        // Tabelle nach gewähltem Jahr filtern, falls ein gültiges Jahr gewählt ist
        if (jahresFilterAktiv)
        {
            einkommenList = einkommenList?.FindAll(e =>
                TryParseDatum(e.getDatum(), out DateTime d) && d.Year == gewaehltesJahr);
            ausgabenList = ausgabenList?.FindAll(a =>
                TryParseDatum(a.getDatum(), out DateTime d) && d.Year == gewaehltesJahr);
        }


        // Einnahmen
        if (einkommenList != null)
        {
            foreach (Einkommen currentEinkommen in einkommenList)
            {
                VisualElement newEntryCopy = outputTemplate.Instantiate();
               
                Label nameLabel       = newEntryCopy.Q<Label>("Name");
                Label typLabel        = newEntryCopy.Q<Label>("Typ");
                Label betragLabel     = newEntryCopy.Q<Label>("Betrag");
                Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
                Button loeschenBtn    = newEntryCopy.Q<Button>("BtnLoeschen");


                if (nameLabel == null) { Debug.LogError("Label 'Name' nicht gefunden!"); continue; }


                nameLabel.text       = currentEinkommen.getDescription();
                if (float.TryParse(currentEinkommen.getAmount(), out float betrag))
                {
                    betragLabel.text = betrag.ToString("N0").Replace(",", ".") + " €";
                }
                else
                {
                    betragLabel.text = currentEinkommen.getAmount() + " €";
                }
                erstellTagLabel.text = currentEinkommen.getDatum();
                typLabel.text        = "Einkommen";
               
                // Gruen fuer Einnahme
                if (betragLabel != null)
                    betragLabel.style.color = new StyleColor(new UnityEngine.Color(128f/255f, 207f/255f, 149f/255f));


                var progBar = newEntryCopy.Q<VisualElement>("ProgressBarFill");
                if (progBar != null) progBar.style.width = Length.Percent(100);


                // Loeschen Button & Hover (Aus Code 2)
                if (loeschenBtn != null)
                {
                    int id = currentEinkommen.getId();
                    loeschenBtn.clicked += () => Loeschen("Einnahme", id);


                    loeschenBtn.RegisterCallback<MouseEnterEvent>(_ =>
                    {
                        loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f));
                        loeschenBtn.style.color           = new StyleColor(UnityEngine.Color.white);
                    });
                    loeschenBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                    {
                        loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f, 0.15f));
                        loeschenBtn.style.color           = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f));
                    });
                }
                tableInput.Add(newEntryCopy);
            }
        }


        // Ausgaben
        if (ausgabenList != null)
        {
            foreach (Ausgaben currentAusgaben in ausgabenList)
            {
                VisualElement newEntryCopy = outputTemplate.Instantiate();
               
                Label nameLabel       = newEntryCopy.Q<Label>("Name");
                Label typLabel        = newEntryCopy.Q<Label>("Typ");
                Label betragLabel     = newEntryCopy.Q<Label>("Betrag");
                Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
                Button loeschenBtn    = newEntryCopy.Q<Button>("BtnLoeschen");


                if (nameLabel == null) { Debug.LogError("Label 'Name' nicht gefunden!"); continue; }


                nameLabel.text       = currentAusgaben.getDescription();
               if (float.TryParse(currentAusgaben.getAmount(), out float betrag))
                    {
                        betragLabel.text = betrag.ToString("N0").Replace(",", ".") + " €";
                    }
                    else
                    {
                        betragLabel.text = currentAusgaben.getAmount() + " €";
                    }
                erstellTagLabel.text = currentAusgaben.getDatum();
                typLabel.text        = "Ausgabe";
               
                // Rot fuer Ausgabe
                if (betragLabel != null)
                    betragLabel.style.color = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f));


                // Loeschen Button & Hover (Aus Code 2)
                if (loeschenBtn != null)
                {
                    int id = currentAusgaben.getId();
                    loeschenBtn.clicked += () => Loeschen("Ausgabe", id);


                    loeschenBtn.RegisterCallback<MouseEnterEvent>(_ =>
                    {
                        loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f));
                        loeschenBtn.style.color           = new StyleColor(UnityEngine.Color.white);
                    });
                    loeschenBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                    {
                        loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f, 0.15f));
                        loeschenBtn.style.color           = new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f));
                    });
                }
                tableInput.Add(newEntryCopy);
            }
        }
    }


    
    // ===============================
    // Aus altem Code übernommen
    // Robustes Datums-Parsing
    // ===============================
    private bool TryParseDatum(string text, out System.DateTime erg)
    {
        string[] formate =
        {
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd.M.yyyy",
            "d.MM.yyyy",
            "yyyy-MM-dd",
            "yyyy/MM/dd"
        };

        var inv  = System.Globalization.CultureInfo.InvariantCulture;
        var deDe = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        var none = System.Globalization.DateTimeStyles.None;

        return System.DateTime.TryParseExact(text, formate, inv, none, out erg)
            || System.DateTime.TryParse(text, deDe, none, out erg);
    }

    // ===============================
    // KONTOSTAND FÜR EIN BESTIMMTES JAHR
    // Summiert nur Einkommen/Ausgaben mit Datum in diesem Jahr.
    // ===============================
    private float BerechneKontostandFuerJahr(int jahr)
    {
        float summe = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (TryParseDatum(e.getDatum(), out DateTime d) && d.Year == jahr)
                    summe += e.Amount;

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (TryParseDatum(a.getDatum(), out DateTime d) && d.Year == jahr)
                    summe -= a.Amount;

        return summe;
    }


    public void deleteEntry(int id)
    {
        if (db == null) return;


        foreach (Einkommen currentEinkommen in db.getAllEinkommenEntries())
        {
            if (currentEinkommen.getId() == id) { db.deleteEinkommen(id); createList(); return; }
        }
        foreach (Ausgaben currentAusgaben in db.getAllAusgabenEntries())
        {
            if (currentAusgaben.getId() == id) { db.deleteAusgaben(id); createList(); return; }
        }
    }

    private List<KassenbuchEintrag> getCombinedEntries()
    {
        List<KassenbuchEintrag> entries = new List<KassenbuchEintrag>();
        return entries;
    }

    public void ExportJahrAlsPdf(string jahr)
    {
        Debug.Log($"[DEBUG-Export] ExportJahrAlsPdf wurde aufgerufen mit Jahr='{jahr}'");

        if (db == null)
        {
            Debug.LogError("[Export] Keine Datenbankverbindung vorhanden!");
            ShowFehler("Keine Datenbankverbindung vorhanden.");
            return;
        }

        /* AUSKOMMENTIERT NACH MERGE CONFLICT DA ENTRIES CONTEXT FEHLT - Pontus
        foreach (Ausgaben ausgabe in db.getAllAusgabenEntries())
        {
            entries.Add(new KassenbuchEintrag(
                ausgabe.getId(),
                "Ausgabe",
                ausgabe.Amount,
                ausgabe.Description,
                ausgabe.Datum
            ));
        }
        */ 

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben  = db.getAllAusgabenEntries();  


        int gefundeneEintraege = 0;


        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                if (e.getDatum().EndsWith(jahr) || e.getDatum().StartsWith(jahr)) gefundeneEintraege++;
            }
        }


        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
            {
                if (a.getDatum().EndsWith(jahr) || a.getDatum().StartsWith(jahr)) gefundeneEintraege++;
            }
        }


        if (gefundeneEintraege == 0)
        {
            Debug.LogWarning($"[Export Abgebrochen] Es sind keine Einträge in der Datenbank für das Jahr {jahr} vorhanden.");
            ShowFehler($"Keine Einträge im Jahr {jahr} gefunden.");
            return;
        }


        string fileName = "Kassenbuch_" + jahr + ".pdf";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
       
        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Document doc = new Document();
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                doc.Open();
               
                doc.Add(new Paragraph("Kassenbuch Export - Jahr: " + jahr));
                doc.Add(new Paragraph("Generiert am: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")));
                doc.Add(new Paragraph(" "));
               
                if (einkommen != null)
                {
                    foreach (var e in einkommen)
                    {
                        if (e.getDatum().EndsWith(jahr) || e.getDatum().StartsWith(jahr))
                        {
                            doc.Add(new Paragraph("Einnahme | " + e.getDatum() + " | " + e.getAmount() + "€ | " + e.getDescription()));
                        }
                    }
                }
               
                if (ausgaben != null)
                {
                    foreach (var a in ausgaben)
                    {
                        if (a.getDatum().EndsWith(jahr) || a.getDatum().StartsWith(jahr))
                        {
                            doc.Add(new Paragraph("Ausgabe | " + a.getDatum() + " | " + a.getAmount() + "€ | " + a.getDescription()));
                        }
                    }
                }
               
                doc.Close();
            }
            Debug.Log("PDF erfolgreich exportiert unter: " + filePath);

            // Datei direkt öffnen, damit der Erfolg auch sichtbar ist
            // (ohne das würde der Export lautlos im Hintergrund passieren)
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception oeffnenEx)
            {
                Debug.LogWarning("[Export] PDF erstellt, konnte aber nicht automatisch geöffnet werden: " + oeffnenEx.Message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim PDF-Export: " + ex.Message);
            ShowFehler("Export fehlgeschlagen: " + ex.Message);
        }
    }
}


public class KassenbuchEintrag
{
    public int Id;
    public string Typ;
    public float Betrag;
    public string Beschreibung;
    public string Datum;


   public KassenbuchEintrag(string typ, float betrag, string beschreibung, string datum)
    {
        Id = 0;
        Typ = typ;
        Betrag = betrag;
        Beschreibung = beschreibung;
        Datum = datum;
    }

    public KassenbuchEintrag(int id, string typ, float betrag, string beschreibung, string datum)
    {
        Id = id;
        Typ = typ;
        Betrag = betrag;
        Beschreibung = beschreibung;
        Datum = datum;
    }
}
