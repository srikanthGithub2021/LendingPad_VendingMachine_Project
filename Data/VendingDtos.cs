using Entities;



namespace Data
{
    public class VendingDtos
    {
        public record CreateTransactionRequest(Guid MachineId, Guid ProductId);

        public record ProcessPaymentRequest(
            Guid TransactionId,
            PaymentMethod Method,
            decimal AmountProvided,
            string? PaymentToken = null // Card/UPI token from frontend terminal
        );

        public record ThirdPartyPaymentRequest(
            Guid TransactionId,
            decimal Amount,
            string Currency,
            string PaymentMethod,
            string? Token
        );

        public record ThirdPartyPaymentResponse(
            bool IsApproved,
            string ReferenceId,
            string Message
        );

        public record VendingOperationResult(
            bool Success,
            string Message,
            System.Transactions.Transaction? Transaction = null,
            Payment? Payment = null
        );
    }
}
