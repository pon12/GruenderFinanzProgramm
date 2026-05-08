// AuthService soll nicht wissen wie User gespeichert werden
// AuthService soll nicht direkt SQLite Code enthalten

public interface IUserRepository
{
    bool userExists(string email);
    User getUserByEmail(string email);
    void createUser(User user);
}