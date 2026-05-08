public class PassKeyRecord
{
    public string userId;
    public string username;
    public string companyName;
    public string passKey;
    public string recoveryKey;

    public PassKeyRecord(string userId, string username, string companyName, string passKey, string recoveryKey)
    {
        this.userId = userId;
        this.username = username;
        this.companyName = companyName;
        this.passKey = passKey;
        this.recoveryKey = recoveryKey;
    }
}