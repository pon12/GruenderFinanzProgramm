using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SQLite;

public class Antworten
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int Antwort { get; set; }
}