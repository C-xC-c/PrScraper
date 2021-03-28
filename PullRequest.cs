namespace PrScraper
{
    public readonly struct PullRequest
    {
        public readonly string Title;

        public readonly string User;

        public readonly string Body;

        public PullRequest(GithubPullRequest pr)
        {
            Title = pr.Title;
            User = pr.User.Login;

            int index = pr.Body.IndexOf("\r\n\r\n<!--\r\n###");
            Body = index == -1 ? pr.Body : pr.Body.Substring(0, index);
        }
    }
}
