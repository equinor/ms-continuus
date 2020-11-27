using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading;

namespace ms_continuus
{
    class Program
    {
        static async Task Main(string[] args)
        {

            Api api = new Api();

            // Migration startedMigration = await api.StartMigration();
            // Console.WriteLine($"Started a new migration:\n\t{startedMigration}");

            Migration migStatus = await api.MigrationStatus(440781);
            // Migration migStatus = await api.MigrationStatus(startedMigration.id);
            int counter = 0;
            int sleepIntervalSeconds = 5;
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
            await blobStorage.CreateContainer();
            await blobStorage.UploadArchive(archivePath);
            // var blobList = await blobStorage.ListBlobs();
            Console.WriteLine(123);
        }
    }
}
