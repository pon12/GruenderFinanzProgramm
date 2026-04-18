using UnityEngine;

public class Unternehmen 
{
    
    private string name;
    private int haftung;

    // Getter und Setter für name
    public string getname()
    {
        return name;
    }

    public void setname(string name)
    {
        this.name = name;
    }

    // Getter und Setter für haftung
    public int gethaftung()
    {
        return haftung;
    }

    public void sethaftung(int haftung)
    {
        this.haftung = haftung;
    }
}