using Entities;
using System.Collections.Concurrent;


namespace VendingMachineApi.Data;

public class InMemoryVendingStore
{
    public ConcurrentDictionary<Guid, VendingMachine> Machines { get; } = new();
    public ConcurrentDictionary<Guid, Product> Products { get; } = new();
    public ConcurrentDictionary<Guid, Inventory> Inventories { get; } = new();
    public ConcurrentDictionary<Guid, Transaction> Transactions { get; } = new();
    public ConcurrentDictionary<Guid, Payment> Payments { get; } = new();
    public SemaphoreSlim HardwareLock { get; } = new(1, 1);

    public InMemoryVendingStore()
    {
        SeedData();
    }

    private void SeedData()
    {
        var machineId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Machines[machineId] = new VendingMachine
        {
            MachineId = machineId,
            Location = "Building A - Lobby",
            Status = MachineStatus.ACTIVE
        };

        var colaId = Guid.Parse("22222222-2222-2222-2222-222222222221");
        var chipsId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Products[colaId] = new Product { ProductId = colaId, Name = "Sparkling Cola", Price = 2.50m, Category = "Beverages" };
        Products[chipsId] = new Product { ProductId = chipsId, Name = "Sea Salt Chips", Price = 1.75m, Category = "Snacks" };

        var inv1 = Guid.NewGuid();
        var inv2 = Guid.NewGuid();
        Inventories[inv1] = new Inventory { InventoryId = inv1, MachineId = machineId, ProductId = colaId, SlotNumber = 1, Quantity = 10 };
        Inventories[inv2] = new Inventory { InventoryId = inv2, MachineId = machineId, ProductId = chipsId, SlotNumber = 2, Quantity = 15 };

        // Manchine 2 details
        var machineId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Machines[machineId2] = new VendingMachine
        {
            MachineId = machineId,
            Location = "Building B - Lobby",
            Status = MachineStatus.ACTIVE
        };

        var colaId2 = Guid.Parse("22222222-2222-2222-2222-222222222223");
        var chipsId2 = Guid.Parse("22222222-2222-2222-2222-222222222224");

        Products[colaId2] = new Product { ProductId = colaId2, Name = "Sparkling water", Price = 2.50m, Category = "Beverages" };
        Products[chipsId2] = new Product { ProductId = chipsId2, Name = "Tortila Chips", Price = 1.75m, Category = "Snacks" };

        var inv3 = Guid.NewGuid();
        var inv4 = Guid.NewGuid();
        Inventories[inv3] = new Inventory { InventoryId = inv3, MachineId = machineId2, ProductId = colaId2, SlotNumber = 1, Quantity = 5 };
        Inventories[inv4] = new Inventory { InventoryId = inv4, MachineId = machineId2, ProductId = chipsId2, SlotNumber = 2, Quantity = 10 };

    }
}
