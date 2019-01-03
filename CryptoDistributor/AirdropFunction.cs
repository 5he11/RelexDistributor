using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoDistributor
{
    public static class AirdropFunction
    {
        /* An example of the content of a distribution spreadsheet in CSV format
        destination address,quantity,contract address,source private key
        0x65513ecd11fd3a5b1fefdcc6a500b025008405a2,100000,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x555ee11fbddc0e49a9bab358a8941ad95ffdb48f,200000.123,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0xe08f0bccbca8192620259aa402b29f7b862575d3,300000,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x7ed638621dbb927c947b0ca064abd051cdc93124,12345.654,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x81b7e08f65bdf5648606c89998a9cc8164397647,65432,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x8a91de6b7625a1c0940f4dae084d864c3ce5fe0c,123456,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x65513ecd11fd3a5b1fefdcc6a500b025008405a2,98765.456789,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x555ee11fbddc0e49a9bab358a8941ad95ffdb48f,123,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x9e737dfc1da73c1c0a0c3ca43bb036966003c471,321,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x229115f344a13defba6470d61a3182b60c0d4979,4567.1234,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x63243370ed17a16a1c212b265b4514cbdce23e6f,12,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        0x23a4cc796859203c5920a3c727c24b2aaf80f407,1.2,0x4a42d2c580f83dce404acad18dab26db11a1750e,c215231c3b5e8baa4f231e70bf0a981a4b0f8c974bebb3ba54ede4f27e2d0xxx
        */

        //decimals of Relex Token. this should NOT be changed
        private const int ERC20Decimals = 18;

        [FunctionName("Distribute")]
        public static async Task Distribute([BlobTrigger("airdrop/{name}")]Stream myBlob, string name, ILogger log)
        {
            //reads distribution addresses and amount line by line from the CSV file input
            var distributions = ReadLines(myBlob, Encoding.UTF8);

            var requests = new List<AirdropRequest>();
            foreach (var distribution in distributions)
            {
                //ignores any empty line
                if (String.IsNullOrWhiteSpace(distribution))
                {
                    continue;
                }

                //treats the first parts of lines seperated by comma as distribution addresses, the second as amount
                var parts = distribution.Split(',');
                if (parts.Length != 4
                    || !parts[0].Trim().StartsWith("0x")
                    || !Double.TryParse(parts[1], out var amount)
                    || !parts[2].Trim().StartsWith("0x")
                    || parts[3].Trim().StartsWith("0x"))
                {
                    log.LogDebug("Invalid distribution found: {0}", distribution);
                    continue;
                }
                requests.Add(new AirdropRequest
                {
                    Amount = amount,
                    Contract = parts[2].Trim(),
                    From = parts[3].Trim(),
                    To = parts[0].Trim(),
                    Reference = name
                });
            }


            var daemonURL = ConfigurationProvider.GetSetting("DaemonURL");
            var daemonCredential = ConfigurationProvider.GetSetting("DaemonCredential");

            var froms = requests.GroupBy(r => r.From).Select(g => g.Key).Distinct();
            var nonces = new Dictionary<string, long>();
            foreach (var from in froms)
            {
                var account = new Account(from);
                var client = new Web3(new Account(from),
                   url: daemonURL,
                   authenticationHeader: new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(daemonCredential))));
                var nonce = await client.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address);
                nonces.Add(from, (long)nonce.Value);
            }

            var queue = CloudStorage.GetQueue("airdrop");
            foreach (var request in requests)
            {
                request.Nonce = nonces[request.From];
                var msg = JsonConvert.SerializeObject(request);
                await queue.AddMessageAsync(new CloudQueueMessage(msg));
                nonces[request.From] = request.Nonce + 1;
            }
        }

        [FunctionName("Airdrop")]
        public static async Task Airdrop([QueueTrigger("airdrop")]string msg, ILogger log)
        {
            var request = JsonConvert.DeserializeObject<AirdropRequest>(msg);

            var daemonURL = ConfigurationProvider.GetSetting("DaemonURL");
            var daemonCredential = ConfigurationProvider.GetSetting("DaemonCredential");

            //the real transactional amount needs to be multiplied by the decimals of Relex Token
            var amountInWei = request.Amount * (long)Math.Pow(10, ERC20Decimals);

            //initialize an Ethereum RPC client and build a standard ERC20 contract for use to interact with
            var account = new Account(request.From);

            var client = new Web3(account,
                url: daemonURL,
                authenticationHeader: new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(daemonCredential))));

            //builds and sends token transfering request
            //standard ERC20 tokens can be transfered by calling the 'transfer' method defined in their contract
            var transferHandler = client.Eth.GetContractTransactionHandler<TransferFunction>();
            var transfer = new TransferFunction()
            {
                To = request.To,
                TokenAmount = new BigInteger(amountInWei),
                Nonce = request.Nonce
            };

            log.LogInformation($"Sending {request.Amount} tokens({request.Contract}) to {request.To}... ");

            await transferHandler.SendRequestAsync(request.Contract, transfer);

            var receipt = await transferHandler.SendRequestAndWaitForReceiptAsync(request.Contract, transfer);
            if (receipt == null || (receipt.HasErrors().HasValue && receipt.HasErrors().Value))
            {
                log.LogError("Failed.");
                throw new ApplicationException("Failed in processing airdrop request.");
            }
            else if (receipt.HasErrors().HasValue && receipt.HasErrors().Value)
            {
                log.LogError($"Failed with status code: {(int)receipt.Status.Value}");
                var errormsg = receipt.Logs.ToString();
                log.LogError(errormsg);
                throw new ApplicationException(errormsg);
            }
            else
            {
                var transaction = new Transaction
                {
                    Amount = request.Amount,
                    Confirmed = false,
                    Contract = request.Contract,
                    Creation = DateTimeOffset.UtcNow,
                    From = request.From,
                    PartitionKey = request.To,
                    RowKey = receipt.TransactionHash,
                    Reference = request.Reference,
                    Gas = (int)receipt.GasUsed.Value
                };
                log.LogInformation($"Done. Transaction hash: {receipt.TransactionHash}.");
                await CloudStorage.GetTable<Transaction>().InsertAsync(new[] { transaction });
                await CloudStorage.GetQueue("verify").AddMessageAsync(new CloudQueueMessage($"{transaction.PartitionKey},{transaction.RowKey}"));
            }
        }

        [FunctionName(nameof(Verify))]
        public static async Task Verify([QueueTrigger("verify")]string msg, ILogger log)
        {
            if (String.IsNullOrWhiteSpace(msg))
            {
                log.LogError("Empty request received.");
            }

            var parts = msg.Split(",");
            if (parts.Length != 2)
            {
                log.LogError("Invalid request received.");
            }

            var daemonURL = ConfigurationProvider.GetSetting("DaemonURL");
            var daemonCredential = ConfigurationProvider.GetSetting("DaemonCredential");
            var client = new Web3(url: daemonURL,
                   authenticationHeader: new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(daemonCredential))));
            var table = CloudStorage.GetTable<Transaction>();
            var transaction = await table.RetrieveAsync<Transaction>(parts[0], parts[1]);

            var receipt = await client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transaction.RowKey);
            if (receipt != null && receipt.Status.Value == 1)
            {
                if (receipt.Status.Value == 1)
                {
                    log.LogInformation("Transaction {0} has been verified in success state.", transaction.RowKey);
                    transaction.Confirmed = true;
                    await table.ReplaceAsync(new[] { transaction });
                }
                else
                {
                    var errormsg = $"Transaction {transaction.RowKey} has not yet been verified.";
                    log.LogInformation(errormsg);
                    throw new ApplicationException(errormsg);
                }
            }
        }

        private static IList<string> ReadLines(Stream stream, Encoding encoding)
        {
            var result = new List<string>();
            using (stream)
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    result.Add(line);
                }
            }
            return result;
        }
    }
}
