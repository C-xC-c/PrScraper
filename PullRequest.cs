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

            /* I have absolutely no idea what this is.
             * Github seems to append part of the readme as a HTML comment
             * to the body, which is just... what?
             */

            int index = pr.Body.IndexOf("<!--\r\n###");

            if (index == 0)
            {
                Body = "";
            }
            else
            {
                // It also seems to be different if there is a body?
                int weridIndex = pr.Body.IndexOf("\r\n\r\n<!--\r\n###");

                Body = weridIndex == -1 ? pr.Body : pr.Body.Substring(0, weridIndex);
            }
        }
    }
}
