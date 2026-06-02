using System;
using System.IO;
using UnityEngine;

public static class PDFStorage
{
    private static string BaseFolder =>
        Path.Combine(Application.persistentDataPath, "PDFs");

    static PDFStorage()
    {
        Directory.CreateDirectory(BaseFolder);
    }

    public static PDFEntry SavePDF(string sourcePath, string category = "Upload")
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"Datei nicht gefunden: {sourcePath}");
            return null;
        }

        string categoryFolder = Path.Combine(BaseFolder, category);
        Directory.CreateDirectory(categoryFolder);

        string fileName = Path.GetFileName(sourcePath);
        string targetPath = Path.Combine(categoryFolder, fileName);

        if (File.Exists(targetPath))
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string name = Path.GetFileNameWithoutExtension(fileName);

            fileName = $"{name}_{timestamp}.pdf";
            targetPath = Path.Combine(categoryFolder, fileName);
        }

        File.Copy(sourcePath, targetPath);

        return new PDFEntry
        {
            fileName = fileName,
            filePath = targetPath,
            category = category,
            uploadedAt = DateTime.Now,
            fileSize = new FileInfo(targetPath).Length
        };
    }

    public static bool DeletePDF(PDFEntry entry)
    {
        return DeletePDF(entry.filePath);
    }

    public static bool DeletePDF(string path)
    {
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    public static byte[] GetPDFBytes(PDFEntry entry)
    {
        if (!File.Exists(entry.filePath))
            return null;

        return File.ReadAllBytes(entry.filePath);
    }

    public static string ExportPDF(PDFEntry entry, string destinationPath)
    {
        if (!File.Exists(entry.filePath))
            return null;

        File.Copy(entry.filePath, destinationPath, true);

        return destinationPath;
    }
}