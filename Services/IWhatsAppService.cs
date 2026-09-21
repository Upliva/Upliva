namespace UplivaResortBooking.Services;

public interface IWhatsAppService
{
    Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default);
    Task<bool> SendImageAsync(string phoneNumber, string imageUrl, string caption, CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeMenuAsync(string phoneNumber, string? customerName = null, CancellationToken cancellationToken = default);
    Task<bool> SendBookingMenuAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<bool> SendRoomListAsync(string phoneNumber, DateTime checkIn, DateTime checkOut, int guests, CancellationToken cancellationToken = default);
    Task<bool> SendBookingSummaryAsync(string phoneNumber, string bookingReference, string roomName, DateTime checkIn, DateTime checkOut, int guests, decimal totalAmount, CancellationToken cancellationToken = default);
}
