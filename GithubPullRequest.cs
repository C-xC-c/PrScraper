using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace PrScraper
{
    public readonly struct GithubPullRequest
    {
        [DataMember(Name = "number")]
        public readonly string Number;

        [DataMember(Name = "title")]
        public readonly string Title;

        [DataMember(Name = "user")]
        public readonly GithubUser User;

        [DataMember(Name = "body")]
        public readonly string Body;

        public GithubPullRequest(string number, string title, GithubUser user, string body)
            => (Number, Title, User, Body) = (number, title, user, body);
    }

    public readonly struct GithubUser
    {
        [DataMember(Name = "login")]
        public readonly string Login;

        [JsonConstructor]
        public GithubUser(string login) => Login = login;
    }
}
