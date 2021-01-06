using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace ms_continuus
{
    class Program
    {
        private static Config config = new Config();
        static async Task DeleteWeeklyBlobs(){
            DateTime olderThan = Utility.DateMinusDays(config.WEEKLY_RETENTION);
            Console.WriteLine($"Deleting blobs with retention='weekly' older than {olderThan}");

            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan , "weekly");
        }

        static async Task DeleteMonthlyBlobs(){
            DateTime olderThan = Utility.DateMinusDays(config.MONTHLY_RETENTION);
            Console.WriteLine($"Deleting blobs with retention='monthly' older than {olderThan}");

            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.DeleteArchivesBefore(olderThan , "monthly");

        }

        static async Task BackupArchive(){
            Api             api               = new Api();
            List<Migration> startedMigrations = new List<Migration>();
            List<string>    allRepositoryList = new List<string>();

            Console.WriteLine("Fetching all repositories...");
            allRepositoryList = await api.ListRepositories();

            int chunks = allRepositoryList.Count / 100;
            int remainder = allRepositoryList.Count % 100;

            // Start the smallest migration first (remainder)
            Console.WriteLine($"Starting migration of {allRepositoryList.Count} repositories divided in {chunks + 1} chunks");
            startedMigrations.Add(await api.StartMigration(allRepositoryList.GetRange((chunks*100), remainder)));

            // TODO: More gracefull error handling and continuation
            for(int i = 0; i < chunks; i++){
                List<string> chunkedRepositoryList = allRepositoryList.GetRange(i,100);
                startedMigrations.Add(await api.StartMigration(chunkedRepositoryList));
            }

            // Iterate through all the started migrations, wait for them to complete,
            // download them, and upload them to blob-storage
            int migrationIndex = 0;
            foreach(Migration migration in startedMigrations){
                Migration migStatus = await api.MigrationStatus(migration.id);
                int exportTimer = 0;
                int sleepIntervalSeconds = 30;
                while (migStatus.state != "exported")
                {
                    Thread.Sleep(sleepIntervalSeconds*1000);
                    migStatus = await api.MigrationStatus(migStatus.id);
                    if (migStatus.state == "failed"){
                        throw new Exception("The migration failed...");
                    }
                    exportTimer++;
                    Console.WriteLine($"Waiting for migration to be ready... {exportTimer * sleepIntervalSeconds} seconds");
                }

                Console.WriteLine($"Ready;\t{migStatus}");
                string archivePath = await api.DownloadArchive(migStatus.id, migrationIndex);
                migrationIndex++;

                BlobStorage blobStorage = new BlobStorage();
                await blobStorage.EnsureContainer();
                await blobStorage.UploadArchive(archivePath);
            }
            Console.WriteLine($"Successfully uploaded archives of {allRepositoryList.Count} repositories");
        }

        static async Task Main(string[] args)
        {
            await BackupArchive();
            // await DeleteWeeklyBlobs();
            // await DeleteMonthlyBlobs();
        }
    }
}
