using Tweetinvi.Models;
using Iris.Api;

namespace Iris.Twitter
{
    internal static class UserFactory
    {
        public static User ToUser(IUser user)
        {
            long id = user.Id;
            string name = user.ScreenName;
            string displayName = user.Name;
            string url = user.Url;
            
            return new User(
                id.ToString(),
                name,
                displayName,
                url);
        }
    }
}