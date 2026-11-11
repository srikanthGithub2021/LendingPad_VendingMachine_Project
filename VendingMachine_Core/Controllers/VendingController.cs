using Microsoft.AspNetCore.Mvc;

using VendingMachineApi.Services;
using static Data.VendingDtos;

namespace VendingMachineApi.Controllers;

[ApiController]
[Route("api/v1")]
public class VendingController : ControllerBase
{
    private readonly VendingService _vendingService;

    public VendingController(VendingService vendingService)
    {
        _vendingService = vendingService;
    }

    [HttpGet("machines/{machineId:guid}/products")]
    public IActionResult GetProducts(Guid machineId) => Ok(_vendingService.GetProducts(machineId));

    [HttpGet("machines/{machineId:guid}/inventory")]
    public IActionResult GetInventory(Guid machineId) => Ok(_vendingService.GetInventory(machineId));

    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var result = await _vendingService.CreateTransactionAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var result = await _vendingService.ProcessPaymentAndDispenseAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("transactions/{transactionId:guid}/cancel")]
    public async Task<IActionResult> CancelTransaction(Guid transactionId)
    {
        var result = await _vendingService.CancelTransactionAsync(transactionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("machines/{machineId:guid}/inventory/{slotNumber:int}")]
    public IActionResult UpdateStock(Guid machineId, int slotNumber, [FromQuery] int delta)
    {
        bool updated = _vendingService.UpdateStock(machineId, slotNumber, delta);
        return updated ? Ok(new { Message = "Stock updated." }) : BadRequest(new { Message = "Invalid slot or stock delta." });
    }
}
