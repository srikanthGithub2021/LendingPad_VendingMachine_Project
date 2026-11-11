namespace Entities
{


    public enum MachineStatus { ACTIVE, OUT_OF_SERVICE, MAINTENANCE }
    public enum TransactionStatus { IDLE, SELECTED, PAID, DISPENSED, FAILED, CANCELLED }
    public enum PaymentMethod { CASH, CARD, UPI }
    public enum PaymentStatus { PENDING, SUCCESS, FAILED, REFUNDED }
    public enum UserType { ADMIN, CUSTOMEER, TECHNICIAN }

    public class VendingMachine
    {
        public Guid MachineId { get; set; }
        public string Location { get; set; } = string.Empty;
        public MachineStatus Status { get; set; } = MachineStatus.ACTIVE;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    }

    public class Product
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class Inventory
    {
        public Guid InventoryId { get; set; }
        public Guid MachineId { get; set; }
        public Guid ProductId { get; set; }
        public int SlotNumber { get; set; }
        public int Quantity { get; set; }
        public bool IsLocked { get; set; } // Prevents concurrent double-booking of the last item
    }

    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public Guid MachineId { get; set; }
        public Guid ProductId { get; set; }
        public int SlotNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.IDLE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Payment
    {
        public Guid PaymentId { get; set; }
        public Guid TransactionId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeReturned { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.PENDING;
        public string? GatewayReferenceId { get; set; }
    }

    public class VendingMachineUser
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public UserType userType { get; set; } = UserType.CUSTOMEER;
        public int UserTimeOutInSeconds { get; set; } = 60;
    }
}
