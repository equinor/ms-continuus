using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace ms_continuus
{
    class Program
    {
        private static readonly Config Config = new Config();

        private static async Task DeleteWeeklyBlobs()
        {
            var olderThan = Utility.DateMinusDays(Config.WeeklyRetention);
            Console.WriteLine($"Deleting blobs with retention='weekly' older than {olderThan}");

            var blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan, "weekly");
        }

        static async Task DeleteMonthlyBlobs()
        {
            var olderThan = Utility.DateMinusDays(Config.MonthlyRetention);
            Console.WriteLine($"Deleting blobs with retention='monthly' older than {olderThan}");

            var blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan, "monthly");

        }

        static async Task BackupArchive()
        {
            var api = new Api();
            var blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            // Each migration can contain approx. 100~120 repositories
            // to keep the API from timing out. This also makes sense for retrying
            // smaller parts that failed in some way.
            const int chunkSize = 100;
            var startedMigrations = new List<Migration>();
            var failedToMigrate = new Dictionary<string, List<string>>();

            Console.WriteLine("Fetching all repositories...");
            var allRepositoryList = await api.ListRepositories();

            var chunks = allRepositoryList.Count / chunkSize;
            var remainder = allRepositoryList.Count % chunkSize;

            Console.WriteLine($"Starting migration of {allRepositoryList.Count} repositories divided into {chunks + 1} chunks");
            // Start the smallest migration first (remainder)
            startedMigrations.Add(await api.StartMigration(allRepositoryList.GetRange((chunks * chunkSize), remainder)));

            for (var i = 0; i < chunks; i++)
            {
                var chunkedRepositoryList = allRepositoryList.GetRange(i, chunkSize);
                startedMigrations.Add(await api.StartMigration(chunkedRepositoryList));
            }

            // Iterate through all the started migrations, wait for them to complete,
            // download them, and upload them to blob-storage
            var migrationIndex = 0;
            foreach (var migration in startedMigrations)
            {
                var migStatus = await api.MigrationStatus(migration.Id);
                var exportTimer = 0;
                const int sleepIntervalSeconds = 30;
                while (migStatus.State != "exported")
                {
                    Thread.Sleep(sleepIntervalSeconds * 1_000);
                    migStatus = await api.MigrationStatus(migStatus.Id);
                    if (migStatus.State == "failed")
                    {
                        failedToMigrate[migration.Id.ToString()] = migration.Repositories;
                        Console.WriteLine($"WARNING: Migration {migration.Id.ToString()} failed... continuing with next");
                        break;
                    }

                    exportTimer++;
                    Console.WriteLine($"Waiting for {migStatus.ToString()} to be ready... waited {exportTimer * sleepIntervalSeconds} seconds");
                }
                if (migStatus.State == "failed") { continue; }

                Console.WriteLine($"Ready;\t{migStatus}");
                var archivePath = await api.DownloadArchive(migStatus.Id, migrationIndex);
                await blobStorage.UploadArchive(archivePath);
                migrationIndex++;
            }

            // Summary of failed migrations
            if (failedToMigrate.Count > 0)
            {
                Console.WriteLine($"WARNING: Some migration requests failed to migrate");
                foreach (var (key, value) in failedToMigrate)
                {
                    Console.WriteLine($"\tMigration Id: {key}, Repositories: [{string.Join(",", value)}]");
                }
                Environment.Exit(2);
            }
            else
            {
                Console.WriteLine($"Successfully uploaded archives of {allRepositoryList.Count} repositories");
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine(Config.ToString());
            Console.WriteLine($"Starting backup of Github organization");
            var startTime = DateTime.Now;
            await BackupArchive();
            await DeleteWeeklyBlobs();
            await DeleteMonthlyBlobs();
            var totalRunTime = DateTime.Now - startTime;
            Console.WriteLine($"MS-Continuus run complete. Started at {startTime.ToString()}, finished at {DateTime.Now.ToString()}, total run time: {totalRunTime.ToString()}");
        }
    }
}
