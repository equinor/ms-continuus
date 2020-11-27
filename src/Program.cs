using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;

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
        static async Task BackupArchive(){
            Api api = new Api();

            Migration startedMigration = await api.StartMigration();

            Migration migStatus = await api.MigrationStatus(startedMigration.id);
            int counter = 0;
            int sleepIntervalSeconds = 15;
            while (migStatus.state == "exporting")
            {
                counter++;
                Console.WriteLine($"Waiting for migration to be ready... {counter * sleepIntervalSeconds} seconds");
                Thread.Sleep(sleepIntervalSeconds*1000);
                migStatus = await api.MigrationStatus(migStatus.id);
            }
            Console.WriteLine($"Ready;\n\t{migStatus}");

            string archivePath = await api.DownloadArchive(migStatus.id);

            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.UploadArchive(archivePath);
        }

        static async Task Main(string[] args)
        {
            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.EnsureContainer();
            await blobStorage.UploadArchive("tmp/archive-26_11_2020-440277.tar.gz");
            // await BackupArchive();
            // Api api = new Api();
            // var tmp = await api.ListMigrations();
            await DeleteWeeklyBlobs();

            // Console.Write(tmp);

        }
    }
}
