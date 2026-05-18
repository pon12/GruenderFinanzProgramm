using SQLite4Unity3d;
[System.Serializable]
public class LookupEntry
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string category { get; set; }  // z. B. "LegalForm", "Industry"
    public string value { get; set; }     // z. B. "GmbH", "IT & Software"
}