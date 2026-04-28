using System.Collections.Generic;

public class DummyUserRepository : IUserRepository
{
    private readonly List<User> users = new List<User>();
    private int nextID = 1;

    public bool userExists(string email)
    {
        return users.Exists(user => user.Email == email);
    }

    public User getUserByEmail(string email)
    {
        return users.Find(user => user.Email == email);
    }

    public void createUser(User user)
    {
        User userWithId = new User(nextID, user.Email, user.PasswordHash);
        users.Add(userWithId);
        nextID++;
    }
}