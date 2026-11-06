namespace OmniOrder.Application.DTOs;

public record DemoTokenRequest(
    string Username,
    string Role
);

public record TokenResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt
);
