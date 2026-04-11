using UnityEngine;


public class Produkt
{
    public int id;
    public  float gesamtKosten;
    public float fixKosten;
    public float varKosten;
    public float varKostenGesamt;
    public float varKostenProEinheit;
    public int menge;
    public float durchschnittlicheFixKosten;
    public float durchschnittlicheVarKosten;
    public float stueckKosten;
    public float verkaufsPreis;

    public Produkt(int id)
    {
        this.id = id;
        Berechnen();
    }
    // Methode um die Infos aus Kostenrechnung zu holen
     private void Berechnen()
    {
        gesamtKosten = Kostenrechnung.gesamtKosten(fixKosten, varKosten);
        varKosten = Kostenrechnung.varKosten(varKostenProEinheit, menge);
        durchschnittlicheFixKosten = Kostenrechnung.durchschnittlicheFixKosten(fixKosten, menge);
        durchschnittlicheVarKosten = Kostenrechnung.durchschnittlicheVarKosten(varKosten, menge);
        stueckKosten = Kostenrechnung.stueckKosten(gesamtKosten, menge);
    }

    // Getter-Methoden für die Kosteninformationen
    public float GetGesamtKosten()
    {
        return gesamtKosten;
    }

    public float GetDurchschnittlicheFixKosten()
    {
        return durchschnittlicheFixKosten;
    }

    public float GetDurchschnittlicheVarKosten()
    {
        return durchschnittlicheVarKosten;
    }

    public float GetStueckKosten()
    {
        return stueckKosten;
    }
}
