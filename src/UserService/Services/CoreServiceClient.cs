namespace UserService.Services;

public interface ICoreServiceClient
{
    Task SyncUserStatusAsync(UpdateAccountStateRequest request, CancellationToken cancellationToken);
}

public sealed class CoreServiceClient(HttpClient client, ILogger<CoreServiceClient> logger) : ICoreServiceClient
{
    public async Task SyncUserStatusAsync(UpdateAccountStateRequest request, CancellationToken cancellationToken)
    {
        using var response = await client.PatchAsJsonAsync("internal/users/status", request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("CoreService status sync failed: {StatusCode} {Error}", response.StatusCode, error);
        throw new HttpRequestException($"CoreService error during status sync: {(int)response.StatusCode}");
    }
}
