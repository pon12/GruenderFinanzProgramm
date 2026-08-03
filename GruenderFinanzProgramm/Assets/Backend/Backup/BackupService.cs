using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class BackupService
{
    private const string BackupFolderName = "Backups";
    private const string BackupPrefix = "backup_";

    public static string CreateBackup()
    {
        try
        {
            string sourceDirectory = Application.persistentDataPath;
            string backupDirectory = CreateTimestampedBackupDirectory();

            string[] databaseFiles = Directory.GetFiles(sourceDirectory, "*.db", SearchOption.TopDirectoryOnly);

            if (databaseFiles.Length == 0)
            {
                Debug.LogWarning("[BackupService] Keine Datenbankdateien für Backup gefunden.");
                return null;
            }

            foreach (string databaseFile in databaseFiles)
            {
                string fileName = Path.GetFileName(databaseFile);
                string targetPath = Path.Combine(backupDirectory, fileName);

                File.Copy(databaseFile, targetPath, true);
                Debug.Log("[BackupService] Datenbank gesichert: " + fileName);
            }

            Debug.Log("[BackupService] Backup erfolgreich erstellt: " + backupDirectory);
            return backupDirectory;
        }
        catch (Exception ex)
        {
            Debug.LogError("[BackupService] Backup konnte nicht erstellt werden: " + ex.Message);
            return null;
        }
    }

    public static bool RestoreLatestBackup()
    {
        string latestBackup = GetLatestBackupDirectory();

        if (string.IsNullOrWhiteSpace(latestBackup))
        {
            Debug.LogWarning("[BackupService] Kein Backup zum Wiederherstellen gefunden.");
            return false;
        }

        return RestoreBackup(latestBackup);
    }

    public static bool RestoreBackup(string backupDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(backupDirectory) || !Directory.Exists(backupDirectory))
            {
                Debug.LogWarning("[BackupService] Backup-Ordner existiert nicht: " + backupDirectory);
                return false;
            }

            string targetDirectory = Application.persistentDataPath;
            string[] backupDatabaseFiles = Directory.GetFiles(backupDirectory, "*.db", SearchOption.TopDirectoryOnly);

            if (backupDatabaseFiles.Length == 0)
            {
                Debug.LogWarning("[BackupService] Keine Datenbankdateien im Backup gefunden.");
                return false;
            }

            foreach (string backupFile in backupDatabaseFiles)
            {
                string fileName = Path.GetFileName(backupFile);
                string targetPath = Path.Combine(targetDirectory, fileName);

                File.Copy(backupFile, targetPath, true);
                Debug.Log("[BackupService] Datenbank wiederhergestellt: " + fileName);
            }

            Debug.Log("[BackupService] Backup erfolgreich wiederhergestellt. App sollte danach neu gestartet werden.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[BackupService] Backup konnte nicht wiederhergestellt werden: " + ex.Message);
            return false;
        }
    }

    public static List<string> GetAvailableBackups()
    {
        string backupRoot = GetBackupRootDirectory();

        if (!Directory.Exists(backupRoot))
            return new List<string>();

        return Directory.GetDirectories(backupRoot, BackupPrefix + "*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(path => path)
            .ToList();
    }

    public static string GetLatestBackupDirectory()
    {
        List<string> backups = GetAvailableBackups();

        if (backups.Count == 0)
            return null;

        return backups[0];
    }

    public static string GetBackupRootDirectory()
    {
        return Path.Combine(Application.persistentDataPath, BackupFolderName);
    }

    private static string CreateTimestampedBackupDirectory()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string backupDirectory = Path.Combine(GetBackupRootDirectory(), BackupPrefix + timestamp);

        Directory.CreateDirectory(backupDirectory);

        return backupDirectory;
    }
}