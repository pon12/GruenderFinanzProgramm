using UnityEngine;
using System.IO;

public class PDFStorageTester : MonoBehaviour
{
    private PDFEntry currentPdf;

    [Header("Test PDF Pfad")]
    public string testPdfPath;

    private void Start()
    {
        Debug.Log("PDF Test gestartet.");
        Debug.Log("Persistent Data Path: " + Application.persistentDataPath);
    }

    public void TestSavePDF()
    {
        if (string.IsNullOrEmpty(testPdfPath))
        {
            Debug.LogError("Kein testPdfPath gesetzt.");
            return;
        }

        currentPdf = PDFStorage.SavePDF(testPdfPath, "Rechnungen");

        if (currentPdf != null)
        {
            Debug.Log("PDF gespeichert:");
            Debug.Log(currentPdf.filePath);
        }
    }

    public void TestListPDFs()
    {
        var pdfs = PDFManager.GetAllPDFs();

        Debug.Log("Gefundene PDFs: " + pdfs.Count);

        foreach (var pdf in pdfs)
        {
            Debug.Log(pdf.fileName + " | " + pdf.filePath);
        }
    }

    public void TestDeletePDF()
    {
        if (currentPdf == null)
        {
            Debug.LogError("Keine PDF zum Löschen gespeichert.");
            return;
        }

        bool deleted = PDFStorage.DeletePDF(currentPdf);

        Debug.Log(deleted ? "PDF gelöscht." : "PDF konnte nicht gelöscht werden.");
    }

    public void TestExportPDF()
    {
        if (currentPdf == null)
        {
            Debug.LogError("Keine PDF zum Exportieren gespeichert.");
            return;
        }

        string desktopPath = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.Desktop
        );

        string exportPath = Path.Combine(desktopPath, currentPdf.fileName);

        string result = PDFStorage.ExportPDF(currentPdf, exportPath);

        Debug.Log(result != null
            ? "PDF exportiert nach: " + result
            : "Export fehlgeschlagen.");
    }
}