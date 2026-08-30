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

    private List<UserPDFDocument> pdfListe = new List<UserPDFDocument>();
    private List<DokumentExportEintrag> dokListe = new List<DokumentExportEintrag>();

    private class DokumentExportEintrag
    {
        public string bezeichnung;
        public string art;
        public string filePath;
        public bool isPDF;
        public int pdfId;
        public string datum;
        public DateTime datumSort;

        // Nur bei JSON-Dokumenten gesetzt
        // für den strukturierten PDF-Export
        public DocumentDashboard.DocumentData quelleDokument;
        // FIX: true, sobald filePath auf einen echten, existierenden
        // Speicherort zeigt (PDFs aus der DB haben das von Anfang an,
        // JSON-Dokumente erst nach dem ersten Export). Verhindert, dass
        // "Ordner oeffnen" vorher das Stammverzeichnis anzeigt.
        public bool hatEchtenPfad;
    }

    // =========================================================
    // SORTIERUNG
    // =========================================================

    private string _sortColumn = "Datum";
    private bool _sortAscending = false;

    private Label _hBezeichnung;
    private Label _hArt;
    private Label _hDatum;

    private static readonly Color HeaderGruen =
        new Color(128f / 255f, 207f / 255f, 149f / 255f);

    // =========================================================
    // LEBENSZYKLUS
    // =========================================================

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[Export] UIDocument nicht gefunden.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        _root = root;

        exportListContainer =
            root.Q<ScrollView>("export-list-container");

        RegistriereSortHeader(root);

        LadeAlleDaten();

        RegistriereHelpTooltips(root);
    }

    // =========================================================
    // SORTIERUNG
    // =========================================================

    private void RegistriereSortHeader(VisualElement root)
    {
        _hBezeichnung = root.Q<Label>("header-name");
        _hArt = root.Q<Label>("header-type");
        _hDatum = root.Q<Label>("header-datum");

        RegistriereEinzelnenHeader(
            _hBezeichnung,
            "Bezeichnung"
        );

        RegistriereEinzelnenHeader(
            _hArt,
            "Art"
        );

        RegistriereEinzelnenHeader(
            _hDatum,
            "Datum"
        );

        AktualisiereHeaderPfeile();
    }

    private void RegistriereEinzelnenHeader(
        Label lbl,
        string columnKey
    )
    {
        if (lbl == null)
            return;

        lbl.pickingMode = PickingMode.Position;

        lbl.RegisterCallback<ClickEvent>(
            _ => SetSortColumn(columnKey)
        );

        lbl.RegisterCallback<MouseEnterEvent>(
            _ => lbl.style.color = Color.white
        );

        lbl.RegisterCallback<MouseLeaveEvent>(
            _ => lbl.style.color = HeaderGruen
        );
    }

    private void SetSortColumn(string spalte)
    {
        if (_sortColumn == spalte)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = spalte;
            _sortAscending = spalte != "Datum";
        }

        AktualisiereHeaderPfeile();

        RefreshExportListe();
    }

    private void AktualisiereHeaderPfeile()
    {
        string pfeil = _sortAscending ? " ↑" : " ↓";

        void Aktualisiere(
            Label lbl,
            string key,
            string basisText
        )
        {
            if (lbl == null)
                return;

            bool aktiv = _sortColumn == key;

            lbl.text =
                aktiv
                    ? basisText + pfeil
                    : basisText;

            lbl.style.color = HeaderGruen;
        }

        Aktualisiere(
            _hBezeichnung,
            "Bezeichnung",
            "Bezeichnung"
        );

        Aktualisiere(
            _hArt,
            "Art",
            "Art"
        );

        Aktualisiere(
            _hDatum,
            "Datum",
            "Datum"
        );
    }

    private IEnumerable<DokumentExportEintrag> SortiereEintraege()
    {
        IEnumerable<DokumentExportEintrag> sortiert =
            _sortColumn switch
            {
                "Bezeichnung" =>
                    dokListe.OrderBy(
                        d => d.bezeichnung,
                        StringComparer.OrdinalIgnoreCase
                    ),

                "Art" =>
                    dokListe.OrderBy(
                        d => d.art,
                        StringComparer.OrdinalIgnoreCase
                    ),

                "Datum" =>
                    dokListe.OrderBy(
                        d => d.datumSort
                    ),

                _ =>
                    dokListe.OrderByDescending(
                        d => d.datumSort
                    )
            };

        return _sortAscending
            ? sortiert
            : sortiert.Reverse();
    }

    // =========================================================
    // DOKUMENTEN-ORDNER
    // =========================================================

    private string GetDokumenteOrdner()
    {
        try
        {
            if (StateManager.Instance == null)
            {
                Debug.LogWarning(
                    "[Export] StateManager nicht vorhanden."
                );

                return null;
            }

            PassKeyRecord currentUser =
                StateManager.Instance.getCurrentUser();

            if (currentUser == null)
            {
                Debug.LogWarning(
                    "[Export] Kein eingeloggter Nutzer."
                );

                return null;
            }

            string username = currentUser.username;

            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.LogWarning(
                    "[Export] Benutzername ist leer."
                );

                return null;
            }

            // Ungültige Zeichen aus dem Benutzernamen entfernen
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                username = username.Replace(c, '_');
            }

            string ordner = Path.Combine(
                Application.persistentDataPath,
                "PDFs",
                username,
                "Dokumente"
            );

            Directory.CreateDirectory(ordner);

            return ordner;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Export] Fehler beim Ermitteln des Dokumente-Ordners: "
                + e
            );

            return null;
        }
    }

    // =========================================================
    // DATEN LADEN
    // =========================================================

    private void LadeAlleDaten()
    {
        dokListe.Clear();

        LadePDFsAusDatenbank();

        LadeDokumenteAusJSON();

        RefreshExportListe();
    }

    private void LadePDFsAusDatenbank()
    {
        currentDb =
            UserDatabaseAccess.getCurrentUserDatabase();

        if (currentDb == null)
        {
            Debug.LogWarning(
                "[Export] Keine aktive DB."
            );

            return;
        }

        if (
            StateManager.Instance == null ||
            !StateManager.Instance.isLoggedIn()
        )
        {
            Debug.LogWarning(
                "[Export] Kein eingeloggter Nutzer."
            );

            return;
        }

        PassKeyRecord currentUser =
            StateManager.Instance.getCurrentUser();

        if (currentUser == null)
            return;

        string rawUserId =
            currentUser.userId.Replace("user_", "");

        if (!int.TryParse(
            rawUserId,
            out currentUserId
        ))
        {
            Debug.LogError(
                "[Export] Ungültige User-ID: "
                + currentUser.userId
            );

            return;
        }

        try
        {
            pdfListe =
                currentDb.getPDFDocumentsByUser(
                    currentUserId
                )
                ?? new List<UserPDFDocument>();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Export] PDF-Ladefehler: "
                + e.Message
            );

            pdfListe =
                new List<UserPDFDocument>();
        }

        foreach (UserPDFDocument pdf in pdfListe)
        {
            dokListe.Add(new DokumentExportEintrag
            {
                bezeichnung = pdf.originalFileName,
                art = string.IsNullOrEmpty(pdf.category) ? "Dokument" : pdf.category,
                filePath = pdf.filePath,
                isPDF = true,
                pdfId = pdf.id,
                datum = pdf.uploadedAt.ToString("dd.MM.yyyy"),
                datumSort = pdf.uploadedAt,
                hatEchtenPfad = true
            });
        }

        Debug.Log(
            $"[Export] {pdfListe.Count} PDFs geladen."
        );
    }

    private void LadeDokumenteAusJSON()
    {
        try
        {
            var saveData =
                DocumentDashboard.GetSavedDocuments();

            if (
                saveData == null ||
                saveData.savedDocs == null
            )
            {
                return;
            }

            // WICHTIG:
            // Alle normalen Dokumente werden im selben Ordner
            // gespeichert wie die von DokumentPdfGenerator erzeugten PDFs.
            string dokumenteOrdner =
                GetDokumenteOrdner();

            if (string.IsNullOrEmpty(dokumenteOrdner))
            {
                Debug.LogError(
                    "[Export] Dokumente-Ordner konnte nicht ermittelt werden."
                );

                return;
            }

            foreach (
                var doc in saveData.savedDocs
            )
            {
                DateTime datumSort =
                    DateTime.MinValue;

                string datumAnzeige = "–";

                if (
                    !string.IsNullOrEmpty(doc.datum) &&
                    DateTime.TryParse(
                        doc.datum,
                        out DateTime geparst
                    )
                )
                {
                    datumSort = geparst;

                    datumAnzeige =
                        geparst.ToString(
                            "dd.MM.yyyy"
                        );
                }

                dokListe.Add(
    new DokumentExportEintrag
    {
        bezeichnung = doc.title,
        art = doc.category,
        filePath = dokumenteOrdner,
        isPDF = false,
        pdfId = -1,
        datum = datumAnzeige,
        datumSort = datumSort,
        quelleDokument = doc,
        hatEchtenPfad = true
    }
);
            }

            Debug.Log(
                $"[Export] {saveData.savedDocs.Count} Dokumente aus JSON geladen."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Export] JSON-Ladefehler: "
                + e.Message
            );
        }
    }

    // =========================================================
    // LISTE RENDERN
    // =========================================================

    private void RefreshExportListe()
    {
        if (exportListContainer == null)
        {
            Debug.LogError(
                "[Export] export-list-container fehlt."
            );

            return;
        }

        if (exportZeileTemplate == null)
        {
            Debug.LogError(
                "[Export] exportZeileTemplate fehlt."
            );

            return;
        }

        exportListContainer.Clear();

        if (dokListe.Count == 0)
        {
            Debug.Log(
                "[Export] Keine Eintraege."
            );

            return;
        }

        foreach (
            DokumentExportEintrag eintrag
            in SortiereEintraege()
        )
        {
            VisualElement neueZeile =
                exportZeileTemplate.Instantiate();

            Label lblBezeichnung =
                neueZeile.Q<Label>(
                    "row-bezeichnung"
                );

            Label lblArt =
                neueZeile.Q<Label>(
                    "row-art"
                );

            Label lblDatum =
                neueZeile.Q<Label>(
                    "row-datum"
                );

            DropdownField dropdown =
                neueZeile.Q<DropdownField>(
                    "format-dropdown"
                );

            Button btnFolder =
                neueZeile.Q<Button>(
                    "btn-open-folder"
                );

            Button btnExport =
                neueZeile.Q<Button>(
                    "btn-export"
                );

            string anzeigeText =
                eintrag.bezeichnung
                    .Split('\n')[0];

            if (anzeigeText.Length > 40)
            {
                anzeigeText =
                    anzeigeText.Substring(
                        0,
                        37
                    )
                    + "...";
            }

            if (lblBezeichnung != null)
                lblBezeichnung.text =
                    anzeigeText;

            if (lblArt != null)
                lblArt.text =
                    eintrag.art;

            if (lblDatum != null)
                lblDatum.text =
                    eintrag.datum;

            if (dropdown != null)
            {
                dropdown.choices =
                    new List<string>
                    {
                        "PDF"
                    };

                dropdown.value =
                    "PDF";

                dropdown.SetEnabled(false);
            }

            DokumentExportEintrag lokalerEintrag =
                eintrag;

            if (btnFolder != null)
            {
                // FIX: Solange kein echter Speicherort existiert (JSON-Dokument
                // vor dem ersten Export), Button deaktivieren statt auf das
                // Stammverzeichnis zu zeigen.
                btnFolder.SetEnabled(lokalerEintrag.hatEchtenPfad);
                btnFolder.tooltip = lokalerEintrag.hatEchtenPfad
                    ? ""
                    : "Noch kein Speicherort - erst als PDF exportieren.";
                btnFolder.clicked += () => OeffneOrdner(lokalerEintrag.filePath);
            }

            if (btnExport != null)
            {
                btnExport.clicked +=
                    () => ExportierePDF(
                        lokalerEintrag
                    );
            }

            var zeilenHelp =
                neueZeile.Q<VisualElement>(
                    "btn-help-export-btn"
                );

            if (zeilenHelp != null)
            {
                HelpTooltip.RegistriereInKarte(
                    _root,
                    zeilenHelp,
                    "Exportiert dieses Dokument als PDF. " +
                    "Die Datei wird im zugehörigen PDF-Ordner gespeichert. " +
                    "Mit dem Ordner-Symbol links öffnest du den Speicherort."
                );
            }

            exportListContainer.Add(
                neueZeile
            );
        }
    }

    // =========================================================
    // ORDNER ÖFFNEN
    // =========================================================

    private void OeffneOrdner(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning(
                "[Export] Kein Pfad."
            );

            return;
        }

        string ordner;

        if (Directory.Exists(filePath))
        {
            ordner = filePath;
        }
        else
        {
            ordner =
                Path.GetDirectoryName(
                    filePath
                );
        }

        if (
            string.IsNullOrEmpty(ordner) ||
            !Directory.Exists(ordner)
        )
        {
            Debug.LogWarning(
                "[Export] Ordner nicht gefunden: "
                + ordner
            );

            return;
        }

        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ordner,
                    UseShellExecute = true,
                    Verb = "open"
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Export] Ordner-Fehler: "
                + e.Message
            );
        }
    }

    // =========================================================
    // PDF EXPORTIEREN
    // =========================================================

    private void ExportierePDF(
        DokumentExportEintrag eintrag
    )
    {
        try
        {
            // =================================================
            // 1. BEREITS VORHANDENE PDF AUS DER DATENBANK
            // =================================================

            if (
                eintrag.isPDF &&
                currentDb != null
            )
            {
                if (
                    string.IsNullOrEmpty(
                        eintrag.filePath
                    )
                )
                {
                    Debug.LogError(
                        "[Export] Kein PDF-Pfad vorhanden: "
                        + eintrag.bezeichnung
                    );

                    return;
                }

                if (
                    !File.Exists(
                        eintrag.filePath
                    )
                )
                {
                    Debug.LogError(
                        "[Export] PDF-Datei nicht gefunden: "
                        + eintrag.filePath
                    );

                    return;
                }

                // Das bereits gespeicherte PDF liegt bereits
                // im korrekten Benutzerordner.
                //
                // Deshalb wird es NICHT mehr auf den Desktop kopiert.

                Debug.Log(
                    "[Export] PDF bereits vorhanden: "
                    + eintrag.filePath
                );

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName =
                            eintrag.filePath,

                        UseShellExecute = true
                    }
                );

                return;
            }

            // =================================================
            // 2. NORMALES DOKUMENT AUS DOKUMENTE-SCREEN
            // =================================================

            if (
                eintrag.quelleDokument != null
            )
            {
                string generiertPfad =
                    DokumentPdfGenerator
                        .ErstellePdfFuerDokument(
                            eintrag.quelleDokument
                        );

                if (
                    !string.IsNullOrEmpty(
                        generiertPfad
                    ) &&
                    File.Exists(
                        generiertPfad
                    )
                )
                {
                    // Der DokumentPdfGenerator erzeugt die PDF
                    // bereits direkt in:
                    //
                    // PDFs/<Username>/Dokumente
                    //
                    // Deshalb KEIN Kopieren auf den Desktop
                    // und KEIN zweites PDF erzeugen.

                    Debug.Log(
                        "[Export] Dokument-PDF erzeugt: "
                        + generiertPfad
                    );

                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName =
                                generiertPfad,

                            UseShellExecute = true
                        }
                    );

                    return;
                }
            }

            // =================================================
            // 3. FALLBACK
            // =================================================

            string dokumenteOrdner =
                GetDokumenteOrdner();

            if (
                string.IsNullOrEmpty(
                    dokumenteOrdner
                )
            )
            {
                Debug.LogError(
                    "[Export] Dokumente-Ordner konnte nicht ermittelt werden."
                );

                return;
            }

            string zeitstempel =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"
                );

            string dateiname =
                eintrag.bezeichnung
                    .Split('\n')[0]
                    .Trim();

            if (dateiname.Length > 50)
            {
                dateiname =
                    dateiname.Substring(
                        0,
                        50
                    );
            }

            foreach (
                char c
                in Path.GetInvalidFileNameChars()
            )
            {
                dateiname =
                    dateiname.Replace(
                        c,
                        '_'
                    );
            }

            string zielPfad =
                Path.Combine(
                    dokumenteOrdner,
                    dateiname
                    + "_"
                    + zeitstempel
                    + ".pdf"
                );

            ErstellePDF(
                zielPfad,
                eintrag
            );

            Debug.Log(
                "[Export] PDF gespeichert: "
                + zielPfad
            );

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName =
                        zielPfad,

                    UseShellExecute = true
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Export] Export fehlgeschlagen: "
                + e
            );
        }
    }

    // =========================================================
    // FALLBACK-PDF
    // =========================================================

    private void ErstellePDF(
        string pfad,
        DokumentExportEintrag eintrag
    )
    {
        using (
            var fs =
                new FileStream(
                    pfad,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                )
        )
        {
            var document =
                new iTextSharp.text.Document();

            iTextSharp.text.pdf.PdfWriter
                .GetInstance(
                    document,
                    fs
                );

            document.Open();

            // Fonts
            var titelFont =
                iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_BOLD,
                    16
                );

            var subFont =
                iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA_OBLIQUE,
                    10
                );

            var textFont =
                iTextSharp.text.FontFactory.GetFont(
                    iTextSharp.text.FontFactory.HELVETICA,
                    12
                );

            // Kategorie
            document.Add(
                new iTextSharp.text.Paragraph(
                    "Kategorie: "
                    + eintrag.art,
                    titelFont
                )
            );

            document.Add(
                new iTextSharp.text.Paragraph(
                    "Exportiert: "
                    + DateTime.Now.ToString(
                        "dd.MM.yyyy HH:mm"
                    ),
                    subFont
                )
            );

            document.Add(
                new iTextSharp.text.Paragraph(
                    " "
                )
            );

            // Trennlinie
            var linie =
                new iTextSharp.text.pdf.draw.LineSeparator();

            document.Add(
                new iTextSharp.text.Chunk(
                    linie
                )
            );

            document.Add(
                new iTextSharp.text.Paragraph(
                    " "
                )
            );

            // Inhalt
            string[] zeilen =
                eintrag.bezeichnung.Split(
                    new[]
                    {
                        '\n',
                        '\r'
                    },
                    StringSplitOptions.None
                );

            foreach (
                string zeile
                in zeilen
            )
            {
                if (
                    string.IsNullOrEmpty(
                        zeile.Trim()
                    )
                )
                {
                    document.Add(
                        new iTextSharp.text.Paragraph(
                            " "
                        )
                    );
                }
                else
                {
                    document.Add(
                        new iTextSharp.text.Paragraph(
                            zeile,
                            textFont
                        )
                    );
                }
            }

            document.Close();
        }
    }

    // =========================================================
    // HILFETEXTE
    // =========================================================

    private void RegistriereHelpTooltips(
        VisualElement root
    )
    {
        HelpTooltip.Registriere(
            root,
            "btn-help-seitentitel",
            "Hier exportierst du alle deine Dokumente und PDFs. " +
            "Jeder Eintrag kann einzeln als PDF gespeichert werden. " +
            "Mit dem Ordner-Button öffnest du den Speicherort."
        );

        HelpTooltip.Registriere(
            root,
            "btn-help-bezeichnung",
            "Name des Dokuments oder der Datei. " +
            "Entspricht dem Titel wie er in der Dokumentenablage hinterlegt ist."
        );

        HelpTooltip.Registriere(
            root,
            "btn-help-art",
            "Kategorie des Dokuments, z. B. Gründung, Finanzen oder Bezahlweise. " +
            "Gibt an woher das Dokument stammt."
        );

        HelpTooltip.Registriere(
            root,
            "btn-help-datum",
            "Datum der letzten Änderung. Klicke auf die Spaltenüberschrift " +
            "um die Liste danach zu sortieren."
        );

        HelpTooltip.Registriere(
            root,
            "btn-help-format",
            "Dateiformat des Exports. " +
            "Aktuell werden alle Dokumente als PDF exportiert."
        );

        HelpTooltip.Registriere(
            root,
            "btn-help-pfad",
            "Speicherort der Dokument-PDF. " +
            "Klicke den Ordner-Button in einer Zeile um den Ordner zu öffnen."
        );
    }
}