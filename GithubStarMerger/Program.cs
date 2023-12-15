using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Octokit;

namespace GithubStarMerger
{
    public static class Program
    {
        private const string PROJECT_NAME = "Github-Star-Merger";
        private const string DESCRIPTION = "Merge stars from one account to another.\nRepo link: https://github.com/sabihismail/Github-Star-Merger/";
        
        // ReSharper disable once ClassNeverInstantiated.Local
        private class CommandLineOptions
        {
            [Option("access-token-merger", Required = true, HelpText = "Personal access token - account that will generate stars.")]
            public string PersonalAccessTokenMerger { get; }
            
            [Option("access-token-source", Required = true, HelpText = "Personal access token - account that will be used as source of truth for generating tars.")]
            public string PersonalAccessTokenSource { get; }
            
            [Option('d', "start-date", Required = true, HelpText = "Start date to check for commits.")]
            public string StartDate { get; }
            
            [Option('r', "repository-name", Required = false, Default = PROJECT_NAME, HelpText = "Repository name. Default is '" + PROJECT_NAME + "'")]
            public string RepositoryName { get; }
        }

        private static async Task Run(string personalAccessTokenMerger, string personalAccessTokenSource, DateTimeOffset startDate, string repositoryName)
        {
            var client = new GitHubClient(new ProductHeaderValue(PROJECT_NAME));
            
            var tokenAuth = new Credentials(personalAccessTokenMerger);
            client.Credentials = tokenAuth;

            var repositories = await client.Repository.GetAllForCurrent();
            var repositoryExists = repositories.Any(x => x.Name == repositoryName);

            Repository starRepository;
            if (!repositoryExists)
            {
                starRepository = await client.Repository.Create(new NewRepository(repositoryName)
                {
                    Description = DESCRIPTION,
                    Visibility = RepositoryVisibility.Private,
                });
            }
            else
            {
                starRepository = repositories.First(x => x.Name == repositoryName);
            }

            var starCommits = await GetAllCommits(client, starRepository.Id, startDate);
            var commits = await GetCommits(personalAccessTokenSource, startDate);
            
            
        }

        private static async Task<IEnumerable<GitHubCommit>> GetCommits(string personalAccessTokenSource, DateTimeOffset startDate)
        {
            var client = new GitHubClient(new ProductHeaderValue(PROJECT_NAME));
            
            var tokenAuth = new Credentials(personalAccessTokenSource);
            client.Credentials = tokenAuth;
            
            var repositories = await client.Repository.GetAllForCurrent();
            var repositoryIds = repositories.Select(x => x.Id);

            var result = repositoryIds.SelectMany(repositoryId => GetAllCommits(client, repositoryId, startDate).Result);
            return result;
        }

        private static Task<IReadOnlyList<GitHubCommit>> GetAllCommits(IGitHubClient client, long repositoryId, DateTimeOffset startDate)
        {
            return client.Repository.Commit.GetAll(repositoryId, new CommitRequest
            {
                Since = startDate
            }, new ApiOptions
            {
                PageSize = 100,
            });
        }

        public static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var personalAccessTokenMerger = options.Value.PersonalAccessTokenMerger;
            var personalAccessTokenSource = options.Value.PersonalAccessTokenSource;
            var startDateStr = options.Value.StartDate;
            var repositoryName = options.Value.RepositoryName;

            if (!Regex.IsMatch(repositoryName, "[a-zA-Z0-9-_]+"))
            {
                Console.WriteLine($"Repository Name is not valid: {repositoryName}");
                return;
            }

            DateTimeOffset.TryParseExact(startDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,  DateTimeStyles.None, out var startDate);

            Run(
                personalAccessTokenMerger: personalAccessTokenMerger, 
                personalAccessTokenSource: personalAccessTokenSource, 
                startDate: startDate,
                repositoryName: repositoryName
            ).Wait();
        }
    }
}