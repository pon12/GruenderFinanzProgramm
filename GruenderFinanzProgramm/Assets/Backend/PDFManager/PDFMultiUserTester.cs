using System.Collections.Generic;
using UnityEngine;

public class PDFMultiUserTester : MonoBehaviour
{
    public string testPdfPath;

    private DataBase testDb;

    private int user1Id = 1;
    private int user2Id = 2;

    private void Start()
    {
        testDb = GlobalDatabaseManager.Instance
            .GetOrCreateDatabase<DataBase>("TestUserDB");

        testDb.setupDatabase();

        Debug.Log("Multi-User PDF Test bereit.");
    }

    public void SaveForUser1()
    {
        PDFStorage.SavePDF(testPdfPath, user1Id, testDb, "Rechnungen");
        Debug.Log("PDF für Nutzer 1 gespeichert.");
    }

    public void SaveForUser2()
    {
        PDFStorage.SavePDF(testPdfPath, user2Id, testDb, "Angebote");
        Debug.Log("PDF für Nutzer 2 gespeichert.");
    }

    public void ListUser1PDFs()
    {
        List<UserPDFDocument> pdfs =
            testDb.getPDFDocumentsByUser(user1Id);

        Debug.Log("===== PDFs Nutzer 1 =====");

        foreach (UserPDFDocument pdf in pdfs)
        {
            Debug.Log($"ID: {pdf.id} | Name: {pdf.originalFileName} | Kategorie: {pdf.category} | Pfad: {pdf.filePath}");
        }
    }

    public void ListUser2PDFs()
    {
        List<UserPDFDocument> pdfs =
            testDb.getPDFDocumentsByUser(user2Id);

        Debug.Log("===== PDFs Nutzer 2 =====");

        foreach (UserPDFDocument pdf in pdfs)
        {
            Debug.Log($"ID: {pdf.id} | Name: {pdf.originalFileName} | Kategorie: {pdf.category} | Pfad: {pdf.filePath}");
        }
    }
}