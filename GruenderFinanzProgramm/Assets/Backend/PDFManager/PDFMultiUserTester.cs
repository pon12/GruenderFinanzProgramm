using System.Collections.Generic;
using UnityEngine;

public class PDFMultiUserTester : MonoBehaviour
{
    public string testPdfPath;

    private DataBase user1Db;
    private DataBase user2Db;

    private int user1Id = 1;
    private int user2Id = 2;

    private void Start()
    {
        user1Db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Nutzer1");
        user2Db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Nutzer2");

        user1Db.setupDatabase();
        user2Db.setupDatabase();

        Debug.Log("Multi-User PDF Test bereit.");
    }

    public void SaveForUser1()
    {
        PDFStorage.SavePDF(testPdfPath, user1Id, user1Db, "Rechnungen");
        Debug.Log("PDF für Nutzer 1 gespeichert.");
    }

    public void SaveForUser2()
    {
        PDFStorage.SavePDF(testPdfPath, user2Id, user2Db, "Angebote");
        Debug.Log("PDF für Nutzer 2 gespeichert.");
    }

    public void ListUser1PDFs()
    {
        List<UserPDFDocument> pdfs = user1Db.getPDFDocumentsByUser(user1Id);

        Debug.Log("===== PDFs Nutzer 1 =====");

        foreach (UserPDFDocument pdf in pdfs)
        {
            Debug.Log($"ID: {pdf.id} | Name: {pdf.originalFileName} | Kategorie: {pdf.category} | Pfad: {pdf.filePath}");
        }
    }

    public void ListUser2PDFs()
    {
        List<UserPDFDocument> pdfs = user2Db.getPDFDocumentsByUser(user2Id);

        Debug.Log("===== PDFs Nutzer 2 =====");

        foreach (UserPDFDocument pdf in pdfs)
        {
            Debug.Log($"ID: {pdf.id} | Name: {pdf.originalFileName} | Kategorie: {pdf.category} | Pfad: {pdf.filePath}");
        }
    }
}