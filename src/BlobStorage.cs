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
        private static readonly Config Config = new Config();
        private static readonly BlobServiceClient BlobServiceClient = new BlobServiceClient(Config.StorageKey);
        private BlobContainerClient _containerClient;

        public async Task EnsureContainer()
        {
            try
            {
                Console.WriteLine($"Ensuring Blob container '{Config.BlobContainer}'...");
                BlobContainerClient container = await BlobServiceClient.CreateBlobContainerAsync(Config.BlobContainer);
                Console.WriteLine("Done!");
                _containerClient = container;
            }
            catch (RequestFailedException error)
            {
                if (error.ErrorCode.Equals("ContainerAlreadyExists"))
                {
                    _containerClient = BlobServiceClient.GetBlobContainerClient(Config.BlobContainer);
                }
                if (error.ErrorCode.Equals("InvalidResourceName"))
                {
                    throw new ArgumentException($"The specifed resource name contains invalid characters. '{Config.BlobContainer}'");
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
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            Console.WriteLine($"Uploading to Blob storage as:\n" +
                $"\t{Config.BlobContainer}/{fileName}\n" +
                $"\tmetadata: {{ retention: {Config.BlobTag} }}");
            using FileStream uploadFileStream = File.OpenRead(filePath);
            long fileSize = uploadFileStream.Length;
            Console.WriteLine($"\tsize: {Utility.BytesToString(fileSize)}");

            while (attempts < 3)
            {
                try
                {
                    await blobClient.UploadAsync(uploadFileStream, true);
                    uploadFileStream.Close();
                    metadata["retention"] = Config.BlobTag;
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
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync(BlobTraits.All))
            {
                blobList.Add(blobItem);
            }
            return blobList;
        }
        public void DeleteArchive(string fileName)
        {
            _containerClient.DeleteBlob(fileName);
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
