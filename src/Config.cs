using System;

namespace ms_continuus
{
    public class Config
    {
        public Config()
        {
            Organization = Environment.GetEnvironmentVariable("GITHUB_ORG");
            if (Organization == null) throw new Exception("Environment variable 'GITHUB_ORG' missing");

            BlobContainer = Environment.GetEnvironmentVariable("BLOB_CONTAINER");
            if (BlobContainer == null)
            {
                Console.WriteLine(
                    "WARNING: Environment variable 'BLOB_CONTAINER' not set. Will assume a container named 'github-archives'");
                BlobContainer = "github-archives";
            }

            GithubURL = Environment.GetEnvironmentVariable("GITHUB_URL");
            if (GithubURL == null)
            {
                Console.WriteLine(
                    "WARNING: Environment variable 'GITHUB_URL' not set. Will assume 'https://api.github.com'");
                GithubURL = "https://api.github.com";
            }

            // Set tag to 'monthly' for the first week of the month.
            BlobTag = Environment.GetEnvironmentVariable("BLOB_TAG") ?? (DateTime.Today.Day < 8 ? "monthly" : "weekly");

            var weeklyFromEnv = Environment.GetEnvironmentVariable("WEEKLY_RETENTION");
            WeeklyRetention = weeklyFromEnv == null ? 60 : int.Parse(weeklyFromEnv);

            var monthlyFromEnv = Environment.GetEnvironmentVariable("MONTHLY_RETENTION");
            MonthlyRetention = monthlyFromEnv == null ? 230 : int.Parse(monthlyFromEnv);

            var yearlyFromEnv = Environment.GetEnvironmentVariable("YEARLY_RETENTION");
            YearlyRetention = yearlyFromEnv == null ? 420 : int.Parse(yearlyFromEnv);

            GithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (GithubToken == null)
                Console.WriteLine(
                    "WARNING: Environment variable 'GITHUB_TOKEN' not set. Will continue operating on public repositories only");

            StorageKey = Environment.GetEnvironmentVariable("STORAGE_KEY");
            if (StorageKey == null) throw new Exception("Environment variable 'STORAGE_KEY' missing");
        }

        public string Organization { get; }
        public string BlobContainer { get; }
        public string GithubURL { get; }
        public string BlobTag { get; }
        public int WeeklyRetention { get; }
        public int MonthlyRetention { get; }
        public int YearlyRetention { get; }
        public string GithubToken { get; }
        public string StorageKey { get; }

        public override string ToString()
        {
            var ghUrl = $"\n\tGITHUB URL: {GithubURL}";
            var org = $"\n\tORGANIZATION: {Organization}";
            var container = $"\n\tBLOB_CONTAINER: {BlobContainer}";
            var tag = $"\n\tBLOB_TAG: {BlobTag}";
            var weekRet = $"\n\tWEEKLY_RETENTION: {WeeklyRetention}";
            var monthRet = $"\n\tMONTHLY_RETENTION: {MonthlyRetention}";
            var yearRet = $"\n\tYEARLY_RETENTION: {YearlyRetention}";

            return "Configuration settings:" + ghUrl + org + container + tag + weekRet + monthRet + yearRet;
        }
    }
}
