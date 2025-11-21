namespace BettsTax.Core.DTOs.Auth;

public record RegisterDto(string FirstName, string LastName, string Email, string Password);

public record LoginDto(string Email, string Password);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);
