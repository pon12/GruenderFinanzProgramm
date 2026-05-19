using SQLite4Unity3d;
using System.Collections.Generic;


[System.Serializable]
public class UserDB
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    
    public string name { get; set; }
    public string passKeyHash { get; set; }
    public string recoveryPassKeyHash { get; set; }
}


public class Company
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public string name { get; set; }
    // Lookup IDs
    public int legalForm { get; set; }
    public int industry { get; set; }
    public string location { get; set; }
}
