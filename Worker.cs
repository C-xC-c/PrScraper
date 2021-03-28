using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PrScraper
{
    public class PullRequestInfo
    {
        public DateTime TimeStamp => DateTime.Now;

        public readonly Dictionary<string, PullRequest> PullRequests = new Dictionary<string, PullRequest>();
    }

    public class Worker : BackgroundService
    {
        private const string _url = "https://api.github.com/repos/rms-support-letter/rms-support-letter.github.io/pulls?state=all&per_page=100&page={0}";

        private readonly string _filePath;

        private readonly HttpClient _client = new HttpClient();

        private readonly PullRequestInfo _prs = new PullRequestInfo();

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, Config config)
        {
            _logger = logger;
            _client.DefaultRequestHeaders.Add("User-Agent", "rms-support-letter");
            _filePath = config.FilePath;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int lastPr = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                int page = 1;
                int newestPr = lastPr;

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation($"Parsing page {page}");
                    var resp = await _client.GetAsync(string.Format(_url, page));
                    var body = await resp.Content.ReadAsStringAsync();

                    if (body.AsSpan().SequenceEqual("[]"))
                    {
                        break;
                    }

                    List<GithubPullRequest> pullRequests = JsonConvert.DeserializeObject<List<GithubPullRequest>>(body);

                    foreach (var pr in pullRequests)
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

                        if (pr.Body.Length == 0 || pr.Body.AsSpan().StartsWith("<!--\r\n###"))
                        {
                            continue;
                        }
                        
                        // This handles if a new PR is made while we're scraping and pushes an old one down a page.
                        if (!_prs.PullRequests.TryAdd(pr.Number, pullRequest))
                        {
                            _logger.LogError($"Tried to add pr that already exists {pr.Number}, {JsonConvert.SerializeObject(pr)}");
                        }
                    }

                    page += 1;
                }

            end:
                _logger.LogInformation($"Found {newestPr - lastPr} new pull requests");
                lastPr = newestPr;

                File.WriteAllText(_filePath, JsonConvert.SerializeObject(_prs, Formatting.Indented));
                _logger.LogInformation($"Saved file {_filePath}");

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
