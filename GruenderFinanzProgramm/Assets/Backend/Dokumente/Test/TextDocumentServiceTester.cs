using UnityEngine;

public class TextDocumentServiceTester : MonoBehaviour
{
    private DataBase db;
    private int testUserId = 1;

    private void Start()
    {
        Debug.Log("===== TextDocumentServiceTester gestartet =====");

        db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            Debug.LogError("[TextDocumentServiceTester] Keine aktive Nutzer-Datenbank gefunden.");
            return;
        }

        db.setupDatabase();

        RunFullTextDocumentTest();
    }

    private void RunFullTextDocumentTest()
    {
        // 1. Textdokument erstellen
        TextDocumentMeta document = TextDocumentService.CreateTextDocument(
            testUserId,
            db,
            "Testdokument",
            "testdokument.txt",
            "Das ist ein Testinhalt.\n\nDieser Text soll später ohne Meta-Zeilen gelesen werden.",
            TextDocumentService.TYPE_STANDARD
        );

        if (document == null)
        {
            Debug.LogError("[TextDocumentServiceTester] Erstellen fehlgeschlagen.");
            return;
        }

        Debug.Log("[TEST] Dokument erstellt:");
        Debug.Log("ID: " + document.id);
        Debug.Log("Pfad: " + document.filePath);
        Debug.Log("Typ: " + document.documentType);

        // 2. Textdokument lesen
        ParsedTextDocument parsedDocument = TextDocumentService.ReadTextDocument(document);

        if (parsedDocument == null)
        {
            Debug.LogError("[TextDocumentServiceTester] Lesen fehlgeschlagen.");
            return;
        }

        Debug.Log("[TEST] Dokument gelesen:");
        Debug.Log("Typ: " + parsedDocument.documentType);
        Debug.Log("Titel: " + parsedDocument.title);
        Debug.Log("Plain Text: " + parsedDocument.plainText);

        // 3. Textdokument aktualisieren
        bool updated = TextDocumentService.UpdateTextDocument(
            document,
            db,
            "Testdokument Aktualisiert",
            "Das ist der aktualisierte Inhalt.\n\nAuch dieser Text soll sauber gelesen werden.",
            TextDocumentService.TYPE_STANDARD
        );

        Debug.Log("[TEST] Dokument aktualisiert: " + updated);

        if (!updated)
        {
            return;
        }

        ParsedTextDocument updatedParsedDocument = TextDocumentService.ReadTextDocument(document);

        Debug.Log("[TEST] Aktualisierter Inhalt:");
        Debug.Log("Titel: " + updatedParsedDocument.title);
        Debug.Log("Plain Text: " + updatedParsedDocument.plainText);

        // 4. Textdokument als PDF exportieren
        UserPDFDocument pdfDocument = TextDocumentPdfExporter.ExportTextDocumentToPdf(
            document,
            testUserId,
            db
        );

        if (pdfDocument == null)
        {
            Debug.LogError("[TextDocumentServiceTester] PDF-Export fehlgeschlagen.");
            return;
        }

        Debug.Log("[TEST] PDF erstellt:");
        Debug.Log("PDF-Pfad: " + pdfDocument.filePath);
        Debug.Log("PDF-Kategorie: " + pdfDocument.category);

        // 5. Textdokument löschen
        bool deleted = TextDocumentService.DeleteTextDocument(
            document.id,
            testUserId,
            db
        );

        Debug.Log("[TEST] Textdokument gelöscht: " + deleted);

        Debug.Log("===== TextDocumentServiceTester fertig =====");
    }
}