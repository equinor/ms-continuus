using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ms_continuus
{
    public class Api
    {
        static Config config = new Config();
        // private string repo_url = $"https://api.github.com/users/soofstad/repos";
        private string repo_url = $"https://api.github.com/orgs/{config.ORGANIZATION}/repos";
        // private string migrations_url = $"https://api.github.com/user/migrations";
        private string migrations_url = $"https://api.github.com/orgs/{config.ORGANIZATION}/migrations";
        private static readonly HttpClient client = new HttpClient();
        private string previewAcceptHeader = "application/vnd.github.wyandotte-preview+json";
        private string defaultAcceptHeader = "application/vnd.github.v3+json";

        public Api()
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GITHUB_TOKEN}");
            client.DefaultRequestHeaders.Add("User-Agent", "Equinor-Archiver");
        }

        private void SetPreviewHeader(bool preview = true)
        {
            if (preview)
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", previewAcceptHeader);
            }
            else
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Accept", defaultAcceptHeader);
            }
        }

        private async Task<JObject> GetJsonObject(string url)
        {
            try
            {

                var responseBody = await client.GetAsync(url);
                responseBody.EnsureSuccessStatusCode();
                string content = await responseBody.Content.ReadAsStringAsync();
                return JObject.Parse(content);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        private async Task<JArray> GetJsonArray(string url)
        {
            try
            {

                var responseBody = await client.GetAsync(url);
                responseBody.EnsureSuccessStatusCode();
                string content = await responseBody.Content.ReadAsStringAsync();
                return JArray.Parse(content);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return null;
            }
        }

        public async Task<List<string>> ListRepositories()
        {
            SetPreviewHeader(true);
            List<string> repoList = new List<string>();
            int page = 1;
            while (true)
            {
                JArray repos = await this.GetJsonArray($"{repo_url}?per_page=100&page={page}");
                if (repos == null) { Environment.Exit(1); }
                foreach (JObject repo in repos)
                {
                    repoList.Add(repo["name"].ToString());
                }
                if (repos.Count < 100) { break; }
                page++;
            }

            return repoList;
        }

        public async Task<List<Migration>> ListMigrations()
        {
            SetPreviewHeader(true);
            JArray migrations = await this.GetJsonArray(migrations_url);
            if (migrations == null) { Environment.Exit(1); }

            List<Migration> migrationsList = new List<Migration>();
            foreach (JObject migration in migrations)
            {
                migrationsList.Add(new Migration(
                    int.Parse(migration["id"].ToString()),
                    migration["guid"].ToString(),
                    migration["state"].ToString(),
                    DateTime.Parse(migration["created_at"].ToString())
                )
                    );
            }
            return migrationsList;
        }

        public async Task<Migration> MigrationStatus(int migrationId)
        {
            SetPreviewHeader(true);
            JObject migration = await this.GetJsonObject(migrations_url + "/" + migrationId.ToString());
            if (migration == null) { Environment.Exit(1); }
            return new Migration(
                    int.Parse(migration["id"].ToString()),
                    migration["guid"].ToString(),
                    migration["state"].ToString(),
                    DateTime.Parse(migration["created_at"].ToString())
                );
        }

        public async Task<string> DownloadArchive(int migrationId, int volume)
        {
            string paddedVolume = volume.ToString();
            if(volume < 10){paddedVolume = "0"+paddedVolume;}
            Directory.CreateDirectory("./tmp");
            string fileName = $"./tmp/archive-{DateTime.Now.ToString("dd_MM_yyyy")}-vol.{paddedVolume}-{migrationId.ToString()}.tar.gz";
            SetPreviewHeader(true);
            Console.WriteLine($"Downloading archive {migrationId}");
            int attempts = 1;
            int retryInterval = 30000;

            while (attempts < 5)
            {
                try
                {
                    var timeStarted = DateTime.Now;
                    var response = await client.GetAsync($"{migrations_url}/{migrationId.ToString()}/archive", HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    string archiveSize = Utility.BytesToString(response.Content.Headers.ContentLength.GetValueOrDefault());
                    Console.WriteLine($"\tSize of archive is {archiveSize}");
                    using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                    {
                        using (Stream streamToWriteTo = File.Open(fileName, FileMode.Create))
                        {
                            await streamToReadFrom.CopyToAsync(streamToWriteTo);
                        }
                    }
                    Console.WriteLine($"\tAverage download speed: {Utility.TransferSpeed(response.Content.Headers.ContentLength.GetValueOrDefault(), timeStarted)}");
                    Console.WriteLine($"Successfully downloaded archive to '{fileName}'");
                    return fileName;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"WARNING: Failed to download archive ({e.Message}). Retrying in {retryInterval / 1000} seconds");
                    Thread.Sleep(retryInterval);
                }
                attempts++;
            }
            throw new Exception($"Failed to download archive '{migrationId}' with {attempts} attempts.");
        }

        public async Task<Migration> StartMigration(List<string> repositoryList)
        {
            string payload = $"{{\"repositories\": {JsonConvert.SerializeObject(repositoryList)}}}";
            SetPreviewHeader(true);
            HttpResponseMessage response = await client.PostAsync(migrations_url, new StringContent(payload));
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            JObject migration = JObject.Parse(content);

            List<string> repoList = new List<string>();
            foreach (JObject repo in migration["repositories"])
            {
                repoList.Add(repo["name"].ToString());
            }

            Migration result = new Migration(
                    int.Parse(migration["id"].ToString()),
                    migration["guid"].ToString(),
                    migration["state"].ToString(),
                    DateTime.Parse(migration["created_at"].ToString()),
                    repoList
                );
            Console.WriteLine($"\t{result}");
            return result;
        }
    }
}
