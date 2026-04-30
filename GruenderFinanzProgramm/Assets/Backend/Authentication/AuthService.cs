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

    private string temporaryPassKey;
    private string temporaryRecoveryKey;

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

    public string generatePassKey()
    {
        int passKey = UnityEngine.Random.Range(1000, 10000);
        temporaryPassKey = passKey.ToString();
        return temporaryPassKey;
    }

    public string generateRecoveryKey()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < 16; i++)
        {
            int index = UnityEngine.Random.Range(0, characters.Length);
            builder.Append(characters[index]);
        }

        temporaryRecoveryKey = builder.ToString();
        return temporaryRecoveryKey;
    }

    public string getPassKey()
    {
        return temporaryPassKey;
    }

    public string getRecoveryKey()
    {
        return temporaryRecoveryKey;
    }

    public bool loginWithPassKey(string enteredPassKey)
    {
        if (string.IsNullOrWhiteSpace(enteredPassKey))
        {
            return false;
        }

        if (enteredPassKey == temporaryPassKey)
        {
            return true;
        }

        return false;
    }

}