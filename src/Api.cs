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
        private static readonly Config Config = new Config();
        // private string repo_url = $"https://api.github.com/users/soofstad/repos";
        private readonly string _repoUrl = $"https://api.github.com/orgs/{Config.Organization}/repos";
        // private string migrations_url = $"https://api.github.com/user/migrations";
        private readonly string _migrationsUrl = $"https://api.github.com/orgs/{Config.Organization}/migrations";
        private static readonly HttpClient Client = new HttpClient();
        private const string PreviewAcceptHeader = "application/vnd.github.wyandotte-preview+json";
        private const string DefaultAcceptHeader = "application/vnd.github.v3+json";

        public Api()
        {
            Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.GithubToken}");
            Client.DefaultRequestHeaders.Add("User-Agent", "Equinor-Archiver");
        }

        private void SetPreviewHeader(bool preview = true)
        {
            if (preview)
            {
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Add("Accept", PreviewAcceptHeader);
            }
            else
            {
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Add("Accept", DefaultAcceptHeader);
            }
        }

        private async Task<JObject> GetJsonObject(string url)
        {
            try
            {

                var responseBody = await Client.GetAsync(url);
                responseBody.EnsureSuccessStatusCode();
                var content = await responseBody.Content.ReadAsStringAsync();
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

                var responseBody = await Client.GetAsync(url);
                responseBody.EnsureSuccessStatusCode();
                var content = await responseBody.Content.ReadAsStringAsync();
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
            var repoList = new List<string>();
            var page = 1;
            while (true)
            {
                var repos = await this.GetJsonArray($"{_repoUrl}?per_page=100&page={page}");
                if (repos == null) { Environment.Exit(1); }
                foreach (var jToken in repos)
                {
                    var repo = (JObject) jToken;
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
            var migrations = await this.GetJsonArray(_migrationsUrl);
            if (migrations == null) { Environment.Exit(1); }

            var migrationsList = new List<Migration>();
            foreach (var jToken in migrations)
            {
                var migration = (JObject) jToken;
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
            var migration = await this.GetJsonObject(_migrationsUrl + "/" + migrationId.ToString());
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
            var paddedVolume = volume.ToString();
            if(volume < 10){paddedVolume = "0"+paddedVolume;}
            Directory.CreateDirectory("./tmp");
            var fileName = $"./tmp/archive-{DateTime.Now.ToString("dd_MM_yyyy")}-vol.{paddedVolume}-{migrationId.ToString()}.tar.gz";
            SetPreviewHeader(true);
            Console.WriteLine($"Downloading archive {migrationId}");
            var attempts = 1;
            const int retryInterval = 30_000;

            while (attempts < 5)
            {
                try
                {
                    var timeStarted = DateTime.Now;
                    var response = await Client.GetAsync($"{_migrationsUrl}/{migrationId.ToString()}/archive", HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var archiveSize = Utility.BytesToString(response.Content.Headers.ContentLength.GetValueOrDefault());
                    Console.WriteLine($"\tSize of archive is {archiveSize}");
                    using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
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
            var payload = $"{{\"repositories\": {JsonConvert.SerializeObject(repositoryList)}}}";
            SetPreviewHeader(true);
            var response = await Client.PostAsync(_migrationsUrl, new StringContent(payload));
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var migration = JObject.Parse(content);

            var repoList = new List<string>();
            foreach (var jToken in migration["repositories"])
            {
                var repo = (JObject) jToken;
                repoList.Add(repo["name"].ToString());
            }

            var result = new Migration(
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
