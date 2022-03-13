using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using stargaze_asset_attribute_dump_console;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace stargaze_asset_attribute_dump_console
{
    internal class Program
    {
        private static IConfiguration? appConfig { get; set; }

        /// <summary>
        /// Probably not very resilient, but it seems to do the thing.
        /// </summary>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            appConfig = builder.Build();

            var projects = GetStargazeProjects();

            foreach(var project in projects)
            {
                var results = await ScrapeProjects(project);

                using (StreamWriter streamWriter = new StreamWriter($"results-{project.Name}.json"))
                {
                    foreach (string result in results)
                    {
                        await streamWriter.WriteLineAsync(result);
                    }
                }
            }
        }
        /// <summary>
        /// Pulls the metadata for items in a project.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private static async Task<ConcurrentBag<string>> ScrapeProjects(StargazeProjectConfigItem project)
        {
            var getUrl = appConfig.GetSection("StargazeApiUrl").Value;
            List<Task> getTasks = new List<Task>();
            ConcurrentBag<string> itemMetadata = new ConcurrentBag<string>();

            using (var semaphore = new SemaphoreSlim(5,5))
            {
                using (HttpClient client = new HttpClient(new RetryHandler(new HttpClientHandler())))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    List<Task> tasks = new List<Task>();
                        for(int i = 1; i <= project.MaxMint; i++)
                        {
                            await semaphore.WaitAsync();
                            tasks.Add(Task.Factory.StartNew(async () =>
                            {
                                try
                                {
                                    string apiUrl = String.Format(getUrl, project.TokenMetaDataAddress, i);

                                    Console.WriteLine($"Getting item metadata for {project.Name} item #{i}");
                                    var response = await client.GetAsync(apiUrl);
                                    var json = await response.Content.ReadAsStringAsync();
                                    itemMetadata.Add(json);
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine($"Failed to get metadata for {project.Name} item #{i}!  The exception was {ex}");
                                }
                                finally
                                {
                                    Console.WriteLine($"Finished getting item metadata for {project.Name} item #{i}");
                                    semaphore.Release();
                                }
                            }));

                            await Task.WhenAll(tasks);
                        }

                }
            }

            return itemMetadata;
        }

        private static List<StargazeProjectConfigItem> GetStargazeProjects()
        {
            List<StargazeProjectConfigItem> projects = new List<StargazeProjectConfigItem>();

            appConfig.GetSection("ProjectList").Bind(projects);
            return projects;
        }

        private static void GetProjectItemAttributes()
        {

        }
        
    }
}



