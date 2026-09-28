public class StorageService
{
    public User GetUserFromDatabase (string username)
    {
        User requestedUser = new User (); 

        // Find user in DB
        requestedUser.UserID = 1;
        requestedUser.Username = username;
        requestedUser.PasswordVerifier = "Password";

        return requestedUser;
    }
}