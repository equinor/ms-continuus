using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;

namespace ms_continuus
{
    public class BlobStorage
    {
        static  Config    config            = new Config();
        private static string    key               = config.STORAGE_KEY;
        private static BlobServiceClient blobServiceClient = new BlobServiceClient(key);


        public async Task CreateContainer()
        {
            try
            {
                var temp = await blobServiceClient.CreateBlobContainerAsync("test-archives");
                Console.WriteLine("1233");
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode.Equals("ContainerAlreadyExists"))
                {
                    return;
                }
            }
            catch (Exception error)
            {
             Console.WriteLine(error.InnerException.Message);
             Console.WriteLine(error.InnerException.StackTrace);
             Environment.Exit(1);
            }
            
        }
        
        public void UploadArchive(string filePath){
            
            }
            
        public void DeleteArchive(string filePath){
         
        }
        
        public void DeleteArchivesBefore(DateTime date){
        
        }
    }
}