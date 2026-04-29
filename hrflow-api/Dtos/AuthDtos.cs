namespace HrFlow.Api.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, UserDto User);

public sealed record UserDto(Guid Id, string Email, string DisplayName, IReadOnlyCollection<string> Roles);
