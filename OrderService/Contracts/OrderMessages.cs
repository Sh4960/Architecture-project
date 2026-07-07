using System.Collections.Generic;

namespace OrderService.Contracts
{
    public interface IOrderItemDto
    {
        int ProductId { get; }
        int Quantity { get; }
    }

    public interface IOrderPlaced
    {
        int OrderId { get; }
        int UserId { get; }
        decimal TotalPrice { get; }
        IReadOnlyList<IOrderItemDto> Items { get; }
    }

    public interface IInventoryReserved
    {
        int OrderId { get; }
    }

    public interface IInventoryRejected
    {
        int OrderId { get; }
        string Reason { get; }
    }

    public interface IOrderFinalized
    {
        int OrderId { get; }
        int UserId { get; }
        string Status { get; }
    }
}
