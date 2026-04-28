// Datenmodell für den Nutzer
// Nutzer ist ein Objekt mit zusammengehörigen Daten
// Kein direktes Passwort hier speichern

public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    public User(int id, string email, string passwordHash)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
    }
}