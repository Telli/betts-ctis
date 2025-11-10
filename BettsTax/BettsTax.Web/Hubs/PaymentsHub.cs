using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data;
using BettsTax.Data.Models;
using System.Security.Claims;

namespace BettsTax.Web.Hubs;

[Authorize]
public class PaymentsHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentsHub> _logger;

    public PaymentsHub(
        ApplicationDbContext context,
        ILogger<PaymentsHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            _logger.LogWarning("User connected to PaymentsHub without valid identifier");
            return;
        }

        _logger.LogInformation("User {UserId} connected to PaymentsHub", userId);

        // Join user to their personal payment updates group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _logger.LogInformation("User {UserId} disconnected from PaymentsHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to payment status updates for a specific payment
    /// </summary>
    public async Task SubscribeToPayment(int paymentId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            // Verify user owns this payment or is admin/associate
            var payment = await _context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                await Clients.Caller.SendAsync("Error", "Payment not found");
                return;
            }

            var userRoles = Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
            var isAuthorized = payment.Client?.UserId == userId || 
                             userRoles.Contains("Admin") || 
                             userRoles.Contains("Associate");

            if (!isAuthorized)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to view this payment");
                return;
            }

            // Add to payment-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"payment_{paymentId}");

            _logger.LogInformation("User {UserId} subscribed to payment {PaymentId}", userId, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to payment {PaymentId} for user {UserId}", paymentId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to subscribe to payment updates");
        }
    }

    /// <summary>
    /// Unsubscribe from payment status updates
    /// </summary>
    public async Task UnsubscribeFromPayment(int paymentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment_{paymentId}");
        _logger.LogInformation("User unsubscribed from payment {PaymentId}", paymentId);
    }

    /// <summary>
    /// Request current payment status
    /// </summary>
    public async Task GetPaymentStatus(int paymentId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var payment = await _context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                await Clients.Caller.SendAsync("Error", "Payment not found");
                return;
            }

            // Verify authorization
            var userRoles = Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
            var isAuthorized = payment.Client?.UserId == userId || 
                             userRoles.Contains("Admin") || 
                             userRoles.Contains("Associate");

            if (!isAuthorized)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to view this payment");
                return;
            }

            var paymentStatus = new
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                UpdatedAt = payment.UpdatedAt ?? payment.CreatedAt
            };

            await Clients.Caller.SendAsync("PaymentStatusUpdate", paymentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for payment {PaymentId}", paymentId);
            await Clients.Caller.SendAsync("Error", "Failed to get payment status");
        }
    }

    /// <summary>
    /// Helper method to broadcast payment status update (called from backend services)
    /// </summary>
    public static async Task BroadcastPaymentStatusUpdate(
        IHubContext<PaymentsHub> hubContext,
        ApplicationDbContext context,
        int paymentId,
        string status,
        ILogger? logger = null)
    {
        try
        {
            var payment = await context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                logger?.LogWarning("Attempted to broadcast payment status for non-existent payment {PaymentId}", paymentId);
                return;
            }

            var paymentStatus = new
            {
                PaymentId = payment.Id,
                Status = status,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = payment.TransactionId,
                UpdatedAt = DateTime.UtcNow
            };

            // Send to payment-specific group
            await hubContext.Clients.Group($"payment_{paymentId}")
                .SendAsync("PaymentStatusUpdate", paymentStatus);

            // Also send to user's personal group
            if (payment.Client?.UserId != null)
            {
                await hubContext.Clients.Group($"user_{payment.Client.UserId}")
                    .SendAsync("PaymentStatusUpdate", paymentStatus);
            }

            logger?.LogInformation("Broadcasted payment status update for payment {PaymentId}: {Status}", paymentId, status);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error broadcasting payment status update for payment {PaymentId}", paymentId);
        }
    }

    /// <summary>
    /// Helper method to send payment confirmation (called from backend services)
    /// </summary>
    public static async Task SendPaymentConfirmation(
        IHubContext<PaymentsHub> hubContext,
        ApplicationDbContext context,
        int paymentId,
        string receiptNumber,
        ILogger? logger = null)
    {
        try
        {
            var payment = await context.Payments
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                logger?.LogWarning("Attempted to send payment confirmation for non-existent payment {PaymentId}", paymentId);
                return;
            }

            var confirmationData = new
            {
                PaymentId = payment.Id,
                Status = payment.Status,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ReceiptNumber = receiptNumber,
                TransactionId = payment.TransactionId,
                ConfirmedAt = DateTime.UtcNow
            };

            // Send to payment-specific group
            await hubContext.Clients.Group($"payment_{paymentId}")
                .SendAsync("PaymentConfirmed", confirmationData);

            // Also send to user's personal group
            if (payment.Client?.UserId != null)
            {
                await hubContext.Clients.Group($"user_{payment.Client.UserId}")
                    .SendAsync("PaymentConfirmed", confirmationData);
            }

            logger?.LogInformation("Sent payment confirmation for payment {PaymentId}, receipt {ReceiptNumber}", 
                paymentId, receiptNumber);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error sending payment confirmation for payment {PaymentId}", paymentId);
        }
    }
}
