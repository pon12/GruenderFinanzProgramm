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

        documentType = NormalizeDocumentType(documentType);

        string folderPath = GetUserTextDocumentFolder(userId, documentType);
        string safeFileName = MakeSafeFileName(fileName);

        if (!safeFileName.EndsWith(".txt"))
        {
            safeFileName += ".txt";
        }

        string storedFileName = Guid.NewGuid().ToString() + ".txt";
        string filePath = Path.Combine(folderPath, storedFileName);

        string fileContent =
            "[DOCTYPE " + documentType + "]" + Environment.NewLine +
            "[TITLE " + title + "]" + Environment.NewLine +
            Environment.NewLine +
            content;

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