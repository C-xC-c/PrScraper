using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PrScraper
{
    public class PullRequestInfo
    {
        public DateTime TimeStamp => DateTime.Now;

        public readonly Dictionary<string, PullRequest> PullRequests;

        public PullRequestInfo(Dictionary<string, PullRequest>? pullRequests)
        {
            PullRequests = pullRequests ?? new Dictionary<string, PullRequest>();
        }
    }

    public class Worker : BackgroundService
    {
        private const string _url = "https://api.github.com/repos/rms-support-letter/rms-support-letter.github.io/pulls?state=all&per_page=100&page={0}";

        private readonly string _filePath;

        private readonly HttpClient _client = new HttpClient();

        private readonly PullRequestInfo _prs;

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, Config config)
        {
            if (config.FilePath is null)
            {
                throw new ArgumentNullException("You must set a FilePath in appsettings.json", nameof(config.FilePath));
            }

            _logger = logger;
            _client.DefaultRequestHeaders.Add("User-Agent", "rms-support-letter");
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, config.FilePath);

            try
            {
                _prs = new PullRequestInfo(JsonConvert.DeserializeObject<PullRequestInfo>(File.ReadAllText(_filePath))?.PullRequests);
            }
            catch (FileNotFoundException)
            {
                _prs = new PullRequestInfo(null);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int lastPr = 0;
            if (_prs.PullRequests.Count > 0)
            {
                lastPr = _prs.PullRequests.Max(x => int.Parse(x.Key));
                _logger.LogInformation($"Pull request file already exists, setting lastPr to {lastPr}");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                int page = 1;
                int newestPr = lastPr;

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug($"Parsing page {page}");
                    var resp = await _client.GetAsync(string.Format(_url, page)).ConfigureAwait(false);
                    var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Got bad response from github: {resp.StatusCode}, {body}");

                        // We most likely got rate limited, so just sleep for another hour.
                        goto sleep;
                    }

                    if (body.AsSpan().SequenceEqual("[]"))
                    {
                        break;
                    }

                    foreach (var pr in JsonConvert.DeserializeObject<List<GithubPullRequest>>(body)!)
                    {
                        int num = int.Parse(pr.Number);
                        if (lastPr > num)
                        {
                            // We've already seen all the prs after this, exit loop.
                            goto end;
                        }

                        if (num > newestPr)
                        {
                            newestPr = num;
                        }

                        PullRequest pullRequest = new PullRequest(pr);
                        if (!ValidateBody(pr.Body))
                        {
                            continue;
                        }

                        // This handles if a new PR is made while we're scraping and pushes an old one down a page.
                        if (!_prs.PullRequests.TryAdd(pr.Number, pullRequest))
                        {
                            _logger.LogWarning($"Tried to add pr that already exists {pr.Number}, {JsonConvert.SerializeObject(pr)}");
                        }
                    }

                    page += 1;
                }

            end:
                _logger.LogInformation($"Found {newestPr - lastPr} new pull requests");
                lastPr = newestPr;

                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_prs, Formatting.Indented));
                _logger.LogInformation($"Saved file {_filePath}");

            sleep:
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Shutting down");
        }

        public static bool ValidateBody(ReadOnlySpan<char> body)
        {
            if (body.IsEmpty)
            {
                return false;
            }

            if (body.StartsWith("<!--\r\n###"))
            {
                return false;
            }

            if (body.SequenceEqual("\r\n"))
            {
                return false;
            }

            return true;
        }
    }
}
