using System.Collections.Generic;
using UnityEngine;

public class ProduktManager
{
    public static ProduktManager produktManager = new();

    public List<Produkt> produktListe = new List<Produkt>();



    public void updateProdukt()
    {
        foreach (Produkt p in produktListe)
        {
            p.berechnen();
        }
    }

    public void addToList(Produkt produkt)
    {
        produktListe.Add(produkt);
    }

    public float getAllGewinn()
    {
        float totalGewinn = 0;
        
        foreach (Produkt p in produktListe)
        {
             totalGewinn += p.getGewinn();
        }

        return totalGewinn;
    }

}
