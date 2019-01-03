namespace CryptoDistributor
{
    public class AirdropRequest
    {
        public string From { get; set; }

        public string To { get; set; }

        public string Contract { get; set; }

        public double Amount { get; set; }

        public string Reference { get; set; }

        public long Nonce { get; set; }
    }
}
