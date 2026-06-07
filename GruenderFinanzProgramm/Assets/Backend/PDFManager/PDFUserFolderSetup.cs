using System.IO;
using UnityEngine;

public static class PDFUserFolderSetup
{
    public static string GetUserCategoryFolder(
        int userId,
        string category
    )
    {
        string folder = Path.Combine(
            Application.persistentDataPath,
            "PDFs",
            $"User_{userId}",
            category
        );

        Directory.CreateDirectory(folder);

        return folder;
    }
}

