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

            foreach (var project in projects)
            {
                //var t = ScrapeProject(project);
                var results = await ScrapeProject(project);

                using (StreamWriter streamWriter = new StreamWriter($"results-{project.Name}.json"))
                {
                    int i = 0;

                    //Lazily write a JSON array to a file.
                    await streamWriter.WriteLineAsync(JsonConvert.SerializeObject(results));
                }
            }
        }
        /// <summary>
        /// Pulls the metadata for items in a project.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private static async Task<ConcurrentBag<object>> ScrapeProject(StargazeProjectConfigItem project)
        {
            var getUrl = appConfig.GetSection("StargazeApiUrl").Value;
            List<Task> getTasks = new List<Task>();
            ConcurrentBag<object> itemMetadata = new ConcurrentBag<object>();

            //Increase this to increase the number of connections allowed to the API.
            using (var semaphore = new SemaphoreSlim(5, 5))
            {
                using (HttpClient client = new HttpClient(new RetryHandler(new HttpClientHandler())))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    List<Task> tasks = new();

                    for (int n = 1; n <= project.MaxMint; n++)
                    {
                        var itemNum = n;

                        await semaphore.WaitAsync();
                        tasks.Add(Task.Factory.StartNew(async () =>
                        {
                            try
                            {
                                string apiUrl = String.Format(getUrl, project.TokenMetaDataAddress, itemNum);
                                JsonSerializer serializer = new JsonSerializer();

                                
                                var response = await client.GetAsync(apiUrl);
                                Console.WriteLine(response.StatusCode);
                                var jsonString = await response.Content.ReadAsStringAsync();
                                var jsonObject = JsonConvert.DeserializeObject(jsonString);
                                itemMetadata.Add(jsonObject);
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine($"Failed to get metadata for {project.Name} item #{itemNum}!  The exception was {ex}");
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }).Unwrap());
                    }

                    Console.WriteLine("Waiting for tasks to complete...");
                    await Task.WhenAll(tasks);
                    Console.WriteLine("Tasks complete.");
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



