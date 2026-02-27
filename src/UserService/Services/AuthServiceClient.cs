namespace UserService.Services;

public interface IAuthServiceClient
{
    Task RegisterCredentialsAsync(RegisterCredentialsRequest request, CancellationToken cancellationToken);
}

public sealed class AuthServiceClient(HttpClient client, ILogger<AuthServiceClient> logger) : IAuthServiceClient
{
    public async Task RegisterCredentialsAsync(RegisterCredentialsRequest request, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("internal/users", request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError("AuthService rejected credential registration: {StatusCode} {Error}", response.StatusCode, error);
        throw new HttpRequestException($"AuthService error during credentials provisioning: {(int)response.StatusCode}");
    }
}
