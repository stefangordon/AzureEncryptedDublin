namespace AzureEncryptedDublin
{
    using System;
    using System.IO;
    using Microsoft.Azure;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Demonstrates how to use encryption with the Azure Blob service.
    /// </summary>
    public class Program
    {
        const string DemoContainer = "dublin";
        static IKey DemoKey;

        const string DemoMessage = "Brace yourselves.  Winter is coming.";

        static void Main(string[] args)
        {
            Console.WriteLine("Blob encryption sample");

            // Setup a blob container reference
            string storageConnectionString = CloudConfigurationManager.GetSetting("StorageConnectionString");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));
            container.Create();

            // Upload and download
            UploadEncrypted(container);
            DownloadEncrypted(container);

            // Cleanup and wait
            Cleanup(container);
            Console.WriteLine("Press enter key to exit");
            Console.ReadLine();
        }

        #region clear

        static void Upload(CloudBlobContainer container)
        {
            // Reference a new blob
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");
            
            Console.WriteLine("Uploading the blob.");

            // Upload the contents to the blob.
            blob.UploadText(DemoMessage);
        }

        static void Download(CloudBlobContainer container)
        {
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");
           
            Console.WriteLine("Downloading the blob.");

            // Download and decrypt the encrypted contents from the blob.
            var result = blob.DownloadText();

            Console.WriteLine(result);
        }

        #endregion

        #region encrypted

        static void UploadEncrypted(CloudBlobContainer container)
        {
            // Reference a new blob
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

            #region Encryption        
            // Create the IKey used for encryption.  IKey is an interface for keys provided in the KeyVault namespace.
            DemoKey = new RsaKey("private:key1");

            // Create the encryption policy to be used for upload.
            BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(DemoKey, null);

            // Set the encryption policy on the request options.
            BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };
            #endregion

            Console.WriteLine("Uploading the blob.");

            // Upload the contents to the blob.
            blob.UploadText(DemoMessage, null, null, uploadOptions, null);
        }

        static void DownloadEncrypted(CloudBlobContainer container)
        {
            CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

            // Download the encrypted blob.
            // For downloads, a resolver can be set up that will help pick the key based on the key id.
            LocalResolver resolver = new LocalResolver();
            resolver.Add(DemoKey);

            BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

            // Set the decryption policy on the request options.
            BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

            Console.WriteLine("Downloading the encrypted blob.");

            var result = blob.DownloadText(null, null, downloadOptions, null);

            Console.WriteLine(result);
        }

        #endregion

        #region miscellaneous
        static void Cleanup(CloudBlobContainer container)
        {
            container.DeleteIfExists();
        }
        #endregion

    }
}