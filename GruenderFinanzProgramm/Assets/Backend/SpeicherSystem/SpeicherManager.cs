using System.Data.SqlClient;
using UnityEngine;

public class SpeicherManager
{   
    // Verbindung zur Datenbank. 
    private const string connectionString = "Server=localhost;Database=Daten;Trusted_Connection=True;";      
   // Daten eintragen
    public void writeData(int id, int value)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "INSERT INTO Tabelle (Id, Gewinn) VALUES (@Id, @Gewinn)";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Gewinn", value);
                command.ExecuteNonQuery();
            }
        }
    }

    // Daten lesen, hier Gewinn von bestimmter Id auslesen. Als beispiel.
    public int readData(int id)
    {
        int result = 0;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT Gewinn FROM Tabelle WHERE Id = @Id";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result = reader.GetInt32(0);
                    }
                }
            }
        }

        return result;
    }
}
