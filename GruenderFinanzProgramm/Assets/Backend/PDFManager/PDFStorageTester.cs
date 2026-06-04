using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class PDFStorageTester : MonoBehaviour
{
    [Header("Test PDF Pfad")]
    public string testPdfPath;

    [Header("Test User")]
    public int testUserId = 1;

    private DataBase db;

    private List<UserPDFDocument> pdfs = new();

    private void Start()
    {
        Debug.Log("PDF Test gestartet.");
        Debug.Log("Persistent Data Path: " + Application.persistentDataPath);

        db = GlobalDatabaseManager.Instance
            .GetOrCreateDatabase<DataBase>("TestUserDB");

        db.setupDatabase();

        RefreshPDFList();
    }

    public void TestSavePDF()
    {
        if (string.IsNullOrEmpty(testPdfPath))
        {
            Debug.LogError("Kein testPdfPath gesetzt.");
            return;
        }

        UserPDFDocument pdf =
            PDFStorage.SavePDF(
                testPdfPath,
                testUserId,
                db
            );

        if (pdf != null)
        {
            Debug.Log("PDF gespeichert:");
            Debug.Log("ID: " + pdf.id);
            Debug.Log("Pfad: " + pdf.filePath);
        }

        RefreshPDFList();
    }

    public void RefreshPDFList()
    {
        pdfs = db.getPDFDocumentsByUser(testUserId);

        Debug.Log("===== PDFs des Nutzers =====");

        foreach (UserPDFDocument pdf in pdfs)
        {
            Debug.Log(
                $"ID: {pdf.id} | " +
                $"Name: {pdf.originalFileName} | " +
                $"Pfad: {pdf.filePath}"
            );
        }
    }

    public void TestDeletePDFById(int pdfId)
    {
        bool deleted =
            PDFStorage.DeletePDFById(
                pdfId,
                testUserId,
                db
            );

        Debug.Log(
            deleted
                ? $"PDF mit ID {pdfId} gelöscht."
                : $"PDF mit ID {pdfId} konnte nicht gelöscht werden."
        );

        RefreshPDFList();
    }

    public void TestExportPDFById(int pdfId)
    {
        string desktopPath =
            System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop
            );

        UserPDFDocument pdf =
            db.getUserPDFDocumentById(pdfId, testUserId);

        if (pdf == null)
        {
            Debug.LogError("PDF nicht gefunden.");
            return;
        }

        string exportPath =
            Path.Combine(desktopPath, pdf.originalFileName);

      bool success =
        PDFStorage.ExportPDFById(
        pdfId,
        testUserId,
        db,
        exportPath
    );

        Debug.Log(
            success
                ? $"PDF exportiert nach: {exportPath}"
                : "Export fehlgeschlagen."
        );
    }
}