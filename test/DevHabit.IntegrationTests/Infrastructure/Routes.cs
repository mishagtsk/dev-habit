namespace DevHabit.IntegrationTests.Infrastructure;

public static class Routes
{
    public static class Auth
    {
        public const string Login = "auth/login";
        public const string Register = "auth/register";
    }
    
    public static class Habits
    {
        public const string Create = "habits";
    }
    
    public static class GitHub
    {
        public const string StoreAccessToken = "github/personal-access-token";
        public const string GetProfile = "github/profile";
        public const string GetEvents = "github/events";
    }
}
