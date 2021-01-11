using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using System.Collections.Generic;
using System.Threading;

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
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode.Equals("ContainerAlreadyExists"))
                {
                    containerClient = blobServiceClient.GetBlobContainerClient(config.BLOB_CONTAINER);
                }
                if (error.ErrorCode.Equals("InvalidResourceName"))
                {
                    throw new ArgumentException($"The specifed resource name contains invalid characters. '{config.BLOB_CONTAINER}'");
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.InnerException.Message);
                Console.WriteLine(error.InnerException.StackTrace);
                Environment.Exit(1);
            }

        }

        public async Task UploadArchive(string filePath)
        {
            DateTime timeStarted = DateTime.Now;
            int retryInterval = 30000;
            int attempts = 1;
            string fileName = Path.GetFileName(filePath);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            Console.WriteLine($"Uploading to Blob storage as:\n" +
                $"\t{config.BLOB_CONTAINER}/{fileName}\n" +
                $"\tmetadata: {{ retention: {config.BLOB_TAG} }}");
            using FileStream uploadFileStream = File.OpenRead(filePath);
            long fileSize = uploadFileStream.Length;
            Console.WriteLine($"\tsize: {Utility.BytesToString(fileSize)}");

            while (attempts < 3)
            {
                try
                {
                    await blobClient.UploadAsync(uploadFileStream, true);
                    uploadFileStream.Close();
                    metadata["retention"] = config.BLOB_TAG;
                    await blobClient.SetMetadataAsync(metadata);
                    Console.WriteLine($"\tDone!");
                    Console.WriteLine($"\tAverage upload speed: {Utility.TransferSpeed(fileSize, timeStarted)}");
                    Console.WriteLine($"\tDeleting file from disk...");
                    File.Delete(filePath);
                    return;
                }
                catch (AggregateException agEx)
                {
                    var firstException = agEx.InnerExceptions[agEx.InnerExceptions.Count - 1];
                    Console.WriteLine($"WARNING: Failed to upload archive to blob storage ({firstException.Message}). Retrying in {retryInterval / 1000} seconds");
                    Thread.Sleep(retryInterval);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"WARNING: Failed to upload archive to blob storage ({e.Message}). Retrying in {retryInterval / 1000} seconds");
                    Thread.Sleep(retryInterval);
                }
                attempts++;
            }
            throw new Exception($"Failed to upload blob '{filePath}' with {attempts} attempts.");
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

        // List every blob, if tag eq input tag, and CreatedOn is older than input date, delete it
        public async Task DeleteArchivesBefore(DateTime before, string tag)
        {
            List<BlobItem> blobList = await ListBlobs();
            List<BlobItem> toBeDeleted = new List<BlobItem>();

            foreach (BlobItem blobItem in blobList)
            {
                var metadata = blobItem.Metadata;
                string defaultValue;
                metadata.TryGetValue("retention", out defaultValue);
                if (defaultValue == tag)
                {
                    if (blobItem.Properties.CreatedOn < before)
                    {
                        DeleteArchive(blobItem.Name);
                    }
                }
            }

        }
    }
}
