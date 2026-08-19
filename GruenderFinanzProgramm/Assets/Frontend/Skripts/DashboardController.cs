// ================================================================
// DashboardController.cs
// ================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class DashboardController : MonoBehaviour
{
    private VisualElement _root;
    private int _currentYear;
    private int _currentMonth;
    private LineChartElement _chart;

    // Pro Kalendertag: getrennte Anzahl Angebote/Rechnungen (für die Badge
    // "A:1 R:2") + die einzelnen Einträge mit Kundennamen (für den Hover-Tooltip)
    private class TagInfo
    {
        public int AngebotAnzahl;
        public int RechnungAnzahl;
        public List<string> Eintraege = new List<string>();
    }

    private Dictionary<string, TagInfo> _deadlines = new Dictionary<string, TagInfo>();

    // Gemeinsames Tooltip-Element für alle Kalendertage (wird einmal erzeugt
    // und wiederverwendet statt 42 einzelner Tooltips)
    private VisualElement _kalenderTooltip;
    private Label _kalenderTooltipLabel;

    private readonly string[] _monthNames =
    {
        "Januar","Februar","März","April","Mai","Juni",
        "Juli","August","September","Oktober","November","Dezember"
    };

    // ============================================================
    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        var today = DateTime.Today;
        _currentYear = today.Year;
        _currentMonth = today.Month;

        SetupButtons();
        SetupCalendar();
        SetupChart();
        RegistriereHelpTooltips();

        StartCoroutine(LadeNachFrame());

        AppEventManager.OnKundenAnzahlGeaendert += OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert += OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert += OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert += OnKassenbuch;
        AppEventManager.OnDokumenteFortschrittGeaendert += OnDokumenteFortschritt;
    }

    private System.Collections.IEnumerator LadeNachFrame()
    {
        yield return null;
        LadeInitialDaten();
        LadeDeadlines();
        RenderKalender();
    }

    void OnDisable()
    {
        AppEventManager.OnKundenAnzahlGeaendert -= OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert -= OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert -= OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert -= OnKassenbuch;
        AppEventManager.OnDokumenteFortschrittGeaendert -= OnDokumenteFortschritt;
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================
    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(_root, "btn-help-seitentitel",
            "Das Dashboard gibt dir einen \u00dcberblick \u00fcber dein Unternehmen. " +
            "Du siehst den Dokumenten-Fortschritt, deine wichtigsten Kennzahlen " +
            "und einen Kalender mit anstehenden Fristen.");

        HelpTooltip.Registriere(_root, "btn-help-fortschritt",
            "Zeigt wie vollst\u00e4ndig deine Gr\u00fcndungsunterlagen sind. " +
            "Jeder Abschnitt (Stammdaten, Vertr\u00e4ge, Steuer, Rechnungen, Sonstiges) " +
            "f\u00fcllt sich, wenn du die zugeh\u00f6rigen Dokumente hinterlegt hast. " +
            "Ziel: 100\u00a0% in allen Bereichen.");

        HelpTooltip.Registriere(_root, "btn-help-kunden",
            "Anzahl der Kunden, die du in diesem Jahr in der Kundendatenbank " +
            "angelegt hast. Klicke auf \u201eKundendatenbank\u201c um Kunden zu verwalten.");

        HelpTooltip.Registriere(_root, "btn-help-angebote",
            "Anzahl der Angebote, die du in diesem Jahr erstellt hast. " +
            "Klicke auf \u201eAngebot\u201c um ein neues Angebot zu erstellen. " +
            "Angebote mit ablaufender G\u00fcltigkeit werden im Kalender markiert.");

        HelpTooltip.Registriere(_root, "btn-help-rechnungen",
            "Anzahl der Rechnungen, die du in diesem Jahr gestellt hast. " +
            "Klicke auf \u201eRechnung\u201c um eine neue Rechnung zu erstellen. " +
            "F\u00e4llige Rechnungen werden im Kalender markiert.");

        HelpTooltip.Registriere(_root, "btn-help-kassenbuch-umsatz",
            "Bilanz aus Einnahmen minus Ausgaben f\u00fcr das aktuell gew\u00e4hlte Jahr. " +
            "Der Wert \u00e4ndert sich wenn du im Kalender das Jahr wechselst.");

        HelpTooltip.Registriere(_root, "btn-help-kontostand",
            "Dein kumulierter Kontostand bis zum Ende des gew\u00e4hlten Jahres \u2014 " +
            "alle Einnahmen minus alle Ausgaben seit Beginn. " +
            "Gr\u00fcn = positiv, Rot = negativ.");

        HelpTooltip.Registriere(_root, "btn-help-kassenbuch-statistik",
            "Zeigt die monatliche Bilanz (Einnahmen minus Ausgaben) als Linienchart. " +
            "Punkte \u00fcber der Nulllinie sind Gewinnmonate, darunter Verlustmonate. " +
            "Das Jahr w\u00e4hlst du \u00fcber den Kalender rechts.");

        HelpTooltip.Registriere(_root, "btn-help-kalender",
            "Farbig markierte Tage haben anstehende Fristen: " +
            "Angebote deren G\u00fcltigkeit abl\u00e4uft (die beim Erstellen festgelegte Frist) " +
            "und Rechnungen mit F\u00e4lligkeitsdatum. " +
            "Fahre mit der Maus \u00fcber einen markierten Tag f\u00fcr Details.");
    }

    // ============================================================
    // INITIALER DB-LOAD
    // ============================================================
    private void LadeInitialDaten()
    {
        LadeJahresDaten(_currentYear);
        LadeGruendungspfadFortschritt();
    }

    // ============================================================
    // GRÜNDUNGSPFAD-FORTSCHRITT
    // ============================================================
    private void LadeGruendungspfadFortschritt()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) { SetzeSegmente(0f, 0f, 0f, 0f, 0f); return; }

            var docs = db.getAllUserDocuments();
            var save = docs?.OrderByDescending(d => d.id)
                           .FirstOrDefault(d => d.documentType == 9001);

            if (save == null) { SetzeSegmente(0f, 0f, 0f, 0f, 0f); return; }

            var data = JsonUtility.FromJson<GruendungspfadSpeicher>(save.text);
            if (data == null) { SetzeSegmente(0f, 0f, 0f, 0f, 0f); return; }

            var erledigteIds = data.erledigteIds ?? new List<string>();
            var eigeneSchritte = data.eigeneSchritte ?? new List<EigenerSchritt>();

            var phasenDef = new (string name, string[] ids)[]
            {
                ("Vorbereitung", new[] { "vorb_1","vorb_2","vorb_3","vorb_4","vorb_5" }),
                ("Anmeldung",    new[] { "anm_1","anm_2","anm_3","anm_4","anm_5" }),
                ("Finanzen",     new[] { "fin_1","fin_2","fin_3","fin_4" }),
                ("Betrieb",      new[] { "betr_1","betr_2","betr_3","betr_4" }),
                ("Sonstiges",    new[] { "sonst_1","sonst_2","sonst_3" }),
            };

            float[] segmente = new float[5];
            int gesamtAlle = 0;
            int gesamtErled = 0;

            for (int i = 0; i < phasenDef.Length; i++)
            {
                var (phaseName, pflichtIds) = phasenDef[i];
                int g = pflichtIds.Length;
                int e = pflichtIds.Count(id => erledigteIds.Contains(id));

                var eigene = eigeneSchritte
                    .Where(s => s.id != null && s.id.StartsWith(phaseName + "_eigen_"))
                    .ToList();
                g += eigene.Count;
                e += eigene.Count(s => erledigteIds.Contains(s.id));

                segmente[i] = g > 0 ? (float)e / g : 0f;
                gesamtAlle += g;
                gesamtErled += e;
            }

            var eigeneOhnePhase = eigeneSchritte
                .Where(s => s.id != null && !phasenDef.Any(p => s.id.StartsWith(p.name + "_eigen_")))
                .ToList();
            gesamtAlle += eigeneOhnePhase.Count;
            gesamtErled += eigeneOhnePhase.Count(s => erledigteIds.Contains(s.id));

            float gesamt = gesamtAlle > 0 ? (float)gesamtErled / gesamtAlle : 0f;

            SetzeSegmente(segmente[0], segmente[1], segmente[2], segmente[3], segmente[4]);

            var pctLabel = _root.Q<Label>("lbl-progress-percent");
            if (pctLabel != null)
                pctLabel.text = Mathf.RoundToInt(gesamt * 100f) + "%";
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Dashboard] LadeGruendungspfadFortschritt: " + ex.Message);
            SetzeSegmente(0f, 0f, 0f, 0f, 0f);
        }
    }

    [System.Serializable]
    private class EigenerSchritt { public string id; }

    [System.Serializable]
    private class GruendungspfadSpeicher
    {
        public List<string> erledigteIds;
        public List<EigenerSchritt> eigeneSchritte;
    }

    private void LadeJahresDaten(int jahr)
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) return;

            var alleKunden = db.getAllCustomers();
            int kundenImJahr = 0;
            if (alleKunden != null)
                foreach (var k in alleKunden)
                    if (k.lastUpdated.Year == jahr) kundenImJahr++;
            SetLabel("lbl-kunden", kundenImJahr.ToString());

            var alleAngebote = db.getAllOffers();
            int angeboteImJahr = 0;
            if (alleAngebote != null)
                foreach (var a in alleAngebote)
                    if (TryParseDatum(a.date, out DateTime d) && d.Year == jahr) angeboteImJahr++;
            SetLabel("lbl-angebote", angeboteImJahr.ToString());

            var alleRechnungen = db.getAllInvoices();
            int rechnungenImJahr = 0;
            if (alleRechnungen != null)
                foreach (var r in alleRechnungen)
                    if (TryParseDatum(r.date, out DateTime d) && d.Year == jahr) rechnungenImJahr++;
            SetLabel("lbl-rechnungen", rechnungenImJahr.ToString());

            float umsatzJahr = 0f;
            float[] monate = BerechneMonatsBilanz(db, jahr, out umsatzJahr);
            float kontostand = BerechneKontostandBisJahresende(db, jahr);

            SetLabel("lbl-kassenbuch", FormatEuro(umsatzJahr));
            SetKontostand(kontostand);

            _chart?.SetValues(monate);
            UpdateYAxisLabels(monate);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Dashboard] JahresDatenLaden: " + e.Message);
        }
    }

    float BerechneKontostandBisJahresende(DataBase db, int jahr)
    {
        var stichtag = new DateTime(jahr, 12, 31);
        float summe = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
            {
                bool ok = TryParseDatum(e.Datum, out DateTime d);
                bool zaehlt = ok && d <= stichtag;
                Debug.Log($"[DEBUG-Kontostand] EINKOMMEN '{e.Datum}' Amount={e.Amount} parsed={ok}->{(ok ? d.ToString("yyyy-MM-dd") : "FEHLER")} zaehlt={zaehlt}");
                if (zaehlt) summe += e.Amount;
            }

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
            {
                bool ok = TryParseDatum(a.Datum, out DateTime d);
                bool zaehlt = ok && d <= stichtag;
                Debug.Log($"[DEBUG-Kontostand] AUSGABE '{a.Datum}' Amount={a.Amount} parsed={ok}->{(ok ? d.ToString("yyyy-MM-dd") : "FEHLER")} zaehlt={zaehlt}");
                if (zaehlt) summe -= a.Amount;
            }

        Debug.Log($"[DEBUG-Kontostand] ENDERGEBNIS = {summe}");
        return summe;
    }

    void SetKontostand(float wert)
    {
        var lbl = _root.Q<Label>("lbl-kontostand");
        if (lbl == null) return;

        lbl.text = FormatEuro(wert);

        lbl.RemoveFromClassList("stat-value-green");
        lbl.RemoveFromClassList("stat-value-red");
        lbl.AddToClassList(wert < 0 ? "stat-value-red" : "stat-value-green");
    }

    static readonly string[] DatumFormate =
        { "dd.MM.yyyy","d.M.yyyy","dd.M.yyyy","d.MM.yyyy","yyyy-MM-dd","yyyy/MM/dd" };

    bool TryParseDatum(string text, out DateTime ergebnis)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var deDe = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        var none = System.Globalization.DateTimeStyles.None;

        return DateTime.TryParseExact(text, DatumFormate, inv, none, out ergebnis)
            || DateTime.TryParse(text, deDe, none, out ergebnis);
    }

    float[] BerechneMonatsBilanz(DataBase db, int jahr, out float summeJahr)
    {
        float[] bilanzProMonat = new float[12];
        summeJahr = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (TryParseDatum(e.Datum, out DateTime d) && d.Year == jahr)
                { bilanzProMonat[d.Month - 1] += e.Amount; summeJahr += e.Amount; }

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (TryParseDatum(a.Datum, out DateTime d) && d.Year == jahr)
                { bilanzProMonat[d.Month - 1] -= a.Amount; summeJahr -= a.Amount; }

        return bilanzProMonat;
    }

    void UpdateYAxisLabels(float[] monatswerte)
    {
        var yAxisContainer = _root.Q<VisualElement>("chart-y-axis");
        if (yAxisContainer == null || monatswerte == null || monatswerte.Length == 0) return;

        float maxV = float.MinValue, minV = float.MaxValue;
        foreach (var v in monatswerte) { if (v > maxV) maxV = v; if (v < minV) minV = v; }

        float range = Mathf.Max(maxV - minV, 1f);
        float vMin = minV - range * 0.08f;
        float vMax = maxV + range * 0.08f;

        var labels = yAxisContainer.Query<Label>().ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            float t = labels.Count > 1 ? (float)i / (labels.Count - 1) : 0f;
            float wert = Mathf.Lerp(vMax, vMin, t);
            labels[i].text = FormatiereEuroKompakt(wert);
        }
    }

    string FormatiereEuroKompakt(float wert)
    {
        string vorzeichen = wert < 0 ? "-" : "";
        float abs = Mathf.Abs(wert);

        if (abs >= 1_000_000f) return $"{vorzeichen}\u20ac{(abs / 1_000_000f).ToString("0.#")}M";
        if (abs >= 1_000f) return $"{vorzeichen}\u20ac{(abs / 1_000f).ToString("0.#")}k";
        return $"{vorzeichen}\u20ac{abs.ToString("0")}";
    }

    // ============================================================
    // EVENT-HANDLER
    // ============================================================
    void OnKunden(int anzahl) => SetLabel("lbl-kunden", anzahl.ToString());

    void OnAngebote(int anzahl)
    {
        SetLabel("lbl-angebote", anzahl.ToString());
        LadeDeadlines();
        RenderKalender();
    }

    void OnRechnungen(int anzahl)
    {
        SetLabel("lbl-rechnungen", anzahl.ToString());
        LadeDeadlines();
        RenderKalender();
    }

    void OnKassenbuch(float umsatzJahr, float kontostand, float[] monate)
    {
        SetLabel("lbl-kassenbuch", FormatEuro(umsatzJahr));
        SetKontostand(kontostand);
        if (monate != null && monate.Length == 12)
        {
            _chart?.SetValues(monate);
            UpdateYAxisLabels(monate);
        }
    }

    void OnDokumenteFortschritt(float stammdaten, float vertraege, float steuer, float rechnungen, float sonstiges)
    {
        SetzeSegmente(stammdaten, vertraege, steuer, rechnungen, sonstiges);
        LadeGruendungspfadFortschritt();
    }

    void SetzeSegmente(float stammdaten, float vertraege, float steuer, float rechnungen, float sonstiges)
    {
        SetSegment("seg-fill-stammdaten", stammdaten);
        SetSegment("seg-fill-vertraege", vertraege);
        SetSegment("seg-fill-steuer", steuer);
        SetSegment("seg-fill-rechnungen", rechnungen);
        SetSegment("seg-fill-sonstiges", sonstiges);
    }

    void SetSegment(string name, float wert)
    {
        var fill = _root.Q<VisualElement>(name);
        if (fill == null) return;

        fill.RemoveFromClassList("seg-full");
        fill.RemoveFromClassList("seg-partial");
        fill.RemoveFromClassList("seg-empty");
        fill.style.width = new StyleLength(new Length(Mathf.Clamp01(wert) * 100f, LengthUnit.Percent));
    }

    // ============================================================
    // BUTTONS
    // ============================================================
    void SetupButtons()
    {
        BindScene("btn-nav-kunden", "KundenDB");
        BindScene("btn-nav-angebot", "Angebot");
        BindScene("btn-nav-rechnung", "Rechnung");
        BindScene("btn-nav-kassenbuch", "Kassenbuch");
    }

    void BindScene(string buttonName, string sceneName)
    {
        _root.Q<Button>(buttonName)?.RegisterCallback<ClickEvent>(_ =>
        {
            bool exists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                if (SceneUtility.GetScenePathByBuildIndex(i).Contains(sceneName))
                { exists = true; break; }

            if (exists) SceneManager.LoadScene(sceneName);
            else Debug.LogWarning($"[Dashboard] Scene '{sceneName}' nicht gefunden.");
        });
    }

    void SetLabel(string name, string text)
    {
        var lbl = _root.Q<Label>(name);
        if (lbl != null) lbl.text = text;
    }

    string FormatEuro(float wert)
    {
        var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

        string vorzeichen = wert < 0 ? "- " : "";
        float betragAbs = Mathf.Abs(wert);

        return "€ " + vorzeichen + betragAbs.ToString("N2", de);
    }

    // ============================================================
    // KALENDER
    // ============================================================
    void SetupCalendar()
    {
        var dropMonat = _root.Q<DropdownField>("dropdown-monat");
        var dropJahr = _root.Q<DropdownField>("dropdown-jahr");

        if (dropMonat != null)
        {
            dropMonat.choices = new List<string>(_monthNames);
            dropMonat.index = _currentMonth - 1;
            dropMonat.RegisterValueChangedCallback(_ =>
            { _currentMonth = dropMonat.index + 1; RenderKalender(); });
        }

        if (dropJahr != null)
        {
            var jahre = new List<string>();
            for (int y = _currentYear - 3; y <= _currentYear + 3; y++) jahre.Add(y.ToString());
            dropJahr.choices = jahre;
            dropJahr.value = _currentYear.ToString();
            dropJahr.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int y))
                {
                    _currentYear = y;
                    RenderKalender();
                    LadeJahresDaten(_currentYear);
                    LadeDeadlines();
                }
            });
        }

        _root.Q<Button>("btn-prev-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(-1));
        _root.Q<Button>("btn-next-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(+1));

        var grid = _root.Q<VisualElement>("kalender-grid");
        if (grid != null) grid.style.flexGrow = 1;

        SetupKalenderTage();
        RenderKalender();
    }

    void WechsleMonat(int delta)
    {
        int vorherigesJahr = _currentYear;

        _currentMonth += delta;
        if (_currentMonth < 1) { _currentMonth = 12; _currentYear--; }
        if (_currentMonth > 12) { _currentMonth = 1; _currentYear++; }

        var dropMonat = _root.Q<DropdownField>("dropdown-monat");
        var dropJahr = _root.Q<DropdownField>("dropdown-jahr");
        if (dropMonat != null) dropMonat.index = _currentMonth - 1;
        if (dropJahr != null) dropJahr.value = _currentYear.ToString();

        RenderKalender();

        if (_currentYear != vorherigesJahr)
        {
            LadeJahresDaten(_currentYear);
            LadeDeadlines();
        }
    }

    // ============================================================
    // DEADLINES
    // ============================================================
    private const int ANGEBOT_GUELTIGKEIT_TAGE = 14;

    void LadeDeadlines()
    {
        _deadlines.Clear();

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        // WICHTIG: Jeder Eintrag wird EINZELN abgesichert. Vorher hing die
        // gesamte Methode in einem try/catch - warf auch nur EIN
        // fehlerhafter Datensatz (kaputtes Datum, gelöschter Kunde o.ä.)
        // eine Exception, wurden ALLE nachfolgenden Angebote/Rechnungen in
        // der Liste stillschweigend gar nicht mehr verarbeitet, auch neu
        // erstellte, völlig valide Einträge nicht. Jetzt überspringt ein
        // fehlerhafter Datensatz nur sich selbst.
        var angebote = db.getAllOffers();
        if (angebote != null)
            foreach (var a in angebote)
            {
                try
                {
                    // FIX: Nutzte vorher immer Erstelldatum+14 Tage, obwohl
                    // beim Anlegen des Angebots die echte Frist ("validUntil")
                    // gespeichert wird und vom Nutzer frei geändert werden kann.
                    DateTime? gueltigBis = null;
                    if (TryParseDatum(a.validUntil, out DateTime echtGueltig))
                        gueltigBis = echtGueltig;
                    else if (TryParseDatum(a.date, out DateTime erstellt))
                        gueltigBis = erstellt.AddDays(ANGEBOT_GUELTIGKEIT_TAGE); // Fallback für alte Angebote ohne validUntil

                    if (gueltigBis == null) continue;

                    string key = gueltigBis.Value.ToString("yyyy-MM-dd");
                    string kunde = HoleKundenname(db, a.customerId);
                    FuegeDeadlineHinzu(key, istAngebot: true,
                        $"Angebot {a.offerNumber} \u2013 {kunde} (g\u00fcltig bis)");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Dashboard] Angebot {a?.offerNumber} konnte nicht f\u00fcr den Kalender verarbeitet werden: " + e.Message);
                }
            }

        var rechnungen = db.getAllInvoices();
        if (rechnungen != null)
            foreach (var r in rechnungen)
            {
                try
                {
                    if (TryParseDatum(r.dueDate, out DateTime faellig))
                    {
                        string key = faellig.ToString("yyyy-MM-dd");
                        string kunde = HoleKundenname(db, r.customerId);
                        FuegeDeadlineHinzu(key, istAngebot: false,
                            $"Rechnung {r.invoiceNumber} \u2013 {kunde} (f\u00e4llig)");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Dashboard] Rechnung {r?.invoiceNumber} konnte nicht f\u00fcr den Kalender verarbeitet werden: " + e.Message);
                }
            }
    }

    void FuegeDeadlineHinzu(string key, bool istAngebot, string text)
    {
        if (!_deadlines.TryGetValue(key, out var info))
        {
            info = new TagInfo();
            _deadlines[key] = info;
        }
        if (istAngebot) info.AngebotAnzahl++;
        else info.RechnungAnzahl++;
        info.Eintraege.Add(text);
    }

    string HoleKundenname(DataBase db, int customerId)
    {
        try
        {
            var kunde = db.getCustomerById(customerId);
            return string.IsNullOrEmpty(kunde?.name) ? "Unbekannter Kunde" : kunde.name;
        }
        catch
        {
            return "Unbekannter Kunde";
        }
    }

    void RenderKalender()
    {
        var today = DateTime.Today;
        var ersterTag = new DateTime(_currentYear, _currentMonth, 1);
        int tageImMonat = DateTime.DaysInMonth(_currentYear, _currentMonth);
        int startWochentag = (int)ersterTag.DayOfWeek;

        for (int i = 0; i < 42; i++)
        {
            var btn = _root.Q<Button>($"cal-day-{i}");
            if (btn == null) continue;

            var zahlLabel = btn.Q<Label>("cal-day-zahl");
            var badgeLabel = btn.Q<Label>("cal-day-badge");

            btn.RemoveFromClassList("cal-day-today");
            btn.RemoveFromClassList("cal-day-other-month");
            btn.RemoveFromClassList("cal-day-deadline");
            btn.RemoveFromClassList("cal-day-empty");
            btn.userData = null;
            if (badgeLabel != null) badgeLabel.text = "";

            bool istImAktuellenMonat = i >= startWochentag && i - startWochentag < tageImMonat;

            if (!istImAktuellenMonat)
            {
                if (zahlLabel != null) zahlLabel.text = "";
                btn.AddToClassList("cal-day-empty");
                btn.SetEnabled(false);
                continue;
            }

            btn.SetEnabled(true);
            int tag = i - startWochentag + 1;
            if (zahlLabel != null) zahlLabel.text = tag.ToString();

            if (tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year)
                btn.AddToClassList("cal-day-today");

            string key = new DateTime(_currentYear, _currentMonth, tag).ToString("yyyy-MM-dd");
            if (_deadlines.TryGetValue(key, out var info))
            {
                btn.AddToClassList("cal-day-deadline");
                btn.userData = info;

                var teile = new List<string>();
                if (info.AngebotAnzahl > 0) teile.Add($"A:{info.AngebotAnzahl}");
                if (info.RechnungAnzahl > 0) teile.Add($"R:{info.RechnungAnzahl}");
                if (badgeLabel != null) badgeLabel.text = string.Join(" ", teile);
            }
        }
    }

    // ============================================================
    // KALENDER: einmaliger Aufbau der Tages-Zellen (Zahl + Badge)
    // und des gemeinsamen Hover-Tooltips
    // ============================================================
    void SetupKalenderTage()
    {
        for (int i = 0; i < 42; i++)
        {
            var btn = _root.Q<Button>($"cal-day-{i}");
            if (btn == null) continue;
            if (btn.Q<Label>("cal-day-zahl") != null) continue; // schon initialisiert

            btn.text = "";

            var zahl = new Label { name = "cal-day-zahl" };
            zahl.AddToClassList("cal-day-zahl");
            btn.Add(zahl);

            var badge = new Label { name = "cal-day-badge" };
            badge.AddToClassList("cal-day-badge");
            btn.Add(badge);

            btn.RegisterCallback<PointerEnterEvent>(_ => ZeigeKalenderTooltip(btn));
            btn.RegisterCallback<PointerLeaveEvent>(_ => VersteckeKalenderTooltip());
        }

        if (_kalenderTooltip == null)
        {
            _kalenderTooltip = new VisualElement { pickingMode = PickingMode.Ignore };
            _kalenderTooltip.style.position = Position.Absolute;
            _kalenderTooltip.style.display = DisplayStyle.None;
            _kalenderTooltip.style.width = 260;
            _kalenderTooltip.style.backgroundColor = new Color(28f / 255f, 28f / 255f, 28f / 255f, 0.97f);
            _kalenderTooltip.style.borderTopLeftRadius = 10;
            _kalenderTooltip.style.borderTopRightRadius = 10;
            _kalenderTooltip.style.borderBottomLeftRadius = 10;
            _kalenderTooltip.style.borderBottomRightRadius = 10;
            _kalenderTooltip.style.borderTopWidth = 1;
            _kalenderTooltip.style.borderRightWidth = 1;
            _kalenderTooltip.style.borderBottomWidth = 1;
            _kalenderTooltip.style.borderLeftWidth = 1;
            var randFarbe = new Color(230f / 255f, 57f / 255f, 70f / 255f);
            _kalenderTooltip.style.borderTopColor = randFarbe;
            _kalenderTooltip.style.borderRightColor = randFarbe;
            _kalenderTooltip.style.borderBottomColor = randFarbe;
            _kalenderTooltip.style.borderLeftColor = randFarbe;
            _kalenderTooltip.style.paddingTop = 12;
            _kalenderTooltip.style.paddingBottom = 12;
            _kalenderTooltip.style.paddingLeft = 14;
            _kalenderTooltip.style.paddingRight = 14;

            _kalenderTooltipLabel = new Label();
            _kalenderTooltipLabel.style.color = new Color(0.88f, 0.88f, 0.88f);
            _kalenderTooltipLabel.style.fontSize = 13;
            _kalenderTooltipLabel.style.whiteSpace = WhiteSpace.Normal;
            _kalenderTooltip.Add(_kalenderTooltipLabel);

            _root.Add(_kalenderTooltip);
        }
    }

    void ZeigeKalenderTooltip(Button tag)
    {
        if (!(tag.userData is TagInfo info) || info.Eintraege.Count == 0) return;

        _kalenderTooltipLabel.text = string.Join("\n", info.Eintraege);
        _kalenderTooltip.style.display = DisplayStyle.Flex;

        _kalenderTooltip.schedule.Execute(() =>
        {
            var tagPos = tag.worldBound;
            var rootPos = _root.worldBound;
            float hoehe = _kalenderTooltip.resolvedStyle.height > 0 ? _kalenderTooltip.resolvedStyle.height : 60f;

            float left = tagPos.x - rootPos.x + (tagPos.width / 2f) - 130f;
            float top = tagPos.y - rootPos.y - hoehe - 10f;

            left = Mathf.Clamp(left, 8f, rootPos.width - 260f - 8f);
            if (top < 8f) top = tagPos.y - rootPos.y + tagPos.height + 10f;

            _kalenderTooltip.style.left = left;
            _kalenderTooltip.style.top = top;
        });
    }

    void VersteckeKalenderTooltip()
    {
        if (_kalenderTooltip != null) _kalenderTooltip.style.display = DisplayStyle.None;
    }

    // ============================================================
    // CHART
    // ============================================================
    void SetupChart()
    {
        var canvas = _root.Q<VisualElement>("chart-canvas");
        if (canvas == null) return;

        _chart = new LineChartElement(new float[12]);
        canvas.Add(_chart);
        _chart.style.position = Position.Absolute;
        _chart.style.left = 0; _chart.style.top = 0;
        _chart.style.right = 0; _chart.style.bottom = 0;
    }

    // ============================================================
    // INNER CLASS: Liniendiagramm
    // ============================================================
    private class LineChartElement : VisualElement
    {
        private float[] _values;
        private static readonly Color LineColor = new Color(0.502f, 0.812f, 0.584f, 1f);
        private static readonly Color GridColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color FillColor = new Color(0.502f, 0.812f, 0.584f, 0.12f);
        private static readonly Color PointColor = new Color(0.502f, 0.812f, 0.584f, 1f);

        public LineChartElement(float[] v) { _values = v; generateVisualContent += Draw; }
        public void SetValues(float[] v) { _values = v; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext ctx)
        {
            if (_values == null || _values.Length < 2) return;
            float w = contentRect.width, h = contentRect.height, padX = 12f, padY = 14f;

            float maxV = float.MinValue, minV = float.MaxValue;
            foreach (var v in _values) { if (v > maxV) maxV = v; if (v < minV) minV = v; }
            float range = Mathf.Max(maxV - minV, 1f);
            float vMin = minV - range * 0.08f;
            float vMax = maxV + range * 0.08f;
            float vRange = vMax - vMin;

            var p = ctx.painter2D;
            p.strokeColor = GridColor; p.lineWidth = 0.5f;
            for (int g = 0; g <= 4; g++)
            {
                float yg = padY + (h - 2 * padY) * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(padX, yg)); p.LineTo(new Vector2(w - padX, yg)); p.Stroke();
            }

            var pts = new Vector2[_values.Length];
            for (int i = 0; i < _values.Length; i++)
                pts[i] = new Vector2(
                    padX + (w - 2 * padX) * i / (_values.Length - 1),
                    padY + (h - 2 * padY) * (1f - (_values[i] - vMin) / vRange));

            p.fillColor = FillColor;
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, h - padY));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Length - 1].x, h - padY));
            p.ClosePath(); p.Fill();

            p.strokeColor = LineColor; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
            p.Stroke();

            p.fillColor = PointColor;
            foreach (var pt in pts) { p.BeginPath(); p.Arc(pt, 3.5f, 0f, 360f); p.Fill(); }
        }
    }
}
