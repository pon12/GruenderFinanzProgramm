using UnityEngine;

public class Finanzen 
{
     
    private float gewinn;
    private float umsatz;
    private float verlust;

    // Getter und Setter für gewinn
    public float getgewinn()
    {
        return gewinn;
    }

    public void setgewinn(float gewinn)
    {
        this.gewinn = gewinn;
    }

    // Getter und Setter für umsatz
    public float getumsatz()
    {
        return umsatz;
    }

    public void setumsatz(float umsatz)
    {
        this.umsatz = umsatz;
    }

    // Getter und Setter für verlust
    public float getverlust()
    {
        return verlust;
    }

    public void setverlust(float verlust)
    {
        this.verlust = verlust;
    }

}
