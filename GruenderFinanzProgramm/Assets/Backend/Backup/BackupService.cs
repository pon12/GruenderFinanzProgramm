using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class BackupService
{
    private const string BackupFolderName = "Backups";
    private const string BackupPrefix = "backup_";
    private const string SafetyBackupPrefix = "Sicherheits_Backup_";

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

    public static string CreateSafetyBackup()
    {
        try
        {
            string sourceDirectory = Application.persistentDataPath;
            string[] databaseFiles = Directory.GetFiles(sourceDirectory, "*.db", SearchOption.TopDirectoryOnly);

            if (databaseFiles.Length == 0)
            {
                Debug.LogWarning("[BackupService] Keine Datenbankdateien für Sicherheits-Backup gefunden.");
                return null;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupDirectory = Path.Combine(GetBackupRootDirectory(), SafetyBackupPrefix + timestamp);
            Directory.CreateDirectory(backupDirectory);

            foreach (string databaseFile in databaseFiles)
            {
                string fileName = Path.GetFileName(databaseFile);
                string targetPath = Path.Combine(backupDirectory, fileName);

                File.Copy(databaseFile, targetPath, true);
                Debug.Log("[BackupService] Sicherheits-Backup erstellt: " + fileName);
            }

            Debug.Log("[BackupService] Sicherheits-Backup erfolgreich erstellt: " + backupDirectory);
            return backupDirectory;
        }
        catch (Exception ex)
        {
            Debug.LogError("[BackupService] Sicherheits-Backup konnte nicht erstellt werden: " + ex.Message);
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

        var backupOrdner = Directory.GetDirectories(backupRoot, BackupPrefix + "*", SearchOption.TopDirectoryOnly);
        var sicherheitsOrdner = Directory.GetDirectories(backupRoot, SafetyBackupPrefix + "*", SearchOption.TopDirectoryOnly);

        return backupOrdner
            .Concat(sicherheitsOrdner)
            .OrderByDescending(path => ExtractTimestamp(path))
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

    public static string FormatBackupDisplayName(string backupDirectory)
    {
        string folderName = Path.GetFileName(backupDirectory);

        bool istSicherheitsBackup = folderName.StartsWith(SafetyBackupPrefix);
        string zeitTeil = istSicherheitsBackup
            ? folderName.Substring(SafetyBackupPrefix.Length)
            : folderName.StartsWith(BackupPrefix)
                ? folderName.Substring(BackupPrefix.Length)
                : folderName;

        string zeitText = zeitTeil;
        if (DateTime.TryParseExact(zeitTeil, "yyyy-MM-dd_HH-mm-ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out DateTime zeitpunkt))
        {
            zeitText = zeitpunkt.ToString("dd.MM.yyyy HH:mm:ss");
        }

        return istSicherheitsBackup
            ? $"Sicherheits-Backup - {zeitText}"
            : zeitText;
    }


    private static DateTime ExtractTimestamp(string backupDirectory)
    {
        string folderName = Path.GetFileName(backupDirectory);
        string zeitTeil = folderName.StartsWith(SafetyBackupPrefix)
            ? folderName.Substring(SafetyBackupPrefix.Length)
            : folderName.StartsWith(BackupPrefix)
                ? folderName.Substring(BackupPrefix.Length)
                : folderName;

        return DateTime.TryParseExact(zeitTeil, "yyyy-MM-dd_HH-mm-ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out DateTime zeitpunkt)
            ? zeitpunkt
            : DateTime.MinValue;
    }

    private static string CreateTimestampedBackupDirectory()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string backupDirectory = Path.Combine(GetBackupRootDirectory(), BackupPrefix + timestamp);

        Directory.CreateDirectory(backupDirectory);

        return backupDirectory;
    }
}