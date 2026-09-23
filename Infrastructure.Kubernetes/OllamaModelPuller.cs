using System.Net.Http.Json;

namespace Infrastructure.Kubernetes;

/// <summary>
/// Ollama needs a separate POST /api/pull after the server is up, since the model isn't a
/// boot-time argument. Kept inside the .NET control plane (not a Kubernetes-side hook) per
/// the "everything stays inside the app" decision.
/// </summary>
public class OllamaModelPuller(IHttpClientFactory httpClientFactory)
{
    public async Task<bool> TryPullAsync(string namespaceName, string model, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(OllamaModelPuller));

        // Cluster-internal Service DNS - only resolvable when this control plane is itself
        // running in-cluster. Doesn't work from a local `dotnet run` against kubeconfig; the
        // call below just fails silently in that case, which is fine since it's best-effort.
        var baseUrl = $"http://{NamingConvention.ServiceName}.{namespaceName}.svc.cluster.local";

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"{baseUrl}/api/pull", new { name = model, stream = false }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
