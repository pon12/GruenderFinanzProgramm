using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class ExportScreenController : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("UI Templates & Container")]
    [SerializeField] private VisualTreeAsset exportZeileTemplate;

    private ScrollView exportListContainer;
    private Label lblCounter;

    private DataBase currentDb;
    private int currentUserId;

    private List<UserPDFDocument> pdfListe = new List<UserPDFDocument>();

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[Export] UIDocument nicht gefunden.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        exportListContainer = root.Q<ScrollView>("export-list-container");
        lblCounter = root.Q<Label>("lbl-counter");

        LadePDFsAusDatenbank();
    }

    private void LadePDFsAusDatenbank()
    {
        pdfListe.Clear();

        currentDb = UserDatabaseAccess.getCurrentUserDatabase();

        if (currentDb == null)
        {
            Debug.LogError("[Export] Keine aktive NutzerDB gefunden.");
            RefreshExportListe();
            return;
        }

        if (StateManager.Instance == null || !StateManager.Instance.isLoggedIn())
        {
            Debug.LogError("[Export] Kein eingeloggter Nutzer gefunden.");
            RefreshExportListe();
            return;
        }

        PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();

        if (currentUser == null)
        {
            Debug.LogError("[Export] currentUser ist null.");
            RefreshExportListe();
            return;
        }

        string rawUserId = currentUser.userId;

        if (rawUserId.StartsWith("user_"))
        {
            rawUserId = rawUserId.Replace("user_", "");
        }

        if (!int.TryParse(rawUserId, out currentUserId))
        {
            Debug.LogError("[Export] UserId ungültig: " + currentUser.userId);
            RefreshExportListe();
            return;
        }

        pdfListe = currentDb.getPDFDocumentsByUser(currentUserId);

        Debug.Log("[Export] Aktuelle UserID: " + currentUser.userId);
        Debug.Log("[Export] Parsed UserID: " + currentUserId);
        Debug.Log("[Export] Echte PDFs geladen: " + pdfListe.Count);

        RefreshExportListe();
    }

    private void RefreshExportListe()
    {
        if (exportListContainer == null)
        {
            Debug.LogError("[Export] export-list-container nicht gefunden.");
            return;
        }

        if (exportZeileTemplate == null)
        {
            Debug.LogError("[Export] exportZeileTemplate nicht zugewiesen.");
            return;
        }

        exportListContainer.Clear();

        if (lblCounter != null)
        {
            lblCounter.text = pdfListe.Count == 1
                ? "1 Dokument"
                : pdfListe.Count + " Dokumente";
        }

        foreach (UserPDFDocument pdf in pdfListe)
        {
            VisualElement neueZeile = exportZeileTemplate.Instantiate();

            Label lblBezeichnung = neueZeile.Q<Label>("row-bezeichnung");
            Label lblArt = neueZeile.Q<Label>("row-art");
            DropdownField dropdownFormat = neueZeile.Q<DropdownField>("format-dropdown");
            Button btnFolder = neueZeile.Q<Button>("btn-open-folder");
            Button btnExport = neueZeile.Q<Button>("btn-export");

            if (lblBezeichnung != null)
                lblBezeichnung.text = pdf.originalFileName;

            if (lblArt != null)
                lblArt.text = string.IsNullOrEmpty(pdf.category)
                    ? "Dokument"
                    : pdf.category;

            if (dropdownFormat != null)
            {
                dropdownFormat.choices = new List<string> { "PDF" };
                dropdownFormat.value = "PDF";
                dropdownFormat.SetEnabled(false);
            }

            if (btnFolder != null)
            {
                btnFolder.clicked += () => OeffneOrdnerPfad(pdf.filePath);
            }

            if (btnExport != null)
            {
                btnExport.clicked += () => ExportierePDF(pdf);
            }

            exportListContainer.Add(neueZeile);
        }
    }

    private void OeffneOrdnerPfad(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning("[Export] Kein Dateipfad vorhanden.");
            return;
        }

        string folderPath = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Debug.LogWarning("[Export] Ordner existiert nicht: " + folderPath);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true,
                Verb = "open"
            });

            Debug.Log("[Export] Öffne Ordner: " + folderPath);
        }
        catch (Exception e)
        {
            Debug.LogError("[Export] Ordner konnte nicht geöffnet werden: " + e.Message);
        }
    }

private void ExportierePDF(UserPDFDocument pdf)
{
    if (pdf == null)
    {
        Debug.LogError("[Export] PDF ist null.");
        return;
    }

    if (currentDb == null)
    {
        Debug.LogError("[Export] Keine aktive DB.");
        return;
    }

    string desktopPath = Environment.GetFolderPath(
        Environment.SpecialFolder.Desktop
    );

    string fileNameWithoutExt =
        Path.GetFileNameWithoutExtension(pdf.originalFileName);

    string extension =
        Path.GetExtension(pdf.originalFileName);

    string destinationPath = Path.Combine(
        desktopPath,
        fileNameWithoutExt + "_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + extension
    );

    bool success = PDFStorage.ExportPDFById(
        pdf.id,
        currentUserId,
        currentDb,
        destinationPath
    );

    Debug.Log(
        success
            ? "[Export] PDF exportiert nach: " + destinationPath
            : "[Export] Export fehlgeschlagen: " + pdf.originalFileName
    );
}
}