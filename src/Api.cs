using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace ms_continuus
{
    public class Api
    {
        static Config config = new Config();
        private string repo_url = $"https://api.github.com/users/soofstad/repos";
        // private string repo_url = $"https://api.github.com/orgs/{config.ORGANIZATION}/repos/";
        private string migrations_url = $"https://api.github.com/user/migrations";
        // private string migrations_url = $"https://api.github.com/orgs/{config.ORGANIZATION}/migrations";
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

        private async Task<JsonDocument> GetJson(string url)
        {
            try
            {

                var responseBody = await client.GetAsync(url);
                responseBody.EnsureSuccessStatusCode();
                string content = await responseBody.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
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
            var jsonDocument = await this.GetJson(repo_url);
            if (jsonDocument == null) { Environment.Exit(1); }
            var repos = jsonDocument.RootElement.EnumerateArray();

            List<string> repoList = new List<string>();
            while (repos.MoveNext())
            {
                Console.WriteLine(repos.Current.GetProperty("name"));
                repoList.Add(repos.Current.GetProperty("name").ToString());
            }

            return repoList;
        }

        public async Task<List<Migration>> ListMigrations()
        {
            SetPreviewHeader(true);
            var jsonDocument = await this.GetJson(migrations_url);
            if (jsonDocument == null) { Environment.Exit(1); }
            var migrations = jsonDocument.RootElement.EnumerateArray();

            List<Migration> migrationsList = new List<Migration>();
            while (migrations.MoveNext())
            {
                // migrationsList.Add(JsonSerializer.Deserialize<Migration>(migrations.Current.GetRawText()));
                migrationsList.Add(new Migration(
                        JsonSerializer.Deserialize<int>(migrations.Current.GetProperty("id").GetRawText()),
                        migrations.Current.GetProperty("guid").ToString(),
                        migrations.Current.GetProperty("state").ToString(),
                        DateTime.Parse(migrations.Current.GetProperty("created_at").ToString())
                    )
                );
            }
            return migrationsList;
        }

        public async Task<Migration> MigrationStatus(int migrationId)
        {
            SetPreviewHeader(true);
            var jsonDocument = await this.GetJson(migrations_url + "/" + migrationId.ToString());
            if (jsonDocument == null) { Environment.Exit(1); }
            var migration = jsonDocument.RootElement;
            return new Migration(
                JsonSerializer.Deserialize<int>(migration.GetProperty("id").GetRawText()),
                migration.GetProperty("guid").ToString(),
                migration.GetProperty("state").ToString(),
                DateTime.Parse(migration.GetProperty("created_at").ToString())
            );
        }

        public async Task<string> DownloadArchive(int migrationId)
        {
            Directory.CreateDirectory("./tmp");
            SetPreviewHeader(true);
            Console.WriteLine($"Downloading archive {migrationId}");
            var response = await client.GetAsync($"{migrations_url}/{migrationId.ToString()}/archive");
            response.EnsureSuccessStatusCode();
            var content = response.Content;
            Stream stream = await content.ReadAsStreamAsync();
            string fileName = $"./tmp/archive-{DateTime.Now.ToString("dd_MM_yyyy")}-{migrationId.ToString()}.tar.gz";
            FileStream file = File.Create(fileName);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(file);
            file.Close();
            Console.WriteLine($"Successfully downloaded archive to '{fileName}'");

            return fileName;
        }

        public async Task<Migration> StartMigration()
        {
            SetPreviewHeader(true);
            HttpResponseMessage response = await client.PostAsync(migrations_url, new StringContent("{\"repositories\": [\"fidowinter\"]}"));
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            var asJson = JsonDocument.Parse(content).RootElement;
            return new Migration(
                JsonSerializer.Deserialize<int>(asJson.GetProperty("id").GetRawText()),
                asJson.GetProperty("guid").ToString(),
                asJson.GetProperty("state").ToString(),
                DateTime.Parse(asJson.GetProperty("created_at").ToString())
            );
        }
    }
}
