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
        private static readonly Config Config = new();
        private static readonly BlobServiceClient BlobServiceClient = new(Config.StorageKey);
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
                _containerClient = error.ErrorCode switch
                {
                    "ContainerAlreadyExists" => BlobServiceClient.GetBlobContainerClient(Config.BlobContainer),
                    "InvalidResourceName" => throw new ArgumentException(
                        $"The specified resource name contains invalid characters. '{Config.BlobContainer}'"
                    ),
                    _ => _containerClient
                };
            }
            catch (Exception error)
            {
                Console.WriteLine(error.InnerException?.Message);
                Console.WriteLine(error.InnerException?.StackTrace);
                Environment.Exit(1);
            }
        }

        public async Task UploadArchive(string filePath)
        {
            var timeStarted = DateTime.Now;
            const int retryInterval = 30_000;
            var attempts = 1;
            var fileName = Path.GetFileName(filePath);
            var blobClient = _containerClient.GetBlobClient(fileName);
            var metadata = new Dictionary<string, string>();

            Console.WriteLine(
                $"Uploading to Blob storage as:\n" +
                $"\t{Config.BlobContainer}/{fileName}\n" +
                $"\tmetadata: {{ retention: {Config.BlobTag} }}"
            );
            await using var uploadFileStream = File.OpenRead(filePath);
            var fileSize = uploadFileStream.Length;
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
                    var firstException = agEx.InnerExceptions[^1];
                    Console.WriteLine(
                        $"WARNING: Failed to upload archive to blob storage ({firstException.Message}). Retrying in {retryInterval / 1000} seconds"
                    );
                    Thread.Sleep(retryInterval);
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"WARNING: Failed to upload archive to blob storage ({e.Message}). Retrying in {retryInterval / 1000} seconds");
                    Thread.Sleep(retryInterval);
                }

                attempts++;
            }

            throw new Exception($"Failed to upload blob '{filePath}' with {attempts} attempts.");
        }

        public async Task<List<BlobItem>> ListBlobs()
        {
            var blobList = new List<BlobItem>();
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
            var blobList = await ListBlobs();
            var toBeDeleted = new List<BlobItem>();

            foreach (var blobItem in blobList)
            {
                var metadata = blobItem.Metadata;
                metadata.TryGetValue("retention", out var defaultValue);
                if (defaultValue != tag) continue;
                if (blobItem.Properties.CreatedOn < before)
                {
                    DeleteArchive(blobItem.Name);
                }
            }
        }
    }
}
