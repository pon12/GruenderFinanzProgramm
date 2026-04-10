using UnityEngine;

public static class Kostenrechnung
{
    public static float gesamtKosten(float fixKosten, float varKosten)
    {
        return fixKosten + varKosten;
    }

    public static float varKosten(float varKostenProEinheit, int menge)
    {
        if (menge < 0)
        {
            Debug.LogError("Die Menge darf nicht negativ sein.");
            return 0f;
        }

        return varKostenProEinheit * menge;
    }

    public static float durchschnittlicheFixKosten(float fixKosten, int menge)
    {
        if (menge <= 0)
        {
            Debug.LogError("Die Menge muss größer als 0 sein.");
            return 0f;
        }

        return fixKosten / menge;
    }

    public static float durchschnittlicheVarKosten(float varKosten, int menge)
    {
        if (menge <= 0)
        {
            Debug.LogError("Die Menge muss größer als 0 sein.");
            return 0f;
        }

        return varKosten / menge;
    }

    public static float stueckKosten(float gesamtKosten, int menge)
    {
        if (menge <= 0)
        {
            Debug.LogError("Die Menge muss größer als 0 sein.");
            return 0f;
        }

        return gesamtKosten / menge;
    }
}