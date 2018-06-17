using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace RelexDistributor
{
    class Program
    {
        //standard ERC20 ABI. this should NOT be changed
        private const string ERC20ABI = "[{\"constant\":true,\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_from\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burnFrom\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_spender\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"},{\"name\":\"_extraData\",\"type\":\"bytes\"}],\"name\":\"approveAndCall\",\"outputs\":[{\"name\":\"success\",\"type\":\"bool\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"\",\"type\":\"address\"},{\"name\":\"\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"name\":\"\",\"type\":\"uint256\"}],\"payable\":false,\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"name\":\"initialSupply\",\"type\":\"uint256\"},{\"name\":\"tokenName\",\"type\":\"string\"},{\"name\":\"tokenSymbol\",\"type\":\"string\"}],\"payable\":false,\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"name\":\"to\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"name\":\"from\",\"type\":\"address\"},{\"indexed\":false,\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Burn\",\"type\":\"event\"}]";

        //Relex token's contract address on the Ethereum network. this should NOT be changed
        private const string RelexContractAddress = "0x4a42d2c580f83dce404acad18dab26db11a1750e";

        //decimals of Relex Token. this should NOT be changed
        private const int RelexDecimals = 18;

        //URL to the HTTP RPC interface hosted by a geth setup
        private const string DaemonUrl = "http://127.0.0.1:8545";

        async static Task Main(string[] args)
        {
            Console.WriteLine("Launching Relex distributor...");
            Console.WriteLine("You may terminate this application at any time by hitting CTRL+C");
            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();

            //parsing parameters from user's command line arguments
            string fileUrl;
            do
            {
                Console.Write("Full path of your distribution spreadsheet in CSV format: ");
                fileUrl = Console.ReadLine().Trim();
            } while (!File.Exists(fileUrl));

            string from;
            do
            {
                Console.Write("The account to distribute Relex from: ");
                from = Console.ReadLine().Trim();
            } while (string.IsNullOrWhiteSpace(from));

            string passphrase;
            do
            {
                Console.Write("The passphrase of this account: ");
                passphrase = Console.ReadLine().Trim();
            } while (string.IsNullOrWhiteSpace(passphrase));

            //initialize an Ethereum RPC client and build a standard ERC20 contract for use to interact with
            var client = new Web3(DaemonUrl);
            var contract = client.Eth.GetContract(ERC20ABI, RelexContractAddress);

            //this list stores all transaction hash from the distribution
            var transactions = new List<string>();

            //reads distribution addresses and amount line by line from the CSV file input
            var distributions = File.ReadAllText(fileUrl).Split('\n');
            foreach (var distribution in distributions)
            {
                //ignores any empty line
                if (string.IsNullOrWhiteSpace(distribution))
                {
                    continue;
                }

                //treats the first parts of lines seperated by comma as distribution addresses, the second as amount
                var parts = distribution.Split(',');
                if (parts.Length != 2
                    || !parts[0].Trim().StartsWith("0x")
                    || !decimal.TryParse(parts[1], out decimal amount)
                    || amount <= 0)
                {
                    Console.WriteLine("Invalid distribution found: {0}", distribution);
                    continue;
                }
                var to = parts[0].Trim();

                Console.Write("Sending {0} Relex tokens to {1}... ", amount, to);

                //the real transactional amount needs to be multiplied by the decimals of Relex Token
                amount *= (long)Math.Pow(10, RelexDecimals);

                //builds and sends token transfering request
                //standard ERC20 tokens can be transfered by calling the 'transfer' method defined in their contract
                var function = contract.GetFunction("transfer");
                var amountInEthereumFormat = new BigInteger(amount);
                var gas = await function.EstimateGasAsync(from, null, null, to, amountInEthereumFormat);
                await client.Personal.UnlockAccount.SendRequestAsync(from, passphrase, 120);
                var transactionID = await function.SendTransactionAsync(from, gas, null, null, to, amountInEthereumFormat);
                var result = await client.Personal.LockAccount.SendRequestAsync(from);
                if (result)
                {
                    transactions.Add(transactionID);
                    Console.WriteLine("Done. Transaction ID: {0}", transactionID);
                }
                else
                {
                    Console.WriteLine("Failed!");
                }
            }

            Console.WriteLine("Finished processing of {0}, press ENTER to start verifying the transactions have just made...", fileUrl);
            Console.ReadLine();

            //loop until all transactions have been verified
            while (transactions.Count > 0)
            {
                //makes a copy of the "to-verify-list" for enumeration as we may need to manipulate this list within the enumeration
                var transactionsCopy = new List<string>(transactions);
                foreach (var transactionID in transactionsCopy)
                {
                    var receipt = await client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionID);
                    if (receipt != null && receipt.Status.Value == 1)
                    {
                        if (receipt.Status.Value == 1)
                        {
                            Console.WriteLine("Transaction {0} has been verified in success state.", transactionID);
                        }
                        else
                        {
                            Console.WriteLine("Transaction {0} has been verified in failure status, please have a manual check on it.", transactionID);
                        }

                        //A transaction should be removed from the "to-verify-list" once we get its transaction receipt, whether it's successful or not
                        transactions.Remove(transactionID);
                    }
                }

                if (transactions.Count > 0)
                {
                    Console.WriteLine("Transaction verification is still in progress, {0} transaction left to go...", transactions.Count);
                    Thread.Sleep(5000);
                }
            }

            Console.WriteLine("All jobs are done. Press ENTER to exit...");
            Console.ReadLine();
        }
    }
}
