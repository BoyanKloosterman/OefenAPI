namespace OefenApi.Models;

public sealed record ErrorResponse(string Error);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record LoginResponse(string Token, string TokenType, int ExpiresIn);

public sealed record Account(int Id, string Username, string Role, string DisplayName, string Email);

public sealed record ClientResponse(int Id, int OwnerId, string Name, string Email, string Note);

public sealed record ClientListResponse(IReadOnlyList<ClientResponse> Items);

public sealed record UserResponse(int Id, string Username, string Role, string DisplayName, string Email);

public sealed record UserProfileUpdate(string? DisplayName, string? Email);

public sealed record UserUpdate(string? DisplayName, string? Email, string? Role);

public sealed record BulkItemResponse(int Id, string Title, string Description);

public sealed record BulkResponse(int Page, int PageSize, int Total, IReadOnlyList<BulkItemResponse> Items);
