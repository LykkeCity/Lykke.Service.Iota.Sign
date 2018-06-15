namespace Lykke.Service.Iota.Sign.Core.Domain
{
    public class TransactionContext
    {
        public TransactionType Type { get; set; }
        public TransactionInput[] Inputs { get; set; }
        public TransactionOutput[] Outputs { get; set; }
    }
}
