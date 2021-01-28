using System;

namespace ms_continuus
{
    public class Config
    {
        public string Organization { get; }
        public string BlobContainer { get; }
        public string BlobTag { get; }
        public int WeeklyRetention { get; }
        public int MonthlyRetention { get; }
        public int YearlyRetention { get; }
        public string GithubToken { get; }
        public string StorageKey { get; }

        public Config()
        {
            this.Organization = Environment.GetEnvironmentVariable("GITHUB_ORG");
            if (this.Organization == null)
            {
                throw new Exception("Environment variable 'GITHUB_ORG' missing");
            }

            this.BlobContainer = Environment.GetEnvironmentVariable("BLOB_CONTAINER");
            if (this.BlobContainer == null)
            {
                Console.WriteLine(
                    "WARNING: Environment variable 'BLOB_CONTAINER' not set. Will assume a container named 'github-archives'");
                this.BlobContainer = "github-archives";
            }

            this.BlobTag = Environment.GetEnvironmentVariable("BLOB_TAG");
            if (this.BlobTag == null)
            {
                // Set tag to 'monthly' for the first week of the month.
                if (DateTime.Today.Day < 8)
                {
                    this.BlobTag = "monthly";
                }
                else
                {
                    this.BlobTag = "weekly";
                }
            }

            var weeklyFromEnv = Environment.GetEnvironmentVariable("WEEKLY_RETENTION");
            if (weeklyFromEnv == null)
            {
                this.WeeklyRetention = 60;
            }
            else
            {
                this.WeeklyRetention = int.Parse(weeklyFromEnv);
            }

            var monthlyFromEnv = Environment.GetEnvironmentVariable("MONTHLY_RETENTION");
            if (monthlyFromEnv == null)
            {
                this.MonthlyRetention = 230;
            }
            else
            {
                this.MonthlyRetention = int.Parse(monthlyFromEnv);
            }

            var yearlyFromEnv = Environment.GetEnvironmentVariable("YEARLY_RETENTION");
            if (yearlyFromEnv == null)
            {
                this.YearlyRetention = 420;
            }
            else
            {
                this.YearlyRetention = int.Parse(yearlyFromEnv);
            }

            this.GithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (this.GithubToken == null)
            {
                Console.WriteLine(
                    "WARNING: Environment variable 'GITHUB_TOKEN' not set. Will continue operating on public repositories only");
            }

            this.StorageKey = Environment.GetEnvironmentVariable("STORAGE_KEY");
            if (this.StorageKey == null)
            {
                throw new Exception("Environment variable 'STORAGE_KEY' missing");
            }
        }

        public override string ToString()
        {
            var org = $"\n\tORGANIZATION: {Organization}";
            var container = $"\n\tBLOB_CONTAINER: {BlobContainer}";
            var tag = $"\n\tBLOB_TAG: {BlobTag}";
            var weekRet = $"\n\tWEEKLY_RETENTION: {WeeklyRetention}";
            var monthRet = $"\n\tMONTHLY_RETENTION: {MonthlyRetention}";
            var yearRet = $"\n\tYEARLY_RETENTION: {YearlyRetention}";

            return $"Configuration settings:" + org + container + tag + weekRet + monthRet + yearRet;
        }
    }
}
