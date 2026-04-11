using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using UnityEngine;

public class CodeTest : MonoBehaviour
{
    private int count = 0;
    void Start()
    {
        blabla();
        blabla();
    }

    public void blabla()
    {
        Produkt test = new Produkt(count);
        count++;
        test.setMenge(100); //werden von UI dann gecalled wenn eingaben gemacht werden
        test.setVarKosten(50);
        test.setFixKosten(10);

        ProduktManager.produktManager.updateProdukt(); //muss noch geschaut werden wann gecalled wird
        
        Debug.Log(test.getStueckKosten()); //werden von UI gecalled wenn ausgaben benötigt werden
        Debug.Log(test.getGesamtKosten());
    }



}
