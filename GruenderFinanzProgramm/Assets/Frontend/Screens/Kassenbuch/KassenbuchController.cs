using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;
using System.Collections.Generic;
using iTextSharp.text; // Für den PDF-Export benötigt
// FIX: Kein "using iTextSharp.text.pdf;" - das kollidiert mit
// UnityEngine.UIElements.TextField (iTextSharp.text.pdf hat ebenfalls
// eine Klasse "TextField" -> CS0104 mehrdeutige Referenz).
// Stattdessen gezielte Aliase nur für das im GuV-Export Benötigte.
using PdfWriter = iTextSharp.text.pdf.PdfWriter;
using PdfPTable = iTextSharp.text.pdf.PdfPTable;
using PdfPCell = iTextSharp.text.pdf.PdfPCell;
using System.Linq;
using System.Globalization;


public class KassenbuchController : MonoBehaviour
{
    private VisualElement _overlay;
    private VisualElement _confirmOverlay;
    private int _pendingDeleteId = -1;
    private string _pendingDeleteTyp = "";
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
    // FIX: Vorher hat der Kalender beim Rendern IMMER mit System.DateTime.Today
    // verglichen -> egal welchen Tag man anklickte, markiert blieb immer "heute".
    // Jetzt wird das tatsächlich ausgewählte Datum separat gemerkt.
    private DateTime? _ausgewaehltesDatum = null;
    private readonly string[] _monthNames =
    {
        "Januar","Februar","März","April","Mai","Juni",
        "Juli","August","September","Oktober","November","Dezember"
    };


    private string _aktuellerTyp;
    private DataBase db;

    private DropdownField artDropdown;

    // FIX: Zentrale, feste Kultur für ALLE Betrags-Formatierungen im
    // Kassenbuch (Eingabefeld, Kontostand, Tabelle, PDF-Export). Vorher
    // wurde teils die Systemkultur genutzt und Kommas manuell durch
    // Punkte ersetzt - das war je nach Regionaleinstellung des Rechners
    // fehlerhaft (Tausender-Trennzeichen fehlten/waren falsch).
    private static readonly CultureInfo DeKultur = CultureInfo.GetCultureInfo("de-DE");

    // ==========================================
    // SORTIERUNG DER TABELLE
    // Gleiches Muster wie "Buchhaltung Screen Controller.cs":
    // Klick auf Spaltenkopf -> Spalte umschalten / Richtung togglen.
    // Standard: nach Datum, neueste zuerst.
    // ==========================================
    private string _sortColumn = "Datum";
    private bool _sortAscending = false;
    private readonly Dictionary<string, Label> _sortHeaders = new Dictionary<string, Label>();
    private readonly Dictionary<string, string> _sortHeaderBaseText = new Dictionary<string, string>();


    // ==========================================
    // UNSERE PLATZHALTER-TEXTE
    // ==========================================
    private const string PLACEHOLDER_BETRAG = "0,00";
    private const string PLACEHOLDER_ZWECK = "z. B. Einkauf, Tanken, Gehalt...";


    private VisualElement _root;

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
        _root = root;


        _overlay = root.Q<VisualElement>("popup-overlay");

        // Greift sich das Element "tableBody" für deine Dokumente/Einträge
        tableInput = root.Q<VisualElement>("tableBody") ?? root.Q<VisualElement>("unity-content-container");

        balanceLabel = root.Q<Label>("balanceLabel") ?? root.Q<Label>("label-kontostand");
        letzteAktualLabel = root.Q<Label>("letzteAktual");

        _confirmOverlay = root.Q<VisualElement>("confirm-overlay");
        if (_confirmOverlay != null)
        {
            _confirmOverlay.RemoveFromHierarchy();
            root.Add(_confirmOverlay);
            _confirmOverlay.style.display = DisplayStyle.None;

            root.Q<Button>("btn-confirm-ja")?.RegisterCallback<ClickEvent>(_ => BestaetigenLoeschen());
            root.Q<Button>("btn-confirm-nein")?.RegisterCallback<ClickEvent>(_ =>
            {
                _confirmOverlay.style.display = DisplayStyle.None;
                _pendingDeleteId = -1;
                _pendingDeleteTyp = "";
            });
        }

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


        var today = DateTime.Today;
        _currentYear = today.Year;
        _currentMonth = today.Month;


        SetupDashboardCalendar(root);

        // ==========================================
        // SORTIERBARE SPALTENKÖPFE REGISTRIEREN
        // ==========================================
        RegisterSortHeader(root.Q<Label>("col-header-name"), "Name");
        RegisterSortHeader(root.Q<Label>("col-header-datum"), "Datum");
        RegisterSortHeader(root.Q<Label>("col-header-betrag"), "Betrag");
        RegisterSortHeader(root.Q<Label>("col-header-art"), "Art");
        RegisterSortHeader(root.Q<Label>("col-header-typ"), "Typ");
        AktualisiereSortPfeile();


        // ==========================================
        // INITIALISIERUNG DES EXPORT-DROPDOWNS (100 JAHRE)
        // ==========================================
        dropJahrField = root.Q<DropdownField>("dropJahr");
        if (dropJahrField != null)
        {
            List<string> jahre = new List<string>();
            for (int i = today.Year; i >= 2010; i--)
            {
                jahre.Add(i.ToString());
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

        // FIX: Popup hatte keine Möglichkeit, es "einfach so" zu schließen
        // (nur Speichern/Abbrechen als Textbuttons unten) - jetzt zusätzlich
        // ein X oben rechts, funktional identisch zu Abbrechen.
        var btnPopupSchliessen = root.Q<Button>("btn-popup-schliessen");
        if (btnPopupSchliessen != null) btnPopupSchliessen.clicked += ClosePopup;


        // --- EVENT-VERKNÜPFUNGEN FÜR DASHBOARD BUTTONS ---
        if (btnAngebotErstellen != null) btnAngebotErstellen.clicked += () => Debug.Log("[Dashboard] Erstelle neues Angebot...");
        if (btnRechnungErstellen != null) btnRechnungErstellen.clicked += () => Debug.Log("[Dashboard] Erstelle neue Rechnung...");
        if (btnFinanzenDashboard != null) btnFinanzenDashboard.clicked += () => Debug.Log("[Dashboard] Tabellen-Ansicht Finanzen aktualisiert.");


        // ==========================================
        // EXPORT BUTTON EVENT VERKNÜPFEN
        // ==========================================
        // GEÄNDERT: Klick öffnet jetzt ein Auswahl-Popup (EÜR/GuV/
        // Buchungsjournal + Jahr) statt sofort die GuV zu exportieren.
        var btnExport = root.Q<Button>("FinanzExport") ?? root.Q<Button>("btn-export") ?? root.Q<Button>("Export");
        if (btnExport != null)
            btnExport.clicked += OeffneExportPopup;

        RegistriereExportPopup(root);

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
            "Kredite",
            "Sacheinlagen",
            "Wertpapiere",
            "Börse / Krypto",
            "Tilgungsraten",
            "Finanzamt",
            "Steuern",
            "Gehälter",
            "Zinsen",
            "Büroausstattung",
            "Fuhrpark",
            "Maschinen / Anlagen",
            "Software / Lizenzen",
            "Corporate Design",
            "Homepage",
            "Grundausstattung",
            "Umlaufvermögen"
        };

            artDropdown.value = "Marketing";
        }


        createList();
        RegistriereHelpTooltips(root);
    }



    private void RegistriereHelpTooltips(VisualElement root)
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Das Kassenbuch erfasst alle Einnahmen und Ausgaben deines Unternehmens. " +
            "Wähle oben rechts das Jahr, um Einträge jahresweise zu filtern. " +
            "Der Kontostand berechnet sich automatisch aus allen Buchungen.");

        HelpTooltip.Registriere(root, "btn-help-kontostand",
            "Zeigt den aktuellen Saldo: alle Einnahmen minus alle Ausgaben. " +
            "Klicke auf den Betrag um Details zu sehen. " +
            "Der Wert wird bei jeder neuen Buchung automatisch aktualisiert.");

        HelpTooltip.Registriere(root, "btn-help-ausgaben",
            "Erfasst eine neue Ausgabe im Kassenbuch. " +
            "Gib Betrag, Verwendungszweck, Art und Datum ein. " +
            "Ausgaben reduzieren deinen Kontostand.");

        HelpTooltip.Registriere(root, "btn-help-einnahmen",
            "Erfasst eine neue Einnahme im Kassenbuch. " +
            "Gib Betrag, Verwendungszweck, Art und Datum ein. " +
            "Einnahmen erhöhen deinen Kontostand.");

        HelpTooltip.Registriere(root, "btn-help-finanzexport",
            "Öffnet ein Auswahlfenster: Wähle zwischen Einnahmen-Überschuss-Rechnung " +
            "(EÜR), Gewinn- und Verlustrechnung (GuV) oder Buchungsjournal, " +
            "sowie das gewünschte Jahr. Die Datei kann direkt an das Finanzamt " +
            "weitergegeben werden.");

        HelpTooltip.Registriere(root, "btn-help-tabelle",
            "Tabellenspalten: Name (Verwendungszweck der Buchung), " +
            "Buchungstag (Datum im Format TT.MM.JJJJ), " +
            "Betrag (Einnahmen grün / Ausgaben rot), " +
            "Art der Buchung (z.B. Marketing, Steuern, Gehälter), " +
            "Einnahme / Ausgabe (Typ der Buchung).");

        HelpTooltip.Registriere(root, "btn-help-popup",
            "Trage hier die Details der Buchung ein. " +
            "Betrag, Verwendungszweck, Art und Datum sind Pflichtfelder. " +
            "Das Datum kann über den Kalender ausgewählt werden.");
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

        // HINWEIS FÜR ALEX: Beträge werden aktuell als "float" in der DB
        // gespeichert (Einkommen.Amount / Ausgaben.Amount). Für Geldbeträge
        // ist "decimal" eigentlich die robustere Wahl (keine Rundungsfehler
        // durch Binärdarstellung). Hier im UI wird strikt auf 2
        // Nachkommastellen begrenzt - ob das DB-seitig auch so
        // durchgesetzt/umgestellt werden soll, bitte mit Alex klären.

        // Reicht für "10.000.000,00" (13 Zeichen) plus etwas Puffer
        field.maxLength = 20;

        if (string.IsNullOrEmpty(field.value) || field.value == placeholder)
        {
            field.SetValueWithoutNotify(placeholder);
        }


        field.RegisterValueChangedCallback(evt =>
 {
     string original = evt.newValue;

     // FIX: Den Platzhalter selbst nicht anfassen - vorher wurde "0,00"
     // (der Platzhalter) durch diese Logik in "0" OHNE Komma zerlegt,
     // sobald ClosePopup() das Feld programmgesteuert zurückgesetzt hat.
     if (original == placeholder)
     {
         return;
     }

     // FIX (3. Anlauf): Die bisherigen Versuche haben aus dem Text ein
     // Dezimaltrennzeichen (Komma/Punkt) herausgelesen - das ging schief,
     // sobald die eigene Live-Formatierung selbst schon einen Punkt als
     // Tausendertrennzeichen eingefügt hatte (z.B. nach "1.000" wurde eine
     // weitere getippte Ziffer fälschlich als Cent-Stelle hinter DIESEM
     // Punkt gelesen -> Sprung auf "1,00 €" statt "10.000 €").
     //
     // Jetzt wie vom Chef vorgeschlagen: ein reines Cent-Eingabefeld nach
     // Kassenautomaten-Prinzip. Es werden IMMER nur die Ziffern gelesen -
     // Kommas/Punkte werden komplett ignoriert, egal ob vom Nutzer oder von
     // der eigenen Formatierung eingefügt. Die letzten 2 Ziffern sind IMMER
     // die Cent-Stellen, alles davor volle Euro. Kein Rätselraten mehr,
     // welches Zeichen das Dezimaltrennzeichen sein soll - es gibt keins
     // mehr zu erraten.
     string ziffern = new string(original.Where(char.IsDigit).ToArray());

     if (ziffern.Length == 0)
     {
         field.SetValueWithoutNotify("");
         return;
     }

     // Sicherheitsnetz gegen extrem lange Eingaben (Überlauf-Schutz)
     if (ziffern.Length > 10) ziffern = ziffern.Substring(ziffern.Length - 10);

     long centWert = long.Parse(ziffern);

     // 10 Millionen Euro Limit (in Cent)
     const long MAX_CENT = 10_000_000L * 100L;
     if (centWert > MAX_CENT) centWert = MAX_CENT;

     long euroTeil = centWert / 100;
     long centTeil = centWert % 100;

     string formatiert = euroTeil.ToString("N0", KassenbuchController.DeKultur)
         + "," + centTeil.ToString("D2");

     field.SetValueWithoutNotify(formatiert);

     // Cursor ans Ende setzen
     field.schedule.Execute(() =>
     {
         field.cursorIndex = formatiert.Length;
         field.selectIndex = formatiert.Length;
     });
 });

        field.RegisterCallback<FocusInEvent>(evt =>
        {
            if (field.value == placeholder)
            {
                field.SetValueWithoutNotify("");
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
            else
            {
                // FIX: Vorher "field.value = ..." benutzt, das feuert erneut
                // den ValueChanged-Handler und das gerade entfernte "€"
                // wurde von der Formatierungslogik sofort wieder verworfen.
                field.SetValueWithoutNotify(field.value.Replace("€", "").Trim());
            }
        });


        field.RegisterCallback<FocusOutEvent>(evt =>
        {
            string aktuellerText = field.value.Trim();


            if (string.IsNullOrEmpty(aktuellerText))
            {
                field.SetValueWithoutNotify(placeholder);
            }
            else
            {
                if (!aktuellerText.Contains("€"))
                {
                    field.SetValueWithoutNotify(aktuellerText + " €");
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


        field.RegisterCallback<FocusInEvent>(evt =>
        {
            if (field.value == placeholder)
            {
                field.value = "";
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
        });


        field.RegisterCallback<FocusOutEvent>(evt =>
        {
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
        var dropJahr = root.Q<DropdownField>("dropdown-jahr");


        if (dropMonat != null)
        {
            dropMonat.choices = new List<string>(_monthNames);
            dropMonat.index = _currentMonth - 1;
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
            dropJahr.value = _currentYear.ToString();
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
        if (_currentMonth < 1) { _currentMonth = 12; _currentYear--; }
        if (_currentMonth > 12) { _currentMonth = 1; _currentYear++; }


        var dropMonat = root.Q<DropdownField>("dropdown-monat");
        var dropJahr = root.Q<DropdownField>("dropdown-jahr");
        if (dropMonat != null) dropMonat.index = _currentMonth - 1;
        if (dropJahr != null) dropJahr.value = _currentYear.ToString();


        RenderKalender(root);
    }


    void RenderKalender(VisualElement root)
    {
        var today = DateTime.Today;
        var ersterTag = new DateTime(_currentYear, _currentMonth, 1);
        int tageImMonat = DateTime.DaysInMonth(_currentYear, _currentMonth);

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

                bool istHeute = tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year;
                bool istAusgewaehlt = _ausgewaehltesDatum.HasValue
                    && tag == _ausgewaehltesDatum.Value.Day
                    && _currentMonth == _ausgewaehltesDatum.Value.Month
                    && _currentYear == _ausgewaehltesDatum.Value.Year;

                // Ausgewählter Tag: kräftige Marken-Grün-Füllung (statt Blau,
                // passt jetzt zum Rest der App: #80CF95).
                // "Heute" bekommt nur einen dezenten grauen Rahmen, damit die
                // beiden nicht mehr wie "2 ausgewählte Tage" aussehen.
                if (istAusgewaehlt)
                {
                    btn.AddToClassList("cal-day-today");
                    btn.style.backgroundColor = new StyleColor(new UnityEngine.Color(128f/255f, 207f/255f, 149f/255f, 1f));
                    btn.style.color = new StyleColor(new UnityEngine.Color(0.1f, 0.1f, 0.1f));
                }
                else if (istHeute)
                {
                    btn.style.borderTopWidth = 1;
                    btn.style.borderBottomWidth = 1;
                    btn.style.borderLeftWidth = 1;
                    btn.style.borderRightWidth = 1;
                    var heuteRahmen = new StyleColor(new UnityEngine.Color(0.55f, 0.55f, 0.55f, 0.9f));
                    btn.style.borderTopColor = heuteRahmen;
                    btn.style.borderBottomColor = heuteRahmen;
                    btn.style.borderLeftColor = heuteRahmen;
                    btn.style.borderRightColor = heuteRahmen;
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
        _ausgewaehltesDatum = new DateTime(jahr, monat, tag);

        if (_overlay != null)
        {
            var inputDatum = _overlay.Q<TextField>("input-datum");
            if (inputDatum != null) inputDatum.value = _ausgewaehltesDatum.Value.ToString("dd.MM.yyyy");
        }

        if (meinUxmlKalender != null)
        {
            meinUxmlKalender.style.display = DisplayStyle.None;
        }

        // Kalender neu zeichnen, damit die neue Markierung sofort sichtbar
        // ist, falls man ihn direkt danach wieder öffnet.
        RenderKalender(GetComponent<UIDocument>().rootVisualElement);
    }


    private void OpenPopup(string typ)
    {
        if (_overlay == null) return;


        _aktuellerTyp = typ;
        _overlay.Q<Label>("popup-title").text = $"{typ} hinzufügen";
        _overlay.Q<TextField>("input-datum").value = DateTime.Now.ToString("dd.MM.yyyy");
        // FIX: Kalender-Markierung mit dem Standarddatum synchron halten,
        // sonst zeigt der Kalender beim ersten Öffnen "heute" markiert,
        // obwohl das Feld schon ein anderes Datum enthalten könnte.
        _ausgewaehltesDatum = DateTime.Now.Date;

        // FIX: SetupBetragInputAnpassung/SetupPlaceholderSimulation wurden
        // vorher bei JEDEM OpenPopup erneut aufgerufen und haben dabei
        // IMMER NEUE Event-Handler registriert, ohne die alten zu entfernen.
        // Nach mehrfachem Öffnen liefen also mehrere Formatierungs-Handler
        // gleichzeitig übereinander - das war die Ursache für die kaputte
        // Formatierung ("0" ohne Komma) beim erneuten Öffnen. Die Handler
        // werden jetzt nur noch EINMAL in OnEnable registriert.


        // Dropdown-Kategorien nach Typ filtern
        var dropdown = _overlay.Q<DropdownField>("Art");
        if (dropdown != null)
        {
            if (typ == "Ausgabe")
            {
                dropdown.choices = new List<string>
                {
                    "Marketing",
                    "Reisekosten",
                    "Sonstige Kosten",
                    "Barentnahme / Privatentnahme",
                    "Tilgungsraten",
                    "Finanzamt",
                    "Steuern",
                    "Gehälter",
                    "Zinsen",
                    "Büroausstattung",
                    "Fuhrpark",
                    "Maschinen / Anlagen",
                    "Software / Lizenzen",
                    "Corporate Design",
                    "Homepage",
                    "Grundausstattung",
                    "Umlaufvermögen"
                };
            }
            else // Einnahme
            {
                dropdown.choices = new List<string>
                {
                    "Privateinzahlung",
                    "Sonstige Einzahlung",
                    "Darlehen",
                    "Kredite",
                    "Sacheinlagen",
                    "Wertpapiere",
                    "Börse / Krypto",
                    "Umsatzerlöse",
                    "Sonstige Einnahmen"
                };
            }
            dropdown.index = 0;
        }


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

        // Zusätzliche Absicherung zum Limit in SetupBetragInputAnpassung -
        // falls z.B. per Copy&Paste eine zu hohe Zahl reinrutscht.
        // HINWEIS: Chef wollte prüfen lassen, ob diese Grenze zusätzlich auf
        // DB-Ebene (mit Alex) erzwungen werden soll, statt nur hier im UI.
        const float MAX_BETRAG_EURO = 10_000_000f;
        if (betrag > MAX_BETRAG_EURO)
        {
            ShowFehler("Der Betrag darf maximal 10.000.000 € betragen.");
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

        eintrag.Art = artDropdown?.value ?? "";
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
        var inv = System.Globalization.CultureInfo.InvariantCulture;
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


        if (eintrag.Typ == "Einnahme")
        {
            db.createEinkommen(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum, eintrag.Art, artDropdown.value);
        }
        else
        {
            db.createAusgaben(eintrag.Betrag, eintrag.Beschreibung, eintrag.Datum, eintrag.Art, artDropdown.value);
        }
    }


    // ─────────────────────────────────────────────────
    // SORTIERUNG (analog zu Buchhaltung Screen Controller)
    // ─────────────────────────────────────────────────

    private void RegisterSortHeader(Label lbl, string spalte)
    {
        if (lbl == null) return;

        _sortHeaders[spalte] = lbl;
        _sortHeaderBaseText[spalte] = lbl.text;

        lbl.pickingMode = PickingMode.Position;
        lbl.RegisterCallback<ClickEvent>(_ => SetSortColumn(spalte));
    }

    private void SetSortColumn(string spalte)
    {
        if (_sortColumn == spalte)
            _sortAscending = !_sortAscending; // gleiche Spalte -> Richtung umkehren
        else
        {
            _sortColumn = spalte;
            // Bei Datum/Betrag ist "neueste/größte zuerst" meist sinnvoller als Default
            _sortAscending = spalte != "Datum" && spalte != "Betrag";
        }

        AktualisiereSortPfeile();
        createList();
    }

    private void AktualisiereSortPfeile()
    {
        string pfeil = _sortAscending ? " ↑" : " ↓";
        foreach (var kv in _sortHeaders)
        {
            bool aktiv = kv.Key == _sortColumn;
            string basisText = _sortHeaderBaseText[kv.Key];
            kv.Value.text = aktiv ? basisText + pfeil : basisText;
            kv.Value.style.color = aktiv
                ? new StyleColor(UnityEngine.Color.white)
                : new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
        }
    }

    private IEnumerable<KassenbuchZeile> SortiereEintraege(List<KassenbuchZeile> liste)
    {
        IEnumerable<KassenbuchZeile> sorted = _sortColumn switch
        {
            "Name" => liste.OrderBy(z => z.Name, StringComparer.OrdinalIgnoreCase),
            "Datum" => liste.OrderBy(z => z.DatumSort),
            "Betrag" => liste.OrderBy(z => z.Betrag),
            "Art" => liste.OrderBy(z => z.Art, StringComparer.OrdinalIgnoreCase),
            "Typ" => liste.OrderBy(z => z.Typ, StringComparer.OrdinalIgnoreCase),
            _ => liste.OrderByDescending(z => z.DatumSort)
        };

        return _sortAscending ? sorted : sorted.Reverse();
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
            balanceLabel.text = differenz.ToString("N2", DeKultur) + " €";
            balanceLabel.style.color = differenz < 0
                ? new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f))   // Rot #E63946
                : new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f)); // Gruen #80CF95
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
            float[] monate = new float[12];


            var einkommenListe = db.getAllEinkommenEntries();
            if (einkommenListe != null)
            {
                foreach (var e in einkommenListe)
                {
                    // Versucht das Datum zu parsen. Je nach dem ob es eine Eigenschaft oder ein Getter ist.
                    if (System.DateTime.TryParse(e.getDatum(), out System.DateTime datum) && datum.Year == heute.Year)
                    {
                        // Falls getAmount() als String zurückgegeben wird, bereinigen wir es, um Fehler zu vermeiden.
                        float amount = e.Amount;
                        umsatzJahr += amount;
                        monate[datum.Month - 1] += amount;
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
        List<Ausgaben> ausgabenList = db.getAllAusgabenEntries();

        // Tabelle nach gewähltem Jahr filtern, falls ein gültiges Jahr gewählt ist
        if (jahresFilterAktiv)
        {
            einkommenList = einkommenList?.FindAll(e =>
                TryParseDatum(e.getDatum(), out DateTime d) && d.Year == gewaehltesJahr);
            ausgabenList = ausgabenList?.FindAll(a =>
                TryParseDatum(a.getDatum(), out DateTime d) && d.Year == gewaehltesJahr);
        }


        // ==========================================
        // EINNAHMEN + AUSGABEN ZU EINER EINHEITLICHEN,
        // SORTIERBAREN LISTE ZUSAMMENFÜHREN
        // (Vorher: zwei starre Blöcke untereinander -
        //  dadurch war weder Sortierung noch eine
        //  chronologisch gemischte Ansicht möglich.)
        // ==========================================
        var alleEintraege = new List<KassenbuchZeile>();

        if (einkommenList != null)
        {
            foreach (Einkommen e in einkommenList)
            {
                TryParseDatum(e.getDatum(), out DateTime datumSort);
                float betrag = e.Amount;
                alleEintraege.Add(new KassenbuchZeile
                {
                    Id = e.getId(),
                    Typ = "Einnahme",
                    Name = e.getDescription(),
                    Datum = e.getDatum(),
                    DatumSort = datumSort,
                    Betrag = betrag,
                    Art = e.getArt()
                });
            }
        }

        if (ausgabenList != null)
        {
            foreach (Ausgaben a in ausgabenList)
            {
                TryParseDatum(a.getDatum(), out DateTime datumSort);
                float betrag = a.Amount;
                alleEintraege.Add(new KassenbuchZeile
                {
                    Id = a.getId(),
                    Typ = "Ausgabe",
                    Name = a.getDescription(),
                    Datum = a.getDatum(),
                    DatumSort = datumSort,
                    Betrag = betrag,
                    Art = a.getArt()
                });
            }
        }

        foreach (var zeile in SortiereEintraege(alleEintraege))
        {
            VisualElement newEntryCopy = outputTemplate.Instantiate();

            Label nameLabel = newEntryCopy.Q<Label>("Name");
            Label typLabel = newEntryCopy.Q<Label>("Typ");
            Label betragLabel = newEntryCopy.Q<Label>("Betrag");
            Label erstellTagLabel = newEntryCopy.Q<Label>("ErstellTag");
            Label artLabel = newEntryCopy.Q<Label>("Art");
            Button loeschenBtn = newEntryCopy.Q<Button>("BtnLoeschen");

            if (nameLabel == null) { Debug.LogError("Label 'Name' nicht gefunden!"); continue; }

            bool istEinnahme = zeile.Typ == "Einnahme";

            nameLabel.text = zeile.Name;
            if (artLabel != null) artLabel.text = string.IsNullOrWhiteSpace(zeile.Art) ? "–" : zeile.Art;
            betragLabel.text = zeile.Betrag.ToString("N2", DeKultur) + " €";
            erstellTagLabel.text = zeile.Datum;
            typLabel.text = istEinnahme ? "Einkommen" : "Ausgabe";

            // Gruen fuer Einnahme, Rot fuer Ausgabe
            if (betragLabel != null)
            {
                betragLabel.style.color = istEinnahme
                    ? new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f))
                    : new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f));
            }

            // Loeschen Button & Hover
            if (loeschenBtn != null)
            {
                int id = zeile.Id;
                string typ = zeile.Typ;
                loeschenBtn.clicked += () => Loeschen(typ, id);

                loeschenBtn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f));
                    loeschenBtn.style.color = new StyleColor(UnityEngine.Color.white);
                });
                loeschenBtn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    loeschenBtn.style.backgroundColor = new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f, 0.15f));
                    loeschenBtn.style.color = new StyleColor(new UnityEngine.Color(230f / 255f, 57f / 255f, 70f / 255f));
                });
            }

            tableInput.Add(newEntryCopy);
        }
    }




    private void BestaetigenLoeschen()
    {
        if (_confirmOverlay != null) _confirmOverlay.style.display = DisplayStyle.None;
        if (_pendingDeleteId >= 0) deleteEntry(_pendingDeleteId);
        _pendingDeleteId = -1;
        _pendingDeleteTyp = "";
    }

    public void Loeschen(string typ, int id)
    {
        if (_confirmOverlay == null) { deleteEntry(id); return; }
        _pendingDeleteId = id;
        _pendingDeleteTyp = typ;
        _confirmOverlay.style.display = DisplayStyle.Flex;
    }

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

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var deDe = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        var none = System.Globalization.DateTimeStyles.None;

        return System.DateTime.TryParseExact(text, formate, inv, none, out erg)
            || System.DateTime.TryParse(text, deDe, none, out erg);
    }


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

    // ============================================================
    // EXPORT-AUSWAHL-POPUP (EÜR / GuV / Buchungsjournal + Jahr)
    // ============================================================
    private VisualElement _exportOverlay;
    private VisualElement _karteEuer, _karteGuv, _karteJournal;
    private DropdownField _dropdownExportJahr;
    private string _gewaehlterExportTyp = "EUER"; // Default: das, was das Finanzamt am häufigsten will

    private void RegistriereExportPopup(VisualElement root)
    {
        _exportOverlay = root.Q<VisualElement>("export-overlay");
        _karteEuer = root.Q<VisualElement>("karte-export-euer");
        _karteGuv = root.Q<VisualElement>("karte-export-guv");
        _karteJournal = root.Q<VisualElement>("karte-export-journal");
        _dropdownExportJahr = root.Q<DropdownField>("dropdown-export-jahr");

        if (_karteEuer != null) _karteEuer.RegisterCallback<ClickEvent>(_ => WaehleExportTyp("EUER"));
        if (_karteGuv != null) _karteGuv.RegisterCallback<ClickEvent>(_ => WaehleExportTyp("GUV"));
        if (_karteJournal != null) _karteJournal.RegisterCallback<ClickEvent>(_ => WaehleExportTyp("JOURNAL"));

        var btnAbbrechen = root.Q<Button>("btn-export-abbrechen");
        if (btnAbbrechen != null) btnAbbrechen.clicked += SchliesseExportPopup;

        var btnBestaetigen = root.Q<Button>("btn-export-bestaetigen");
        if (btnBestaetigen != null)
            btnBestaetigen.clicked += () =>
            {
                if (_dropdownExportJahr == null) return;
                string jahr = _dropdownExportJahr.value;

                switch (_gewaehlterExportTyp)
                {
                    case "GUV":     ExportJahrAlsPdf(jahr); break;
                    case "JOURNAL": ExportBuchungsjournalAlsPdf(jahr); break;
                    default:        ExportEuerAlsPdf(jahr); break;
                }
                SchliesseExportPopup();
            };
    }

    private void OeffneExportPopup()
    {
        if (_exportOverlay == null) return;

        // Jahres-Dropdown befüllen (gleiche Spanne wie der Haupt-Jahresfilter)
        if (_dropdownExportJahr != null)
        {
            var jahre = new List<string>();
            int aktuellesJahr = DateTime.Today.Year;
            for (int i = aktuellesJahr; i >= 2010; i--) jahre.Add(i.ToString());
            _dropdownExportJahr.choices = jahre;

            // Falls der Haupt-Jahresfilter des Kassenbuchs schon ein Jahr
            // gewählt hat, das gleich vorauswählen - sonst aktuelles Jahr.
            string vorausgewaehlt = dropJahrField != null && !string.IsNullOrEmpty(dropJahrField.value)
                ? dropJahrField.value
                : aktuellesJahr.ToString();
            _dropdownExportJahr.value = jahre.Contains(vorausgewaehlt) ? vorausgewaehlt : aktuellesJahr.ToString();
        }

        WaehleExportTyp(_gewaehlterExportTyp);
        _exportOverlay.style.display = DisplayStyle.Flex;
    }

    private void SchliesseExportPopup()
    {
        if (_exportOverlay != null) _exportOverlay.style.display = DisplayStyle.None;
    }

    private void WaehleExportTyp(string typ)
    {
        _gewaehlterExportTyp = typ;

        var gruen = new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
        var grau = new StyleColor(new UnityEngine.Color(80f / 255f, 80f / 255f, 80f / 255f));

        SetzeKartenAuswahl(_karteEuer, typ == "EUER", gruen, grau);
        SetzeKartenAuswahl(_karteGuv, typ == "GUV", gruen, grau);
        SetzeKartenAuswahl(_karteJournal, typ == "JOURNAL", gruen, grau);
    }

    private void SetzeKartenAuswahl(VisualElement karte, bool ausgewaehlt, StyleColor gruen, StyleColor grau)
    {
        if (karte == null) return;
        var rand = ausgewaehlt ? gruen : grau;
        karte.style.borderTopColor = rand;
        karte.style.borderBottomColor = rand;
        karte.style.borderLeftColor = rand;
        karte.style.borderRightColor = rand;
        karte.style.borderTopWidth = ausgewaehlt ? 2 : 1;
        karte.style.borderBottomWidth = ausgewaehlt ? 2 : 1;
        karte.style.borderLeftWidth = ausgewaehlt ? 2 : 1;
        karte.style.borderRightWidth = ausgewaehlt ? 2 : 1;
        karte.style.backgroundColor = ausgewaehlt
            ? new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f, 0.12f))
            : new StyleColor(new UnityEngine.Color(50f / 255f, 50f / 255f, 50f / 255f));
    }

    // ==========================================
    // EINNAHMEN-ÜBERSCHUSS-RECHNUNG (EÜR)
    // Standardformat nach §4 Abs. 3 EStG für kleine Unternehmen/
    // Freiberufler: Betriebseinnahmen und Betriebsausgaben jeweils nach
    // Kategorie aufgelistet, Gewinn = Einnahmen - Ausgaben.
    // ==========================================
    public void ExportEuerAlsPdf(string jahr)
    {
        if (db == null) { ShowFehler("Keine Datenbankverbindung vorhanden."); return; }
        if (!int.TryParse(jahr, out int jahrZahl)) { ShowFehler("Ungültiges Jahr für den Export."); return; }

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben = db.getAllAusgabenEntries();

        var einkommenJahr = (einkommen ?? new List<Einkommen>())
            .Where(e => TryParseDatum(e.getDatum(), out DateTime d) && d.Year == jahrZahl)
            .ToList();
        var ausgabenJahr = (ausgaben ?? new List<Ausgaben>())
            .Where(a => TryParseDatum(a.getDatum(), out DateTime d) && d.Year == jahrZahl)
            .ToList();

        if (einkommenJahr.Count == 0 && ausgabenJahr.Count == 0)
        {
            ShowFehler($"Keine Einträge im Jahr {jahr} gefunden.");
            return;
        }

        var einnahmenPosten = einkommenJahr
            .GroupBy(e => string.IsNullOrWhiteSpace(e.getArt()) ? "Sonstige Einzahlung" : e.getArt())
            .Select(g => (Name: g.Key, Betrag: g.Sum(e => e.Amount)))
            .OrderByDescending(p => p.Betrag)
            .ToList();

        var ausgabenPosten = ausgabenJahr
            .GroupBy(a => string.IsNullOrWhiteSpace(a.getArt()) ? "Sonstige Kosten" : a.getArt())
            .Select(g => (Name: g.Key, Betrag: g.Sum(a => a.Amount)))
            .OrderByDescending(p => p.Betrag)
            .ToList();

        float summeEinnahmen = einnahmenPosten.Sum(p => p.Betrag);
        float summeAusgaben = ausgabenPosten.Sum(p => p.Betrag);
        float gewinn = summeEinnahmen - summeAusgaben;

        string username = GetSafeFolderName(StateManager.Instance.getCurrentUser().username);
        string folderPath = Path.Combine(Application.persistentDataPath, "PDFs", username, "Finanzamt-Export");
        Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath, "EUER_" + jahr + ".pdf");

        try
        {
            Document doc = new Document(PageSize.A4, 50, 50, 50, 50);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent("", "", true);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle("Einnahmen-Überschuss-Rechnung " + jahr);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var headerFontWeiss = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, iTextSharp.text.Color.WHITE);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var grayFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));

                var gruenBrand = new iTextSharp.text.Color(102, 165, 119);
                var rotBrand = new iTextSharp.text.Color(210, 70, 80);

                doc.Add(new Paragraph("Einnahmen-Überschuss-Rechnung " + jahr, titleFont));
                doc.Add(new Paragraph("nach § 4 Abs. 3 EStG", grayFont));
                doc.Add(new Paragraph("Generiert am: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"), grayFont));
                doc.Add(new Paragraph(" "));

                // --- Betriebseinnahmen ---
                doc.Add(new Paragraph("1. Betriebseinnahmen", boldFont));
                doc.Add(new Paragraph(" "));
                PdfPTable tabEin = new PdfPTable(2);
                tabEin.WidthPercentage = 100f;
                tabEin.SetWidths(new float[] { 4f, 2f });
                AddGuvHeaderCell(tabEin, "Kategorie", headerFontWeiss, gruenBrand);
                AddGuvHeaderCell(tabEin, "Betrag", headerFontWeiss, gruenBrand);
                foreach (var p in einnahmenPosten)
                {
                    AddGuvBodyCell(tabEin, p.Name, normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(tabEin, p.Betrag.ToString("N2", DeKultur) + " €", normalFont, Element.ALIGN_RIGHT);
                }
                AddGuvBodyCell(tabEin, "Summe Betriebseinnahmen", boldFont, Element.ALIGN_LEFT, true);
                AddGuvBodyCell(tabEin, summeEinnahmen.ToString("N2", DeKultur) + " €", boldFont, Element.ALIGN_RIGHT, true);
                doc.Add(tabEin);
                doc.Add(new Paragraph(" "));

                // --- Betriebsausgaben ---
                doc.Add(new Paragraph("2. Betriebsausgaben", boldFont));
                doc.Add(new Paragraph(" "));
                PdfPTable tabAus = new PdfPTable(2);
                tabAus.WidthPercentage = 100f;
                tabAus.SetWidths(new float[] { 4f, 2f });
                AddGuvHeaderCell(tabAus, "Kategorie", headerFontWeiss, rotBrand);
                AddGuvHeaderCell(tabAus, "Betrag", headerFontWeiss, rotBrand);
                foreach (var p in ausgabenPosten)
                {
                    AddGuvBodyCell(tabAus, p.Name, normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(tabAus, p.Betrag.ToString("N2", DeKultur) + " €", normalFont, Element.ALIGN_RIGHT);
                }
                AddGuvBodyCell(tabAus, "Summe Betriebsausgaben", boldFont, Element.ALIGN_LEFT, true);
                AddGuvBodyCell(tabAus, summeAusgaben.ToString("N2", DeKultur) + " €", boldFont, Element.ALIGN_RIGHT, true);
                doc.Add(tabAus);
                doc.Add(new Paragraph(" "));

                // --- Ergebnis ---
                var ergebnisFarbe = gewinn >= 0 ? gruenBrand : rotBrand;
                var ergebnisFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, ergebnisFarbe);
                string ergebnisText = gewinn >= 0
                    ? "3. Gewinn " + jahr + ": " + gewinn.ToString("N2", DeKultur) + " €"
                    : "3. Verlust " + jahr + ": " + Math.Abs(gewinn).ToString("N2", DeKultur) + " €";
                doc.Add(new Paragraph(ergebnisText, ergebnisFont));
                doc.Add(new Paragraph(
                    "(Betriebseinnahmen " + summeEinnahmen.ToString("N2", DeKultur) + " € - Betriebsausgaben "
                    + summeAusgaben.ToString("N2", DeKultur) + " €)", grayFont));

                doc.Close();
            }
            OeffneExportierteDatei(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim EÜR-Export: " + ex.Message);
            ShowFehler("Export fehlgeschlagen: " + ex.Message);
        }
    }

    // ==========================================
    // BUCHUNGSJOURNAL
    // Chronologische Liste ALLER Buchungen des Jahres - für Prüfungen und
    // Nachweise gegenüber dem Finanzamt.
    // ==========================================
    public void ExportBuchungsjournalAlsPdf(string jahr)
    {
        if (db == null) { ShowFehler("Keine Datenbankverbindung vorhanden."); return; }
        if (!int.TryParse(jahr, out int jahrZahl)) { ShowFehler("Ungültiges Jahr für den Export."); return; }

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben = db.getAllAusgabenEntries();

        var buchungen = new List<(DateTime Datum, string Beschreibung, string Art, string Typ, float Betrag)>();

        foreach (var e in einkommen ?? new List<Einkommen>())
            if (TryParseDatum(e.getDatum(), out DateTime d) && d.Year == jahrZahl)
                buchungen.Add((d, e.getDescription(), e.getArt(), "Einnahme", e.Amount));

        foreach (var a in ausgaben ?? new List<Ausgaben>())
            if (TryParseDatum(a.getDatum(), out DateTime d) && d.Year == jahrZahl)
                buchungen.Add((d, a.getDescription(), a.getArt(), "Ausgabe", a.Amount));

        if (buchungen.Count == 0)
        {
            ShowFehler($"Keine Einträge im Jahr {jahr} gefunden.");
            return;
        }

        buchungen = buchungen.OrderBy(b => b.Datum).ToList();
        float summeEinnahmen = buchungen.Where(b => b.Typ == "Einnahme").Sum(b => b.Betrag);
        float summeAusgaben = buchungen.Where(b => b.Typ == "Ausgabe").Sum(b => b.Betrag);

        string username = GetSafeFolderName(StateManager.Instance.getCurrentUser().username);
        string folderPath = Path.Combine(Application.persistentDataPath, "PDFs", username, "Finanzamt-Export");
        Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath, "Buchungsjournal_" + jahr + ".pdf");

        try
        {
            Document doc = new Document(PageSize.A4.Rotate(), 40, 40, 50, 50);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent("", "", true);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle("Buchungsjournal " + jahr);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var headerFontWeiss = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, iTextSharp.text.Color.WHITE);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                var grayFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));
                var gruenText = FontFactory.GetFont(FontFactory.HELVETICA, 10, new iTextSharp.text.Color(60, 140, 80));
                var rotText = FontFactory.GetFont(FontFactory.HELVETICA, 10, new iTextSharp.text.Color(190, 60, 70));

                var kopfFarbe = new iTextSharp.text.Color(60, 60, 60);

                doc.Add(new Paragraph("Buchungsjournal " + jahr, titleFont));
                doc.Add(new Paragraph("Generiert am: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                    " \u2013 " + buchungen.Count + " Buchungen", grayFont));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100f;
                table.SetWidths(new float[] { 2f, 5f, 3f, 2f, 2.5f });

                AddGuvHeaderCell(table, "Datum", headerFontWeiss, kopfFarbe);
                AddGuvHeaderCell(table, "Beschreibung", headerFontWeiss, kopfFarbe);
                AddGuvHeaderCell(table, "Art", headerFontWeiss, kopfFarbe);
                AddGuvHeaderCell(table, "Typ", headerFontWeiss, kopfFarbe);
                AddGuvHeaderCell(table, "Betrag", headerFontWeiss, kopfFarbe);

                foreach (var b in buchungen)
                {
                    var betragFont = b.Typ == "Einnahme" ? gruenText : rotText;
                    string vorzeichen = b.Typ == "Einnahme" ? "+" : "-";

                    AddGuvBodyCell(table, b.Datum.ToString("dd.MM.yyyy"), normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(table, b.Beschreibung ?? "", normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(table, string.IsNullOrWhiteSpace(b.Art) ? "-" : b.Art, normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(table, b.Typ, normalFont, Element.ALIGN_LEFT);
                    AddGuvBodyCell(table, vorzeichen + b.Betrag.ToString("N2", DeKultur) + " €", betragFont, Element.ALIGN_RIGHT);
                }

                doc.Add(table);
                doc.Add(new Paragraph(" "));

                float saldo = summeEinnahmen - summeAusgaben;
                var saldoFarbe = saldo >= 0
                    ? new iTextSharp.text.Color(102, 165, 119)
                    : new iTextSharp.text.Color(210, 70, 80);
                var saldoFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, saldoFarbe);

                doc.Add(new Paragraph("Summe Einnahmen: " + summeEinnahmen.ToString("N2", DeKultur) + " €", boldFont));
                doc.Add(new Paragraph("Summe Ausgaben: " + summeAusgaben.ToString("N2", DeKultur) + " €", boldFont));
                doc.Add(new Paragraph("Saldo " + jahr + ": " + saldo.ToString("N2", DeKultur) + " €", saldoFont));

                doc.Close();
            }
            OeffneExportierteDatei(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim Buchungsjournal-Export: " + ex.Message);
            ShowFehler("Export fehlgeschlagen: " + ex.Message);
        }
    }

    // Datei nach dem Export direkt öffnen, damit der Erfolg auch sichtbar
    // ist (gleiches Verhalten wie beim GuV-Export, jetzt für alle 3
    // Exportarten in eine Hilfsmethode gezogen).
    private void OeffneExportierteDatei(string filePath)
    {
        Debug.Log("PDF erfolgreich exportiert unter: " + filePath);
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception oeffnenEx)
        {
            Debug.LogWarning("[Export] PDF erstellt, konnte aber nicht automatisch geöffnet werden: " + oeffnenEx.Message);
        }
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

        if (!int.TryParse(jahr, out int jahrZahl))
        {
            ShowFehler("Ungültiges Jahr für den Export.");
            return;
        }

        var einkommen = db.getAllEinkommenEntries();
        var ausgaben = db.getAllAusgabenEntries();

        // Nur Einträge des gewählten Jahres, ueber echtes geparstes Datum
        // gefiltert (vorher: fehleranfaelliger String-Vergleich
        // EndsWith/StartsWith(jahr) auf dem rohen Datumstext).
        var einkommenJahr = (einkommen ?? new List<Einkommen>())
            .Where(e => TryParseDatum(e.getDatum(), out DateTime d) && d.Year == jahrZahl)
            .ToList();
        var ausgabenJahr = (ausgaben ?? new List<Ausgaben>())
            .Where(a => TryParseDatum(a.getDatum(), out DateTime d) && d.Year == jahrZahl)
            .ToList();

        if (einkommenJahr.Count == 0 && ausgabenJahr.Count == 0)
        {
            Debug.LogWarning($"[Export Abgebrochen] Es sind keine Einträge in der Datenbank für das Jahr {jahr} vorhanden.");
            ShowFehler($"Keine Einträge im Jahr {jahr} gefunden.");
            return;
        }

        // ==========================================
        // ECHTE GEWINN- UND VERLUSTRECHNUNG (GuV)
        // Vereinfachtes Schema nach Team-Absprache:
        // Haben (Einnahmen nach Art) | Soll (Ausgaben nach Art)
        // Summe Haben / Summe Soll -> Gewinn = Haben - Soll
        // Anfangsbestand Folgejahr = kumulierter Saldo bis Jahresende
        // ==========================================
        var habenPosten = einkommenJahr
            .GroupBy(e => string.IsNullOrWhiteSpace(e.getArt()) ? "Sonstige Einzahlung" : e.getArt())
            .Select(g => (Name: g.Key, Betrag: g.Sum(e => e.Amount)))
            .OrderByDescending(p => p.Betrag)
            .ToList();

        var sollPosten = ausgabenJahr
            .GroupBy(a => string.IsNullOrWhiteSpace(a.getArt()) ? "Sonstige Kosten" : a.getArt())
            .Select(g => (Name: g.Key, Betrag: g.Sum(a => a.Amount)))
            .OrderByDescending(p => p.Betrag)
            .ToList();

        float summeHaben = habenPosten.Sum(p => p.Betrag);
        float summeSoll = sollPosten.Sum(p => p.Betrag);
        float gewinn = summeHaben - summeSoll;

        float anfangsbestandJahr = BerechneKumulierterKontostandBisEndeJahr(jahrZahl - 1);
        float endbestandJahr = anfangsbestandJahr + gewinn;

        string username = GetSafeFolderName(StateManager.Instance.getCurrentUser().username);

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "PDFs",
            username,
            "Finanzamt-Export"
        );
        Directory.CreateDirectory(folderPath);

        string fileName = "GuV_" + jahr + ".pdf";
        string filePath = Path.Combine(folderPath, fileName);

        try
        {
            Document doc = new Document(PageSize.A4, 50, 50, 50, 50);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);

                // Firma/Adresse leer lassen -> PdfFooterEvent zieht sie
                // automatisch aus den hinterlegten Firmendaten (gleiches
                // Muster wie bei Rechnungen/Angeboten).
                writer.PageEvent = new PdfFooterEvent("", "", true);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle("Gewinn- und Verlustrechnung " + jahr);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var headerFontWeiss = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, iTextSharp.text.Color.WHITE);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var grayFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));

                var gruenBrand = new iTextSharp.text.Color(102, 165, 119);
                var rotBrand = new iTextSharp.text.Color(210, 70, 80);

                doc.Add(new Paragraph("Gewinn- und Verlustrechnung " + jahr, titleFont));
                doc.Add(new Paragraph("Generiert am: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"), grayFont));
                doc.Add(new Paragraph(" "));

                // Haben/Soll Gegenüberstellung
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100f;
                table.SetWidths(new float[] { 3f, 2f, 3f, 2f });

                AddGuvHeaderCell(table, "Haben (Einnahmen)", headerFontWeiss, gruenBrand);
                AddGuvHeaderCell(table, "Betrag", headerFontWeiss, gruenBrand);
                AddGuvHeaderCell(table, "Soll (Ausgaben)", headerFontWeiss, rotBrand);
                AddGuvHeaderCell(table, "Betrag", headerFontWeiss, rotBrand);

                int zeilenAnzahl = Math.Max(habenPosten.Count, sollPosten.Count);
                for (int i = 0; i < zeilenAnzahl; i++)
                {
                    if (i < habenPosten.Count)
                    {
                        AddGuvBodyCell(table, habenPosten[i].Name, normalFont, Element.ALIGN_LEFT);
                        AddGuvBodyCell(table, habenPosten[i].Betrag.ToString("N2", DeKultur) + " €", normalFont, Element.ALIGN_RIGHT);
                    }
                    else
                    {
                        AddGuvBodyCell(table, "", normalFont, Element.ALIGN_LEFT);
                        AddGuvBodyCell(table, "", normalFont, Element.ALIGN_RIGHT);
                    }

                    if (i < sollPosten.Count)
                    {
                        AddGuvBodyCell(table, sollPosten[i].Name, normalFont, Element.ALIGN_LEFT);
                        AddGuvBodyCell(table, sollPosten[i].Betrag.ToString("N2", DeKultur) + " €", normalFont, Element.ALIGN_RIGHT);
                    }
                    else
                    {
                        AddGuvBodyCell(table, "", normalFont, Element.ALIGN_LEFT);
                        AddGuvBodyCell(table, "", normalFont, Element.ALIGN_RIGHT);
                    }
                }

                // Summenzeile
                AddGuvBodyCell(table, "Summe Haben", boldFont, Element.ALIGN_LEFT, true);
                AddGuvBodyCell(table, summeHaben.ToString("N2", DeKultur) + " €", boldFont, Element.ALIGN_RIGHT, true);
                AddGuvBodyCell(table, "Summe Soll", boldFont, Element.ALIGN_LEFT, true);
                AddGuvBodyCell(table, summeSoll.ToString("N2", DeKultur) + " €", boldFont, Element.ALIGN_RIGHT, true);

                doc.Add(table);
                doc.Add(new Paragraph(" "));

                // Jahresauswertung: Anfangsbestand, Gewinn/Verlust, Endbestand
                var auswertungFarbe = gewinn >= 0 ? gruenBrand : rotBrand;
                var auswertungFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, auswertungFarbe);

                string gewinnText = gewinn >= 0
                    ? "Jahresergebnis " + jahr + ": Gewinn " + gewinn.ToString("N2", DeKultur) + " €"
                    : "Jahresergebnis " + jahr + ": Verlust " + Math.Abs(gewinn).ToString("N2", DeKultur) + " €";

                doc.Add(new Paragraph(
                    "Anfangsbestand " + jahr + " (aus Vorjahr): " + anfangsbestandJahr.ToString("N2", DeKultur) + " €",
                    normalFont));
                doc.Add(new Paragraph(gewinnText, auswertungFont));
                doc.Add(new Paragraph(
                    "Endbestand " + jahr + " / Anfangsbestand " + (jahrZahl + 1) + ": "
                    + endbestandJahr.ToString("N2", DeKultur) + " €",
                    normalFont));

                doc.Close();
            }
            OeffneExportierteDatei(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim PDF-Export: " + ex.Message);
            ShowFehler("Export fehlgeschlagen: " + ex.Message);
        }
    }

    // Kumulierter Saldo ueber ALLE Buchungen (nicht nur das Exportjahr) bis
    // einschliesslich 31.12.<jahr> - das ist der Betrag, mit dem das
    // Folgejahr rechnerisch startet ("Anfangsbestand Jahr 2").
    private float BerechneKumulierterKontostandBisEndeJahr(int jahr)
    {
        float summe = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (TryParseDatum(e.getDatum(), out DateTime d) && d.Year <= jahr)
                    summe += e.Amount;

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (TryParseDatum(a.getDatum(), out DateTime d) && d.Year <= jahr)
                    summe -= a.Amount;

        return summe;
    }

    private static void AddGuvHeaderCell(PdfPTable table, string text, iTextSharp.text.Font font, iTextSharp.text.Color hintergrund)
    {
        var cell = new PdfPCell(new Phrase(text, font));
        cell.BackgroundColor = hintergrund;
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.PaddingTop = 8;
        cell.PaddingBottom = 8;
        cell.Border = Rectangle.NO_BORDER;
        table.AddCell(cell);
    }

    private static void AddGuvBodyCell(PdfPTable table, string text, iTextSharp.text.Font font, int alignment, bool summenzeile = false)
    {
        var cell = new PdfPCell(new Phrase(text, font));
        cell.HorizontalAlignment = alignment;
        cell.PaddingTop = 5;
        cell.PaddingBottom = 5;
        cell.PaddingLeft = 6;
        cell.PaddingRight = 6;

        if (summenzeile)
        {
            cell.BackgroundColor = new iTextSharp.text.Color(240, 240, 240);
            cell.Border = Rectangle.TOP_BORDER;
            cell.BorderWidth = 1.2f;
            cell.BorderColor = iTextSharp.text.Color.BLACK;
        }
        else
        {
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderWidth = 0.4f;
            cell.BorderColor = new iTextSharp.text.Color(220, 220, 220);
        }

        table.AddCell(cell);
    }


    private static string GetSafeFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return "User";
        }

        string safeName = folderName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar.ToString(), "");
        }

        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "User";
        }

        return safeName;
    }
}


// Einheitliche Zeile für die Tabellen-Sortierung (Einnahme + Ausgabe gemischt).
// Getrennt von KassenbuchEintrag (das ist die Zwischen-Klasse fürs Speicher-Popup).
public class KassenbuchZeile
{
    public int Id;
    public string Typ;      // "Einnahme" oder "Ausgabe"
    public string Name;
    public string Datum;
    public DateTime DatumSort;
    public float Betrag;
    public string Art;
}

public class KassenbuchEintrag
{
    public int Id;
    public string Typ;
    public float Betrag;
    public string Beschreibung;
    public string Datum;
    public string Art;


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