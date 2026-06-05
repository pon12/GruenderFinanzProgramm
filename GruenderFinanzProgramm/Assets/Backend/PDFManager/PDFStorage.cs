using System;
using System.IO;
using UnityEngine;

public static class PDFStorage
{
    public static UserPDFDocument SavePDF(
        string sourcePath,
        int userId,
        DataBase db,
        string category = "Uploads"
    )
    {
        if (sourcePath.StartsWith("file:///"))
            sourcePath = sourcePath.Replace("file:///", "");

        if (!File.Exists(sourcePath))
        {
            Debug.LogError("Datei nicht gefunden: " + sourcePath);
            return null;
        }

        string userFolder = PDFUserFolderSetup.GetUserCategoryFolder(userId, category);

        string originalName = Path.GetFileName(sourcePath);
        string extension = Path.GetExtension(originalName);
        string storedName = Guid.NewGuid().ToString() + extension;
        string targetPath = Path.Combine(userFolder, storedName);

        File.Copy(sourcePath, targetPath, false);

        UserPDFDocument document = new UserPDFDocument
        {
            userId = userId,
            originalFileName = originalName,
            storedFileName = storedName,
            filePath = targetPath,
            category = category,
            uploadedAt = DateTime.Now
        };

        db.createUserPDFDocument(document);

        return document;
    }

    public static bool DeletePDFById(int pdfId, int userId, DataBase db)
    {
        UserPDFDocument document = db.getUserPDFDocumentById(pdfId, userId);

        if (document == null)
        {
            Debug.LogError("PDF nicht gefunden oder gehört nicht diesem Nutzer.");
            return false;
        }

        if (File.Exists(document.filePath))
            File.Delete(document.filePath);

        db.deleteUserPDFDocument(pdfId, userId);
        return true;
    }

    public static bool ExportPDFById(
        int pdfId,
        int userId,
        DataBase db,
        string destinationPath
    )
    {
        UserPDFDocument document = db.getUserPDFDocumentById(pdfId, userId);

        if (document == null)
        {
            Debug.LogError("PDF nicht gefunden oder gehört nicht diesem Nutzer.");
            return false;
        }

        if (!File.Exists(document.filePath))
        {
            Debug.LogError("Datei existiert nicht: " + document.filePath);
            return false;
        }

        File.Copy(document.filePath, destinationPath, true);
        return true;
    }

    public static UserPDFDocument RegisterExistingPDF(
        string existingPath,
        int userId,
        DataBase db,
        string category = "Rechnungen"
    )
    {
        if (!File.Exists(existingPath))
        {
            Debug.LogError("PDF existiert nicht: " + existingPath);
            return null;
        }

        string originalName = Path.GetFileName(existingPath);

        UserPDFDocument document = new UserPDFDocument
        {
            userId = userId,
            originalFileName = originalName,
            storedFileName = originalName,
            filePath = existingPath,
            category = category,
            uploadedAt = DateTime.Now
        };

        db.createUserPDFDocument(document);

        return document;
    }
}