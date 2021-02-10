using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Integration.Observability.Helpers
{
    public static class StorageHelper
    {
        /// <summary>
        /// Archives a payload as an Azure Storage blob
        /// </summary>
        /// <param name="body"></param>
        /// <param name="containerName"></param>
        /// <param name="blobName"></param>
        /// <param name="connectionString"></param>
        public static void ArchiveToBlob(string body, string containerName, string blobName, string connectionString)
        {
            BlobContainerClient container = new BlobContainerClient(connectionString, containerName);
            container.CreateIfNotExists();
            BlobClient blob = container.GetBlobClient(blobName);
            var content = Encoding.UTF8.GetBytes(body);
            using (var memoryStream = new MemoryStream(content))
            {
                blob.Upload(memoryStream);
            }
        }
    }
}
