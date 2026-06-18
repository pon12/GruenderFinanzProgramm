using System;
using SQLite;

public class UserPDFDocument
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public int userId { get; set; }

    public string originalFileName { get; set; }

    public string storedFileName { get; set; }

    public string filePath { get; set; }

    public string category { get; set; }

    public DateTime uploadedAt { get; set; }
}