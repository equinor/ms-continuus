using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace ms_continuus
{
    class Program
    {
        private static Config config = new Config();
        static async Task DeleteWeeklyBlobs()
        {
            DateTime olderThan = Utility.DateMinusDays(config.WEEKLY_RETENTION);
            Console.WriteLine($"Deleting blobs with retention='weekly' older than {olderThan}");

            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan, "weekly");
        }

        static async Task DeleteMonthlyBlobs()
        {
            DateTime olderThan = Utility.DateMinusDays(config.MONTHLY_RETENTION);
            Console.WriteLine($"Deleting blobs with retention='monthly' older than {olderThan}");

            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan, "monthly");

        }

        static async Task BackupArchive()
        {
            Api api = new Api();
            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            // Each migration can contain approx. 100~120 repositories
            // to keep the API from timing out. This also makes sense for retrying
            // smaller parts that failed in some way.
            int chunkSize = 100;
            List<Migration> startedMigrations = new List<Migration>();
            List<string> allRepositoryList = new List<string>();
            Dictionary<string, List<String>> failedToMigrate = new Dictionary<string, List<String>>();

            Console.WriteLine("Fetching all repositories...");
            allRepositoryList = await api.ListRepositories();

            int chunks = allRepositoryList.Count / chunkSize;
            int remainder = allRepositoryList.Count % chunkSize;

            Console.WriteLine($"Starting migration of {allRepositoryList.Count} repositories divided into {chunks + 1} chunks");
            // Start the smallest migration first (remainder)
            startedMigrations.Add(await api.StartMigration(allRepositoryList.GetRange((chunks * chunkSize), remainder)));

            for (int i = 0; i < chunks; i++)
            {
                List<string> chunkedRepositoryList = allRepositoryList.GetRange(i, chunkSize);
                startedMigrations.Add(await api.StartMigration(chunkedRepositoryList));
            }

            // Iterate through all the started migrations, wait for them to complete,
            // download them, and upload them to blob-storage
            int migrationIndex = 0;
            foreach (Migration migration in startedMigrations)
            {
                Migration migStatus = await api.MigrationStatus(migration.id);
                int exportTimer = 0;
                int sleepIntervalSeconds = 30;
                while (migStatus.state != "exported")
                {
                    Thread.Sleep(sleepIntervalSeconds * 1000);
                    migStatus = await api.MigrationStatus(migStatus.id);
                    if (migStatus.state == "failed")
                    {
                        failedToMigrate[migration.id.ToString()] = migration.repositories;
                        Console.WriteLine($"WARNING: Migration {migration.id.ToString()} failed... continuing with next");
                        break;
                    }
                    exportTimer++;
                    Console.WriteLine($"Waiting for {migStatus.ToString()} to be ready... waited {exportTimer * sleepIntervalSeconds} seconds");
                }
                if (migration.state == "failed") { continue; }

                Console.WriteLine($"Ready;\t{migStatus}");
                string archivePath = await api.DownloadArchive(migStatus.id, migrationIndex);
                await blobStorage.UploadArchive(archivePath);
                migrationIndex++;
            }

            // Summary of failed migrations
            if (failedToMigrate.Count > 0)
            {
                Console.WriteLine($"WARNING: Some migration requests failed to migrate");
                foreach (var item in failedToMigrate)
                {
                    Console.WriteLine($"\tMigration Id: {item.Key}, Repositories: [{string.Join(",", item.Value)}]");
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
            Console.WriteLine(config.ToString());
            Console.WriteLine($"Starting backup of Github organization");
            DateTime startTime = DateTime.Now;
            await BackupArchive();
            await DeleteWeeklyBlobs();
            await DeleteMonthlyBlobs();
            TimeSpan totalRunTime = DateTime.Now - startTime;
            Console.WriteLine($"MS-Continuus run complete. Started at {startTime.ToString()}, finnished at {DateTime.Now.ToString()}, total run time: {totalRunTime.ToString()}");
        }
    }
}
