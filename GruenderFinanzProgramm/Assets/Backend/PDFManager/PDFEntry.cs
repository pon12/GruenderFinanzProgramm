using SQLite4Unity3d;
using System;
[System.Serializable]
public class PDFEntry
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string fileName { get; set; }      // z. B. "Rechnung_001.pdf"
    public string filePath { get; set; }      // interner Speicherpfad
    public string category { get; set; }      // z. B. "Rechnung", "Angebot", "Upload"
    public DateTime uploadedAt { get; set; }  // Speicherzeitpunkz
    public long fileSize { get; set; }        // Dateigröße 
}
