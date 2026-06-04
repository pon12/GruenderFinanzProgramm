using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class PDFManagementSceneTester : MonoBehaviour
{
    public TMP_InputField pdfPathInput;
    public TMP_Dropdown userDropdown;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown pdfDropdown;

    private DataBase currentDb;
    private int currentUserId;

    private List<UserPDFDocument> currentPDFs = new List<UserPDFDocument>();

    private void Start()
    {
        SetupDropdowns();
        LoadCurrentUser();
        RefreshPDFDropdown();
    }

    private void SetupDropdowns()
    {
        userDropdown.ClearOptions();
        userDropdown.AddOptions(new List<string> { "User 1", "User 2" });

        categoryDropdown.ClearOptions();
        categoryDropdown.AddOptions(new List<string>
        {
            "Uploads",
            "Rechnungen",
            "Angebote",
            "Verträge"
        });
    }

    public void OnUserChanged()
    {
        LoadCurrentUser();
        RefreshPDFDropdown();
    }

    private void LoadCurrentUser()
    {
        currentUserId = userDropdown.value + 1;

        currentDb = GlobalDatabaseManager.Instance
            .GetOrCreateDatabase<DataBase>("TestUserDB");

        currentDb.setupDatabase();

        Debug.Log("Aktueller Nutzer: " + currentUserId);
    }

    public void ImportPDFPath()
{
    string importPath = pdfPathInput.text.Trim();
    importPath = importPath.Trim('"');

    if (importPath.StartsWith("file:///"))
        importPath = importPath.Replace("file:///", "");

    importPath = System.Uri.UnescapeDataString(importPath);

    if (!File.Exists(importPath))
    {
        Debug.LogError("Import-Datei existiert nicht: " + importPath);
        return;
    }

    string category = categoryDropdown.options[categoryDropdown.value].text;

    UserPDFDocument imported = PDFStorage.SavePDF(
        importPath,
        currentUserId,
        currentDb,
        category
    );

    if (imported != null)
    {
        Debug.Log($"Importiert: ID {imported.id} | {imported.originalFileName} | {imported.category}");
        pdfPathInput.text = "";
        RefreshPDFDropdown();
    }
}

  public void SaveSelectedPDF()
{
    UserPDFDocument selected = GetSelectedPDF();

    if (selected == null)
    {
        Debug.LogError("Keine PDF ausgewählt.");
        return;
    }

    UserPDFDocument duplicated = PDFStorage.SavePDF(
        selected.filePath,
        currentUserId,
        currentDb,
        selected.category
    );

    if (duplicated != null)
    {
        Debug.Log(
            $"PDF dupliziert: Alt-ID {selected.id} → Neu-ID {duplicated.id} | {duplicated.originalFileName}"
        );

        RefreshPDFDropdown();
    }
}
    public void RefreshPDFDropdown()
    {
        currentPDFs = currentDb.getPDFDocumentsByUser(currentUserId);

        pdfDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (UserPDFDocument pdf in currentPDFs)
        {
            options.Add($"ID {pdf.id} | {pdf.originalFileName} | {pdf.category}");
        }

        if (options.Count == 0)
            options.Add("Keine PDFs gefunden");

        pdfDropdown.AddOptions(options);

        Debug.Log("PDFs geladen: " + currentPDFs.Count);
    }

    public void ExportSelectedPDF()
    {
        UserPDFDocument selected = GetSelectedPDF();

        if (selected == null)
        {
            Debug.LogError("Keine PDF ausgewählt.");
            return;
        }

        string desktop = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.Desktop
        );

        string destinationPath = Path.Combine(desktop, selected.originalFileName);

        bool success = PDFStorage.ExportPDFById(
            selected.id,
            currentUserId,
            currentDb,
            destinationPath
        );

        Debug.Log(success
            ? "Exportiert nach: " + destinationPath
            : "Export fehlgeschlagen.");
    }

    public void DeleteSelectedPDF()
    {
        UserPDFDocument selected = GetSelectedPDF();

        if (selected == null)
        {
            Debug.LogError("Keine PDF ausgewählt.");
            return;
        }

        bool success = PDFStorage.DeletePDFById(
            selected.id,
            currentUserId,
            currentDb
        );

        Debug.Log(success
            ? "Gelöscht: " + selected.originalFileName
            : "Löschen fehlgeschlagen.");

        RefreshPDFDropdown();
    }

    private UserPDFDocument GetSelectedPDF()
    {
        if (currentPDFs == null || currentPDFs.Count == 0)
            return null;

        int index = pdfDropdown.value;

        if (index < 0 || index >= currentPDFs.Count)
            return null;

        return currentPDFs[index];
    }
}