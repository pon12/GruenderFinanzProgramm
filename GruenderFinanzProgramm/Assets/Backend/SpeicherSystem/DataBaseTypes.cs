using SQLite4Unity3d;



[System.Serializable]
public class User
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    
    public string name { get; set; }
    public int passKey { get; set; }
    public int recoveryKey { get; set; }
    public bool isLoggedIn { get; set; }
}

[System.Serializable]
public class Company
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    
    public string name { get; set; }
    public int legalForm { get; set; }
}
