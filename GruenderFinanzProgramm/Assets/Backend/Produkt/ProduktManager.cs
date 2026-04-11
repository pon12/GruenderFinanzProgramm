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

}
