using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class BuissnesplanTest : MonoBehaviour
{
    void Start()
    {
        UserDatabaseAccess.getCurrentUserDatabase().setupBusinessPlanTable();

        createSampleAntworten();

        getAllAntworten();

        deleteAntworten();

        getAllAntwortenAfterDeletion();
        
    }



    private void createSampleAntworten()
    {
        UserDatabaseAccess.getCurrentUserDatabase().createAntwort(1);
        UserDatabaseAccess.getCurrentUserDatabase().createAntwort(0);
        UserDatabaseAccess.getCurrentUserDatabase().createAntwort(1);
    }

    private void getAllAntworten()
    {
        List<Antworten> antwortenEntries = UserDatabaseAccess.getCurrentUserDatabase().getAllAntworten();
        foreach (Antworten entry in antwortenEntries)
        {
            Debug.Log($"Antwort: {entry.Antwort} Nummer: {entry.Id}");
        }
    }

    private void deleteAntworten()
    {
        UserDatabaseAccess.getCurrentUserDatabase().deleteAntwort(1);
        Debug.Log("Die erste Antwort wurde gelöscht.");
    }

    private void getAllAntwortenAfterDeletion()
    {
        List<Antworten> antwortenEntries = UserDatabaseAccess.getCurrentUserDatabase().getAllAntworten();
        foreach (Antworten entry in antwortenEntries)
        {
            Debug.Log($"Antwort: {entry.Antwort} Nummer: {entry.Id}");
        }
    }

}
