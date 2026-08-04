using System;
using System.IO;
using UnityEngine;

public static class TextDocumentService
{
    public const string TYPE_STANDARD = "STANDARD";
    public const string TYPE_DIAGRAMM = "DIAGRAMM";
    public const string TYPE_CHECKLIST = "CHECKLIST";

    public static TextDocumentMeta CreateTextDocument(
        int userId,
        DataBase db,
        string title,
        string fileName,
        string content,
        string documentType = TYPE_STANDARD
    )
    {
        if (db == null)
        {
            Debug.LogError("[TextDocument] Keine Datenbank übergeben.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            Debug.LogError("[TextDocument] Titel darf nicht leer sein.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = title;
        }

        try
        {
            documentType = NormalizeDocumentType(documentType);

            string folderPath = GetUserTextDocumentFolder(userId, documentType);
            Directory.CreateDirectory(folderPath);

            string safeFileName = MakeSafeFileName(fileName);

            if (!safeFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                safeFileName += ".txt";
            }

            string storedFileName = Guid.NewGuid().ToString() + ".txt";
            string filePath = Path.Combine(folderPath, storedFileName);

            string fileContent =
                "[DOCTYPE " + documentType + "]" + Environment.NewLine +
                "[TITLE " + title + "]" + Environment.NewLine +
                Environment.NewLine +
                (content ?? "");

            File.WriteAllText(filePath, fileContent);

            TextDocumentMeta meta = new TextDocumentMeta
            {
                userId = userId,
                title = title,
                originalFileName = safeFileName,
                storedFileName = storedFileName,
                filePath = filePath,
                documentType = documentType,
                createdAt = DateTime.Now,
                lastUpdated = DateTime.Now
            };

            db.createTextDocumentMeta(meta);

            Debug.Log("[TextDocument] Datei erstellt: " + filePath);

            return meta;
        }
        catch (Exception exception)
        {
            Debug.LogError("[TextDocument] Fehler beim Erstellen: " + exception.Message);
            return null;
        }
    }

    public static ParsedTextDocument ReadTextDocument(TextDocumentMeta document)
    {
        if (document == null)
        {
            Debug.LogError("[TextDocumentService] Dokument ist null.");
            return null;
        }

        if (string.IsNullOrEmpty(document.filePath))
        {
            Debug.LogError("[TextDocumentService] Dokument hat keinen Dateipfad.");
            return null;
        }

        if (!File.Exists(document.filePath))
        {
            Debug.LogError("[TextDocumentService] Datei existiert nicht: " + document.filePath);
            return null;
        }

        return TextDocumentParser.ParseTextDocument(document.filePath);
    }

    public static string ReadPlainText(TextDocumentMeta document)
    {
        ParsedTextDocument parsedDocument = ReadTextDocument(document);

        if (parsedDocument == null)
        {
            return "";
        }

        return parsedDocument.plainText;
    }

    public static bool UpdateTextDocument(
        TextDocumentMeta document,
        DataBase db,
        string newTitle,
        string newContent,
        string newDocumentType = TYPE_STANDARD
    )
    {
        if (document == null)
        {
            Debug.LogError("[TextDocumentService] Dokument ist null.");
            return false;
        }

        if (db == null)
        {
            Debug.LogError("[TextDocumentService] Datenbank ist null.");
            return false;
        }

        if (string.IsNullOrEmpty(document.filePath))
        {
            Debug.LogError("[TextDocumentService] Dokument hat keinen Dateipfad.");
            return false;
        }

        try
        {
            string documentType = NormalizeDocumentType(newDocumentType);

            string directoryPath = Path.GetDirectoryName(document.filePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string fileContent =
                "[DOCTYPE " + documentType + "]" + Environment.NewLine +
                "[TITLE " + (newTitle ?? "") + "]" + Environment.NewLine +
                Environment.NewLine +
                (newContent ?? "");

            File.WriteAllText(document.filePath, fileContent);

            document.title = newTitle ?? "";
            document.documentType = documentType;
            document.lastUpdated = DateTime.Now;

            db.updateTextDocumentMeta(document);

            Debug.Log("[TextDocumentService] Textdokument aktualisiert: " + document.filePath);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("[TextDocumentService] Fehler beim Aktualisieren: " + exception.Message);
            return false;
        }
    }

    public static bool DeleteTextDocument(
        int documentId,
        int userId,
        DataBase db
    )
    {
        if (db == null)
        {
            Debug.LogError("[TextDocumentService] Datenbank ist null.");
            return false;
        }

        TextDocumentMeta document = db.getTextDocumentMetaById(documentId, userId);

        if (document == null)
        {
            Debug.LogError("[TextDocumentService] Textdokument nicht gefunden oder gehört nicht diesem Nutzer.");
            return false;
        }

        try
        {
            if (!string.IsNullOrEmpty(document.filePath) && File.Exists(document.filePath))
            {
                File.Delete(document.filePath);
            }

            db.deleteTextDocumentMeta(documentId, userId);

            Debug.Log("[TextDocumentService] Textdokument gelöscht: " + documentId);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("[TextDocumentService] Fehler beim Löschen: " + exception.Message);
            return false;
        }
    }

    public static string GetUserTextDocumentFolder(int userId, string documentType)
    {
        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "TextDocuments",
            "User_" + userId,
            NormalizeDocumentType(documentType)
        );

        Directory.CreateDirectory(folderPath);

        return folderPath;
    }

    public static string NormalizeDocumentType(string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
        {
            return TYPE_STANDARD;
        }

        string upperType = documentType.Trim().ToUpper();

        if (upperType == TYPE_DIAGRAMM)
        {
            return TYPE_DIAGRAMM;
        }

        if (upperType == TYPE_CHECKLIST)
        {
            return TYPE_CHECKLIST;
        }

        return TYPE_STANDARD;
    }

    private static string MakeSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Textdokument";
        }

        string safeName = fileName.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar.ToString(), "");
        }

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "Textdokument";
        }

        return safeName;
    }
}