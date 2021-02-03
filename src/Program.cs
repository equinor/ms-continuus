using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ms_continuus
{
    internal static class Program
    {
        private static readonly Config Config = new();
        private static readonly Api Api = new();
        private static readonly BlobStorage BlobStorage = new();

        private static async Task DeleteWeeklyBlobs()
        {
            var olderThan = Utility.DateMinusDays(Config.WeeklyRetention);
            Console.WriteLine($"Deleting blobs with retention='weekly' older than {olderThan}");
            await BlobStorage.DeleteArchivesBefore(olderThan, "weekly");
        }

        private static async Task DeleteMonthlyBlobs()
        {
            var olderThan = Utility.DateMinusDays(Config.MonthlyRetention);
            Console.WriteLine($"Deleting blobs with retention='monthly' older than {olderThan}");
            await BlobStorage.DeleteArchivesBefore(olderThan, "monthly");
        }

        private static async Task<bool> DownloadAndUpload(Migration migration, int index)
        {
            Migration migStatus = await Api.MigrationStatus(migration.Id);
            var exportTimer = 0;
            const int sleepIntervalSeconds = 30;
            while (migStatus.State != MigrationStatus.exported)
            {
                Thread.Sleep(sleepIntervalSeconds * 1_000);

                migStatus = await Api.MigrationStatus(migStatus.Id);
                if (migStatus.State == MigrationStatus.failed) return false;

                exportTimer++;
                Console.WriteLine(
                    $"Waiting for {migStatus} to be ready... waited {exportTimer * sleepIntervalSeconds} seconds");
            }
            string archivePath = await Api.DownloadArchive(migStatus.Id, index);
            await BlobStorage.UploadArchive(archivePath);
            return true;
        }

        private static async Task BackupArchive()
        {
            // Each migration can contain approx. 100~120 repositories
            // to keep the API from timing out. This also makes sense for retrying
            // smaller parts that failed in some way.
            const int chunkSize = 100;
            var startedMigrations = new List<Migration>();
            var failedToMigrate = new Dictionary<int, (List<string>, int)>();

            Console.WriteLine("Fetching all repositories...");
            var allRepositoryList = await Api.ListRepositories();

            var chunks = allRepositoryList.Count / chunkSize;
            var remainder = allRepositoryList.Count % chunkSize;

            Console.WriteLine(
                $"Starting migration of {allRepositoryList.Count} repositories divided into {chunks + 1} chunks");
            // Start the smallest migration first (remainder)
            startedMigrations.Add(await Api.StartMigration(allRepositoryList.GetRange(chunks * chunkSize, remainder)));

            for (var i = 0; i < chunks; i++)
            {
                var chunkedRepositoryList = allRepositoryList.GetRange(i, chunkSize);
                startedMigrations.Add(await Api.StartMigration(chunkedRepositoryList));
            }

            // Iterate through all the started migrations, wait for them to complete,
            // download them, and upload them to blob-storage
            for (var i = 0; i < startedMigrations.Count; i++)
            {
                var migration = startedMigrations[i];
                var uploaded = await DownloadAndUpload(migration, i);

                if (!uploaded)
                {
                        failedToMigrate[migration.Id] = (migration.Repositories, i);
                        Console.WriteLine($"WARNING: Migration {migration.Id} failed... continuing with next");
                        break;
                    }
            }

            // Go a second round to retry failed exports
            startedMigrations.Clear();
            foreach (var (id, (repos,volume)) in failedToMigrate)
            {
                startedMigrations.Add(await Api.StartMigration(repos));
            }

            for (var i = 0; i < startedMigrations.Count; i++)
            {
                Migration migration = startedMigrations[i];
                // Grab original volume/chunk number based on index in the list.
                var volume = failedToMigrate.Values.ElementAt(i).Item2;
                var uploaded = await DownloadAndUpload(migration, volume);

                // If the migration succeeded the second time, remove it from the list of failed migrations
                if (uploaded) failedToMigrate.Remove(migration.Id);
            }


            // Summary of failed migrations
            if (failedToMigrate.Count > 0)
            {
                Console.WriteLine("WARNING: Some migration requests failed to migrate");
                foreach (var (id, (repos,volume)) in failedToMigrate)
                    Console.WriteLine($"\tMigration Id: {id}, Repositories: [{string.Join(",", repos)}]");
                Environment.Exit(2);
            }
            else
            {
                Console.WriteLine($"Successfully uploaded all archives of {allRepositoryList.Count} repositories");
            }
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine(Config.ToString());
            Console.WriteLine("Starting backup of Github organization");
            var startTime = DateTime.Now;
            await BlobStorage.EnsureContainer();
            await BackupArchive();
            await DeleteWeeklyBlobs();
            await DeleteMonthlyBlobs();
            Console.WriteLine(
                $"MS-Continuus run complete. Started at {startTime}, finished at {DateTime.Now}, total run time: {DateTime.Now - startTime}");
        }
    }
}
