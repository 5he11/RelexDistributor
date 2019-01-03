using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace CryptoDistributor
{
    public static class CloudStorage
    {
        public static CloudStorageAccount GetAccount()
            => CloudStorageAccount.Parse(ConfigurationProvider.GetSetting("AzureWebJobsStorage"));

        public static CloudTable GetTable<T>() where T : ITableEntity
            => GetTable(typeof(T).Name);

        public static CloudTable GetTable(string tableName)
            => GetAccount().CreateCloudTableClient().GetTableReference(tableName);

        public static CloudQueue GetQueue(string queueName)
            => GetAccount().CreateCloudQueueClient().GetQueueReference(queueName);

        public static CloudBlobContainer GetBlobContainer(string containerName)
            => GetAccount().CreateCloudBlobClient().GetContainerReference(containerName);

        public static CloudBlob GetBlob(string containerName, string blobName)
            => GetAccount().CreateCloudBlobClient().GetContainerReference(containerName).GetBlobReference(blobName);
    }
}
