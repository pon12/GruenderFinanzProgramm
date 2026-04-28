// Hauptklasse für Login und Registrierung 
// Registrieren
// Einloggen 
// PW hashen
// PW prüfen
// Die UI soll dann die Eingabe bringen
// "Der AuthService darf nicht wissen, ob die Daten aus SQLite, einer Datei oder Testdaten kommen. Er arbeitet nur gegen das Interface."
// Überprüft ob der Nutzer existiert
// PW Hashing mit SHA-256
// Vergliech zwischen PW Hash und gespeichertem Hash

using System.Security.Cryptography;
using System.Text;

public class AuthService
{
    private readonly IUserRepository userRepository;

    public AuthService(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public bool register(string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (password != confirmPassword)
        {
            return false;
        }

        if (userRepository.userExists(email))
        {
            return false;
        }

        string passwordHash = hashPassword(password);

        User newUser = new User(0, email, passwordHash);

        userRepository.createUser(newUser);

        return true;
    }

    public User login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        User user = userRepository.getUserByEmail(email);

        if (user == null)
        {
            return null;
        }

        string enteredPasswordHash = hashPassword(password);

        if (enteredPasswordHash == user.PasswordHash)
        {
            return user;
        }

        return null;
    }

    public string hashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            StringBuilder builder = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}