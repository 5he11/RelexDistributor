using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace CryptoDistributor
{
    [FunctionOutput]
    public class BalanceOfOutputDTO : IFunctionOutputDTO
    {
        [Parameter("uint256", "balance", 1)]
        public BigInteger Balance { get; set; }
    }
}
