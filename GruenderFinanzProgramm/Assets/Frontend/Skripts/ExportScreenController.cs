using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ExportScreenController : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("UI Templates & Container")]
    [SerializeField] private VisualTreeAsset exportZeileTemplate;

    private VisualElement _root;
    private ScrollView exportListContainer;

    private DataBase currentDb;
    private int currentUserId;

    private List<UserPDFDocument> pdfListe      = new List<UserPDFDocument>();
    private List<DokumentExportEintrag> dokListe = new List<DokumentExportEintrag>();

    private class DokumentExportEintrag
    {
        public string bezeichnung;
        public string art;
        public string filePath;
        public bool   isPDF;
        public int    pdfId;
        public string datum;       // Anzeige, z.B. "05.08.2026"
        public DateTime datumSort; // für die Sortierung
    }

    // Sortierung (gleiches Muster wie Buchhaltung Screen Controller)
    private string _sortColumn = "Datum";
    private bool   _sortAscending = false; // neueste zuerst
    private Label  _hBezeichnung, _hArt, _hDatum;
    private static readonly Color HeaderGruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) { Debug.LogError("[Export] UIDocument nicht gefunden."); return; }

        VisualElement root = uiDocument.rootVisualElement;
        _root = root;
        exportListContainer = root.Q<ScrollView>("export-list-container");

        RegistriereSortHeader(root);

        LadeAlleDaten();
        RegistriereHelpTooltips(root);
    }

    // ─────────────────────────────────────────
    // SORTIERUNG (gleiches Muster wie Buchhaltung Screen Controller)
    // ─────────────────────────────────────────

    private void RegistriereSortHeader(VisualElement root)
    {
        _hBezeichnung = root.Q<Label>("header-name");
        _hArt         = root.Q<Label>("header-type");
        _hDatum       = root.Q<Label>("header-datum");

        RegistriereEinzelnenHeader(_hBezeichnung, "Bezeichnung");
        RegistriereEinzelnenHeader(_hArt,         "Art");
        RegistriereEinzelnenHeader(_hDatum,       "Datum");

        AktualisiereHeaderPfeile();
    }

    private void RegistriereEinzelnenHeader(Label lbl, string columnKey)
    {
        if (lbl == null) return;
        lbl.pickingMode = PickingMode.Position;
        lbl.RegisterCallback<ClickEvent>(_ => SetSortColumn(columnKey));
        lbl.RegisterCallback<MouseEnterEvent>(_ => lbl.style.color = Color.white);
        lbl.RegisterCallback<MouseLeaveEvent>(_ => lbl.style.color = HeaderGruen);
    }

    private void SetSortColumn(string spalte)
    {
        if (_sortColumn == spalte) _sortAscending = !_sortAscending;
        else { _sortColumn = spalte; _sortAscending = spalte != "Datum"; }

        AktualisiereHeaderPfeile();
        RefreshExportListe();
    }

    private void AktualisiereHeaderPfeile()
    {
        string pfeil = _sortAscending ? " ↑" : " ↓";

        void Aktualisiere(Label lbl, string key, string basisText)
        {
            if (lbl == null) return;
            bool aktiv = _sortColumn == key;
            lbl.text = aktiv ? basisText + pfeil : basisText;
            lbl.style.color = HeaderGruen;
        }

        Aktualisiere(_hBezeichnung, "Bezeichnung", "Bezeichnung");
        Aktualisiere(_hArt,         "Art",         "Art");
        Aktualisiere(_hDatum,       "Datum",       "Datum");
    }

    private IEnumerable<DokumentExportEintrag> SortiereEintraege()
    {
        IEnumerable<DokumentExportEintrag> sortiert = _sortColumn switch
        {
            "Bezeichnung" => dokListe.OrderBy(d => d.bezeichnung, StringComparer.OrdinalIgnoreCase),
            "Art"         => dokListe.OrderBy(d => d.art, StringComparer.OrdinalIgnoreCase),
            "Datum"       => dokListe.OrderBy(d => d.datumSort),
            _             => dokListe.OrderByDescending(d => d.datumSort)
        };
        return _sortAscending ? sortiert : sortiert.Reverse();
    }

    // ─────────────────────────────────────────
    // DATEN LADEN
    // ─────────────────────────────────────────

    private void LadeAlleDaten()
    {
        dokListe.Clear();
        LadePDFsAusDatenbank();
        LadeDokumenteAusJSON();
        RefreshExportListe();
    }

    private void LadePDFsAusDatenbank()
    {
        currentDb = UserDatabaseAccess.getCurrentUserDatabase();
        if (currentDb == null) { Debug.LogWarning("[Export] Keine aktive DB."); return; }

        if (StateManager.Instance == null || !StateManager.Instance.isLoggedIn())
        {
            Debug.LogWarning("[Export] Kein eingeloggter Nutzer.");
            return;
        }

        PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();
        if (currentUser == null) return;

        string rawUserId = currentUser.userId.Replace("user_", "");
        if (!int.TryParse(rawUserId, out currentUserId)) return;

        try
        {
            pdfListe = currentDb.getPDFDocumentsByUser(currentUserId) ?? new List<UserPDFDocument>();
        }
        catch (Exception e)
        {
            Debug.LogError("[Export] PDF-Ladefehler: " + e.Message);
            pdfListe = new List<UserPDFDocument>();
        }

        foreach (UserPDFDocument pdf in pdfListe)
        {
            dokListe.Add(new DokumentExportEintrag
            {
                bezeichnung = pdf.originalFileName,
                art         = string.IsNullOrEmpty(pdf.category) ? "Dokument" : pdf.category,
                filePath    = pdf.filePath,
                isPDF       = true,
                pdfId       = pdf.id,
                datum       = pdf.uploadedAt.ToString("dd.MM.yyyy"),
                datumSort   = pdf.uploadedAt
            });
        }

        Debug.Log($"[Export] {pdfListe.Count} PDFs geladen.");
    }

    private void LadeDokumenteAusJSON()
    {
        try
        {
            var saveData = DocumentDashboard.GetSavedDocuments();
            if (saveData?.savedDocs == null) return;

            foreach (var doc in saveData.savedDocs)
            {
                DateTime datumSort = DateTime.MinValue;
                string datumAnzeige = "\u2013"; // – als Platzhalter für alte Dokumente ohne Datum
                if (!string.IsNullOrEmpty(doc.datum) && DateTime.TryParse(doc.datum, out DateTime geparst))
                {
                    datumSort = geparst;
                    datumAnzeige = geparst.ToString("dd.MM.yyyy");
                }

                dokListe.Add(new DokumentExportEintrag
                {
                    bezeichnung = doc.title,
                    art         = doc.category,
                    filePath    = Application.persistentDataPath,
                    isPDF       = false,
                    pdfId       = -1,
                    datum       = datumAnzeige,
                    datumSort   = datumSort
                });
            }

            Debug.Log($"[Export] {saveData.savedDocs.Count} Dokumente aus JSON geladen.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Export] JSON-Ladefehler: " + e.Message);
        }
    }

    // ─────────────────────────────────────────
    // LISTE RENDERN
    // ─────────────────────────────────────────

    private void RefreshExportListe()
    {
        if (exportListContainer == null) { Debug.LogError("[Export] export-list-container fehlt."); return; }
        if (exportZeileTemplate == null) { Debug.LogError("[Export] exportZeileTemplate fehlt.");   return; }

        exportListContainer.Clear();

        if (dokListe.Count == 0) { Debug.Log("[Export] Keine Eintraege."); return; }

        foreach (DokumentExportEintrag eintrag in SortiereEintraege())
        {
            VisualElement neueZeile = exportZeileTemplate.Instantiate();

            Label         lblBezeichnung = neueZeile.Q<Label>("row-bezeichnung");
            Label         lblArt         = neueZeile.Q<Label>("row-art");
            Label         lblDatum       = neueZeile.Q<Label>("row-datum");
            DropdownField dropdown       = neueZeile.Q<DropdownField>("format-dropdown");
            Button        btnFolder      = neueZeile.Q<Button>("btn-open-folder");
            Button        btnExport      = neueZeile.Q<Button>("btn-export");

            // Nur erste Zeile des Titels anzeigen
            string anzeigeText = eintrag.bezeichnung.Split('\n')[0];
            if (anzeigeText.Length > 40) anzeigeText = anzeigeText.Substring(0, 37) + "...";

            if (lblBezeichnung != null) lblBezeichnung.text = anzeigeText;
            if (lblArt         != null) lblArt.text         = eintrag.art;
            if (lblDatum       != null) lblDatum.text       = eintrag.datum;

            if (dropdown != null)
            {
                dropdown.choices = new List<string> { "PDF" };
                dropdown.value   = "PDF";
                dropdown.SetEnabled(false);
            }

            DokumentExportEintrag lokalerEintrag = eintrag;

            if (btnFolder != null)
                btnFolder.clicked += () => OeffneOrdner(lokalerEintrag.filePath);

            if (btnExport != null)
                btnExport.clicked += () => ExportierePDF(lokalerEintrag);

            // Hilfe-Icon pro Zeile registrieren
            var zeilenHelp = neueZeile.Q<VisualElement>("btn-help-export-btn");
            if (zeilenHelp != null)
                HelpTooltip.RegistriereInKarte(_root, zeilenHelp,
                    "Exportiert dieses Dokument als PDF auf deinen Desktop. " +
                    "Die Datei wird automatisch geöffnet. " +
                    "Mit dem Ordner-Symbol links öffnest du den Speicherort.");

            exportListContainer.Add(neueZeile);
        }
    }

    // ─────────────────────────────────────────
    // AKTIONEN
    // ─────────────────────────────────────────

    private void OeffneOrdner(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) { Debug.LogWarning("[Export] Kein Pfad."); return; }

        string ordner = Directory.Exists(filePath)
            ? filePath
            : Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner))
        {
            Debug.LogWarning("[Export] Ordner nicht gefunden: " + ordner);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = ordner,
                UseShellExecute = true,
                Verb            = "open"
            });
        }
        catch (Exception e)
        {
            Debug.LogError("[Export] Ordner-Fehler: " + e.Message);
        }
    }

    private void ExportierePDF(DokumentExportEintrag eintrag)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string zeitstempel = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Dateiname aus erster Zeile des Titels
        string dateiname = eintrag.bezeichnung.Split('\n')[0].Trim();
        if (dateiname.Length > 50) dateiname = dateiname.Substring(0, 50);

        string zielPfad = Path.Combine(desktopPath, dateiname + "_" + zeitstempel + ".pdf");

        try
        {
            if (eintrag.isPDF && currentDb != null)
            {
                // PDF aus Datenbank exportieren
                bool success = PDFStorage.ExportPDFById(
                    eintrag.pdfId, currentUserId, currentDb, zielPfad);

                if (!success)
                {
                    Debug.LogError("[Export] DB-Export fehlgeschlagen: " + eintrag.bezeichnung);
                    return;
                }
            }
            else
            {
                // JSON-Dokument als PDF mit korrekten Zeilenumbruechen
                ErstellePDF(zielPfad, eintrag);
            }

            Debug.Log("[Export] PDF gespeichert: " + zielPfad);

            // Datei direkt oeffnen
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = zielPfad,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Debug.LogError("[Export] Export fehlgeschlagen: " + e.Message);
        }
    }

    private void ErstellePDF(string pfad, DokumentExportEintrag eintrag)
    {
        using (var fs = new FileStream(pfad, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var document = new iTextSharp.text.Document();
            iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
            document.Open();

            // Fonts
            var titelFont = iTextSharp.text.FontFactory.GetFont(
                iTextSharp.text.FontFactory.HELVETICA_BOLD, 16);
            var subFont = iTextSharp.text.FontFactory.GetFont(
                iTextSharp.text.FontFactory.HELVETICA_OBLIQUE, 10);
            var textFont = iTextSharp.text.FontFactory.GetFont(
                iTextSharp.text.FontFactory.HELVETICA, 12);

            // Kategorie als Header
            document.Add(new iTextSharp.text.Paragraph(
                "Kategorie: " + eintrag.art, titelFont));
            document.Add(new iTextSharp.text.Paragraph(
                "Exportiert: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"), subFont));
            document.Add(new iTextSharp.text.Paragraph(" "));

            // Trennlinie
            var linie = new iTextSharp.text.pdf.draw.LineSeparator();
            document.Add(new iTextSharp.text.Chunk(linie));
            document.Add(new iTextSharp.text.Paragraph(" "));

            // Inhalt – jede Zeile als eigener Absatz
            string[] zeilen = eintrag.bezeichnung.Split(
                new[] { '\n', '\r' },
                StringSplitOptions.None);

            foreach (string zeile in zeilen)
            {
                if (string.IsNullOrEmpty(zeile.Trim()))
                    document.Add(new iTextSharp.text.Paragraph(" "));
                else
                    document.Add(new iTextSharp.text.Paragraph(zeile, textFont));
            }

            document.Close();
        }
    }

    private void RegistriereHelpTooltips(VisualElement root)
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier exportierst du alle deine Dokumente und PDFs. " +
            "Jeder Eintrag kann einzeln als PDF gespeichert werden. " +
            "Mit dem Ordner-Button öffnest du den letzten Speicherort.");

        HelpTooltip.Registriere(root, "btn-help-bezeichnung",
            "Name des Dokuments oder der Datei. " +
            "Entspricht dem Titel wie er in der Dokumentenablage hinterlegt ist.");

        HelpTooltip.Registriere(root, "btn-help-art",
            "Kategorie des Dokuments, z. B. Gründung, Finanzen oder Bezahlweise. " +
            "Gibt an woher das Dokument stammt.");

        HelpTooltip.Registriere(root, "btn-help-datum",
            "Datum der letzten Änderung. Klicke auf die Spaltenüberschrift " +
            "um die Liste danach zu sortieren.");

        HelpTooltip.Registriere(root, "btn-help-format",
            "Dateiformat des Exports. " +
            "Aktuell werden alle Dokumente als PDF exportiert.");

        HelpTooltip.Registriere(root, "btn-help-pfad",
            "Letzter Speicherort der exportierten Datei. " +
            "Klicke den Ordner-Button in einer Zeile um den Ordner zu öffnen.");
    }

}