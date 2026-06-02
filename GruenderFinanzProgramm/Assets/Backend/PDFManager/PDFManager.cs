using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PDFManager
{
    public static List<PDFEntry> GetAllPDFs()
    {
        List<PDFEntry> result = new();

        string root =
            Path.Combine(Application.persistentDataPath, "PDFs");

        if (!Directory.Exists(root))
            return result;

        foreach (string file in Directory.GetFiles(
                     root,
                     "*.pdf",
                     SearchOption.AllDirectories))
        {
            FileInfo info = new(file);

            result.Add(new PDFEntry
            {
                fileName = info.Name,
                filePath = file,
                fileSize = info.Length,
                uploadedAt = info.CreationTime
            });
        }

        return result;
    }
}