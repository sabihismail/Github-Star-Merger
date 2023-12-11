using CommandLine;

namespace GithubStarMerger
{
    public static class Program
    {
        private const string PROJECT_NAME = "GithubStarMerger";
        
        // ReSharper disable once ClassNeverInstantiated.Local
        private class CommandLineOptions
        {
            [Option('r', "repository-name", Required = false, Default = PROJECT_NAME, HelpText = "Repository name. Default is '" + PROJECT_NAME + "'")]
            public string RepositoryName { get; }
        }
        
        public static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var repositoryName = options.Value.RepositoryName;
            
            
        }
    }
}