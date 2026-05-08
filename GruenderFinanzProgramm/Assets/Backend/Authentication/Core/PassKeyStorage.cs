using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PassKeyStorage
{
    private string filePath;

    public PassKeyStorage()
    {
        string folderPath = Path.Combine(Application.dataPath, "Backend/Authentication/Data");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        filePath = Path.Combine(folderPath, "auth_database.txt");

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }
    }

    public List<PassKeyRecord> getAllRecords()
    {
        List<PassKeyRecord> records = new List<PassKeyRecord>();

        if (!File.Exists(filePath))
        {
            return records;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split(';');

            if (parts.Length == 5)
            {
                records.Add(new PassKeyRecord(parts[0], parts[1], parts[2], parts[3], parts[4]));
            }
        }

        return records;
    }

    public void saveRecord(PassKeyRecord record)
    {
        string line = record.userId + ";" + record.username + ";" + record.companyName + ";" + record.passKey + ";" + record.recoveryKey;
        File.AppendAllText(filePath, line + "\n");
    }

    public void overwriteAllRecords(List<PassKeyRecord> records)
    {
        List<string> lines = new List<string>();

        foreach (PassKeyRecord record in records)
        {
            lines.Add(record.userId + ";" + record.username + ";" + record.companyName + ";" + record.passKey + ";" + record.recoveryKey);
        }

        File.WriteAllLines(filePath, lines);
    }

    public string getStoragePath()
    {
        return filePath;
    }
}