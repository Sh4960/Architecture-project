namespace NotificationService.Contracts
{
    public interface IOrderFinalized
    {
        int OrderId { get; }
        int UserId { get; }
        string Status { get; }
    }
}
