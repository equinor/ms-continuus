using System;
using System.Threading.Tasks;
using System.Threading;

namespace ms_continuus
{
    class Program
    {
        static async Task BackupArchive(){
            Api api = new Api();

            Migration startedMigration = await api.StartMigration();

            // Migration migStatus = await api.MigrationStatus(440781);
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
            await blobStorage.CreateContainer();
            await blobStorage.UploadArchive(archivePath);
            // var blobList = await blobStorage.ListBlobs();
            Console.WriteLine(123);
        }
        static async Task Main(string[] args)
        {
            await BackupArchive();
            // Api api = new Api();
            // var tmp = await api.ListMigrations();


            // Console.Write(tmp);

        }
    }
}
