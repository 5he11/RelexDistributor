using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace CryptoDistributor
{
    public class Transaction : TableEntity
    {
        public string From { get; set; }

        public string Contract { get; set; }

        public double Amount { get; set; }

        public int Gas { get; set; }

        public bool Confirmed { get; set; }

        public string Reference { get; set; }

        public DateTimeOffset Creation { get; set; }

        public static string BuildPartitionKey(string to) => to.Trim();

        public static string BuildRowKey(string hash) => hash.Trim();
    }
}
