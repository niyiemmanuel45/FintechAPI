namespace Core.Enums;

public enum PaymentStateEnum
{
    Pending,
    Authorized,
    Processing,
    Settled,
    Completed,
    Failed,
    Declined,
    RequiresAction,
    Cancelled,
    RefundInitiated,
    RefundProcessing,
    Refunded
}
