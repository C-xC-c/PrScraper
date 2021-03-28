using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PrScraper
{
    public class PullRequestInfo
    {
        public DateTime TimeStamp => DateTime.Now;

        public readonly SortedDictionary<int, PullRequest> PullRequests;

        public PullRequestInfo(SortedDictionary<int, PullRequest>? pullRequests)
        {
            pullRequests ??= new SortedDictionary<int, PullRequest>();

            PullRequests = new SortedDictionary<int, PullRequest>(pullRequests, new DescendingComparer<int>());
        }
    }

    public readonly struct PullRequest
    {
        public readonly string Title;

        public readonly string User;

        public readonly string Body;

        [JsonConstructor]
        public PullRequest(string title, string user, string body)
            => (Title, User, Body) = (title, user, body);

        public PullRequest(GithubPullRequest pr)
        {
            Title = pr.Title;
            User = pr.User.Login;

            int index = pr.Body.IndexOf("\r\n\r\n<!--\r\n###");
            Body = index == -1 ? pr.Body : pr.Body.Substring(0, index);
        }
    }
}
