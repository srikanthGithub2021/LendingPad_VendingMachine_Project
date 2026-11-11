using Entities;
using Data;


using static Data.VendingDtos;
using VendingMachineApi.Data;


namespace VendingMachineApi.Services;

public class VendingService
{
    private readonly InMemoryVendingStore _store;
    private readonly IThirdPartyPaymentClient _paymentClient;
    private readonly IHardwareController _hardware;

    public VendingService(
        InMemoryVendingStore store,
        IThirdPartyPaymentClient paymentClient,
        IHardwareController hardware)
    {
        _store = store;
        _paymentClient = paymentClient;
        _hardware = hardware;
    }

    public IEnumerable<Product> GetProducts(Guid machineId)
    {
        var productIds = _store.Inventories.Values
            .Where(i => i.MachineId == machineId)
            .Select(i => i.ProductId)
            .ToHashSet();

        return _store.Products.Values.Where(p => productIds.Contains(p.ProductId));
    }

    public IEnumerable<Inventory> GetInventory(Guid machineId) =>
        _store.Inventories.Values.Where(i => i.MachineId == machineId).OrderBy(i => i.SlotNumber);

    public async Task<VendingOperationResult> CreateTransactionAsync(CreateTransactionRequest req)
    {
        await _store.HardwareLock.WaitAsync();
        try
        {
            if (!_store.Machines.TryGetValue(req.MachineId, out var machine) || machine.Status != MachineStatus.ACTIVE)
                return new VendingOperationResult(false, "Vending machine is unavailable.");

            if (!_store.Products.TryGetValue(req.ProductId, out var product))
                return new VendingOperationResult(false, "Selected product does not exist.");

            var slot = _store.Inventories.Values
                .FirstOrDefault(i => i.MachineId == req.MachineId && i.ProductId == req.ProductId && i.Quantity > 0 && !i.IsLocked);

            if (slot == null)
                return new VendingOperationResult(false, "Product is currently out of stock or locked by another session.");

            // Lock slot during active checkout
            slot.IsLocked = true;

            var tx = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                MachineId = req.MachineId,
                ProductId = req.ProductId,
                SlotNumber = slot.SlotNumber,
                TotalAmount = product.Price,
                Status = TransactionStatus.SELECTED,
                CreatedAt = DateTime.UtcNow
            };

            _store.Transactions[tx.TransactionId] = tx;
             return new VendingOperationResult(true, $"Selected {product.Name}. Please pay ${product.Price:F2}.", tx);
          //  return new VendingOperationResult(true, $"Selected {product.Name}. Please pay ${product.Price:F2}.", null);
        }
        finally
        {
            _store.HardwareLock.Release();
        }
    }

    public async Task<VendingOperationResult> ProcessPaymentAndDispenseAsync(ProcessPaymentRequest req)
    {
        if (!_store.Transactions.TryGetValue(req.TransactionId, out var tx))
            return new VendingOperationResult(false, "Transaction not found.");

        if (tx.Status != TransactionStatus.SELECTED)
            return new VendingOperationResult(false, $"Cannot process payment for transaction in state {tx.Status}.");

        var slot = _store.Inventories.Values.First(i => i.MachineId == tx.MachineId && i.SlotNumber == tx.SlotNumber);

        // 1. Validate Payment Amount
        if (req.Method == PaymentMethod.CASH && req.AmountProvided < tx.TotalAmount)
        {
            decimal shortBy = tx.TotalAmount - req.AmountProvided;
            return new VendingOperationResult(false, $"Insufficient cash inserted. Inserted: ${req.AmountProvided:F2}, Required: ${tx.TotalAmount:F2}. Please insert ${shortBy:F2} more.");
        }

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            TransactionId = tx.TransactionId,
            PaymentMethod = req.Method,
            AmountPaid = req.Method == PaymentMethod.CASH ? req.AmountProvided : tx.TotalAmount,
            ChangeReturned = req.Method == PaymentMethod.CASH ? req.AmountProvided - tx.TotalAmount : 0m
        };

        // 2. Call Third-Party Payment API (for Card/UPI authorization or Cash telemetry/validation)
        var gatewayResult = await _paymentClient.ChargeAsync(new ThirdPartyPaymentRequest(
            tx.TransactionId,
            tx.TotalAmount,
            "USD",
            req.Method.ToString(),
            req.PaymentToken
        ));

        if (!gatewayResult.IsApproved)
        {
            payment.PaymentStatus = PaymentStatus.FAILED;
            _store.Payments[payment.PaymentId] = payment;
           // return new VendingOperationResult(false, $"Payment failed: {gatewayResult.Message}", tx, payment);
           return new VendingOperationResult(false, $"Payment failed: {gatewayResult.Message}",null, payment);
        }

        payment.PaymentStatus = PaymentStatus.SUCCESS;
        payment.GatewayReferenceId = gatewayResult.ReferenceId;
        tx.Status = TransactionStatus.PAID;
        _store.Payments[payment.PaymentId] = payment;

        // 3. Trigger Hardware Dispense & Drop Sensor Check
        bool dispensed = await _hardware.DispenseSlotAsync(tx.SlotNumber);

        if (!dispensed)
        {
            // Automatic Rollback & Refund if hardware jam occurs
            tx.Status = TransactionStatus.FAILED;
            slot.IsLocked = false;

            if (req.Method != PaymentMethod.CASH && payment.GatewayReferenceId != null)
            {
                await _paymentClient.RefundAsync(payment.GatewayReferenceId, tx.TotalAmount);
            }
            else
            {
                await _hardware.ReturnCashChangeAsync(req.AmountProvided);
            }

            payment.PaymentStatus = PaymentStatus.REFUNDED;
            return new VendingOperationResult(false, "Hardware dispense failed. Full refund initiated.", tx, payment);
            //return new VendingOperationResult(false, "Hardware dispense failed. Full refund initiated.", null, payment);
        }

        // 4. Return Cash Change (if any), Decrement Stock, and Complete Transaction
        if (payment.ChangeReturned > 0)
        {
            await _hardware.ReturnCashChangeAsync(payment.ChangeReturned);
        }

        slot.Quantity -= 1;
        slot.IsLocked = false;
        tx.Status = TransactionStatus.DISPENSED;

        string summaryMsg = req.Method == PaymentMethod.CASH
            ? $"Dispensed! Inserted: ${payment.AmountPaid:F2}, Change Returned: ${payment.ChangeReturned:F2}."
            : $"Dispensed! Card/UPI charged ${payment.AmountPaid:F2} (Ref: {payment.GatewayReferenceId}).";

        return new VendingOperationResult(true, summaryMsg, tx, payment);
        //return new VendingOperationResult(true, summaryMsg, null, payment);
    }

    public async Task<VendingOperationResult> CancelTransactionAsync(Guid transactionId)
    {
        if (!_store.Transactions.TryGetValue(transactionId, out var tx))
            return new VendingOperationResult(false, "Transaction not found.");

        if (tx.Status != TransactionStatus.SELECTED)
            return new VendingOperationResult(false, "Only pending transactions can be cancelled.");

        var slot = _store.Inventories.Values.FirstOrDefault(i => i.MachineId == tx.MachineId && i.SlotNumber == tx.SlotNumber);
        if (slot != null) slot.IsLocked = false;

        tx.Status = TransactionStatus.CANCELLED;
        return await Task.FromResult(new VendingOperationResult(true, "Transaction cancelled and slot unlocked.", tx));
       // return await Task.FromResult(new VendingOperationResult(true, "Transaction cancelled and slot unlocked."));
    }

    public bool UpdateStock(Guid machineId, int slotNumber, int delta)
    {
        var slot = _store.Inventories.Values.FirstOrDefault(i => i.MachineId == machineId && i.SlotNumber == slotNumber);
        if (slot == null || slot.Quantity + delta < 0) return false;

        slot.Quantity += delta;
        return true;
    }
}
