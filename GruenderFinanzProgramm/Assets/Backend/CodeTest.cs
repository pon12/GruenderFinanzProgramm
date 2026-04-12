using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using UnityEngine;

public class CodeTest : MonoBehaviour
{
    private int count = 0;
    void Start()
    {
        blabla(50,100,20);
        blabla(10,650,100);
    }

    public void blabla(int menge, float varKosten, float fixKosten)
    {
        Produkt test = new Produkt(count);
        count++;
        test.setMenge(menge); //werden von UI dann gecalled wenn eingaben gemacht werden
        test.setVarKosten(varKosten);
        test.setFixKosten(fixKosten);

        ProduktManager.produktManager.updateProdukt(); //muss noch geschaut werden wann gecalled wird
        
        Debug.Log(test.getId() + " StückKosten: " + test.getStueckKosten()); //werden von UI gecalled wenn ausgaben benötigt werden
        Debug.Log(test.getId() + " GesamtKosten: " + test.getGesamtKosten());
    }



}
