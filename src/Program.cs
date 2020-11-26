using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ms_continuus
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            // Api api = new Api();

            // Migration mig = await api.StartMigration();
            // var temp = await api.ListMigrations();
            // Console.WriteLine(temp.ToString());
            // var arc = await api.DownloadArchive(440277);
            // var arc = await api.MigrationStatus(440329);
            // Console.WriteLine(arc.ToString());
            
            
            BlobStorage blobStorage = new BlobStorage();
            await blobStorage.CreateContainer();
            Console.WriteLine("123");
        }
    }
}
