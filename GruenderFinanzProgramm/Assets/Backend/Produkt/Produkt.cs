using UnityEngine;


public class Produkt
{
    //meta
    private int id;
    //Kostenrechnung
    private float gesamtKosten;
    private float fixKosten;
    private float varKosten;
    private float varKostenGesamt;
    private float varKostenProEinheit;
    private int menge;
    private float durchschnittlicheFixKosten;
    private float durchschnittlicheVarKosten;
    private float stueckKosten;
    //Ab hier für weitere Berechnungen
    private float verkaufsPreis;

    private int verkaufteMenge;

    private float umsatz;

    private float gewinn;

    private int mehrWertSteuer;

    public Produkt(int id)
    {
        this.id = id;
        ProduktManager.produktManager.addToList(this);
        //berechnen();
    }
    // Methode um die Infos aus Kostenrechnung zu holen
    public void berechnen()
    {
        gesamtKosten = Kostenrechnung.gesamtKosten(fixKosten, varKosten);
        varKosten = Kostenrechnung.varKosten(varKostenProEinheit, menge);
        durchschnittlicheFixKosten = Kostenrechnung.durchschnittlicheFixKosten(fixKosten, menge);
        durchschnittlicheVarKosten = Kostenrechnung.durchschnittlicheVarKosten(varKosten, menge);
        stueckKosten = Kostenrechnung.stueckKosten(gesamtKosten, menge);
    }

    public int getId()
    {
        return id;
    }

    // Getter-Methoden für die Kosteninformationen
    public float getGesamtKosten()
    {
        return gesamtKosten;
    }

    public float getDurchschnittlicheFixKosten()
    {
        return durchschnittlicheFixKosten;
    }

    public float getDurchschnittlicheVarKosten()
    {
        return durchschnittlicheVarKosten;
    }

    public float getStueckKosten()
    {
        return stueckKosten;
    }

        public float getFixKosten()
    {
        return fixKosten;
    }

    public float getVarKosten()
    {
        return varKosten;
    }

    public float getVarKostenGesamt()
    {
        return varKostenGesamt;
    }

    public float getVarKostenProEinheit()
    {
        return varKostenProEinheit;
    }

    public int getMenge()
    {
        return menge;
    }

    public float getVerkaufsPreis()
    {
        return verkaufsPreis;
    }

    public int getVerkaufteMenge()
    {
        return verkaufteMenge;
    }

    public float getUmsatz()
    {
        return umsatz;
    }

    public float getGewinn()
    {
        return gewinn;
    }

    public int getMehrWertSteuer()
    {
        return mehrWertSteuer;
    }

    // Setter-Methoden
      public void setId(int id)
    {
        this.id = id;
    }

    public void setGesamtKosten(float gesamtKosten)
    {
        this.gesamtKosten = gesamtKosten;
    }

    public void setFixKosten(float fixKosten)
    {
        this.fixKosten = fixKosten;
    }

    public void setVarKosten(float varKosten)
    {
        this.varKosten = varKosten;
    }

    public void setVarKostenProEinheit(float varKostenProEinheit)
    {
        this.varKostenProEinheit = varKostenProEinheit;
    }

    public void setMenge(int menge)
    {
        this.menge = menge;
    }

    public void setDurchschnittlicheFixKosten(float durchschnittlicheFixKosten)
    {
        this.durchschnittlicheFixKosten = durchschnittlicheFixKosten;
    }

    public void setDurchschnittlicheVarKosten(float durchschnittlicheVarKosten)
    {
        this.durchschnittlicheVarKosten = durchschnittlicheVarKosten;
    }

    public void setStueckKosten(float stueckKosten)
    {
        this.stueckKosten = stueckKosten;
    }

    public void setVerkaufsPreis(float verkaufsPreis)
    {
        this.verkaufsPreis = verkaufsPreis;
    }

    public void setVerkaufteMenge(int verkaufteMenge)
    {
        this.verkaufteMenge = verkaufteMenge;
    }

    public void setUmsatz(float umsatz)
    {
        this.umsatz = umsatz;
    }

    public void setGewinn(float gewinn)
    {
        this.gewinn = gewinn;
    }
    
    public void setMehrWertSteuer(int mehrWertSteuer)
    {
        this.mehrWertSteuer = mehrWertSteuer;
    }

}
