using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using System.Collections.Generic;

namespace ms_continuus
{
    public class BlobStorage
    {
        private static Config config = new Config();
        private static BlobServiceClient blobServiceClient = new BlobServiceClient(config.STORAGE_KEY);
        private BlobContainerClient containerClient;

        public async Task EnsureContainer()
        {
            try
            {
                Console.WriteLine($"Ensuring Blob container '{config.BLOB_CONTAINER}'...");
                BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(config.BLOB_CONTAINER);
                Console.WriteLine("Done!");
                containerClient = container;
                return;
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode.Equals("ContainerAlreadyExists"))
                {
                    containerClient = blobServiceClient.GetBlobContainerClient(config.BLOB_CONTAINER);
                    return;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.InnerException.Message);
                Console.WriteLine(error.InnerException.StackTrace);
                Environment.Exit(1);
                return;
            }

        }

        public async Task UploadArchive(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            Console.WriteLine($"Uploading to Blob storage as blob:\n" +
                $"\t{config.BLOB_CONTAINER}/{fileName}\n" +
                $"\tmetadata: {{ retention: {config.BLOB_TAG} }}");
            using FileStream uploadFileStream = File.OpenRead(filePath);
            await blobClient.UploadAsync(uploadFileStream, true);
            uploadFileStream.Close();

            metadata["retention"] = config.BLOB_TAG;
            await blobClient.SetMetadataAsync(metadata);
            Console.WriteLine($"Done!");
        }

        public async Task<List<BlobItem>> ListBlobs()
        {
            List<BlobItem> blobList = new List<BlobItem>();
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(BlobTraits.All))
            {
                blobList.Add(blobItem);
            }
            return blobList;
        }
        public void DeleteArchive(string fileName)
        {
            containerClient.DeleteBlob(fileName);
            Console.WriteLine($"Deleted blob {fileName}");
        }

        public async Task DeleteArchivesBefore(DateTime before, string tag)
        {
            List<BlobItem> blobList = await ListBlobs();
            List<BlobItem> toBeDeleted = new List<BlobItem>();

            foreach(BlobItem blobItem in blobList){
                var metadata = blobItem.Metadata;
                string defaultValue;
                metadata.TryGetValue("retention", out defaultValue);
                if(defaultValue == tag){
                    if(blobItem.Properties.CreatedOn < before){
                        DeleteArchive(blobItem.Name);
                    }
                }
            }

        }
    }
}
