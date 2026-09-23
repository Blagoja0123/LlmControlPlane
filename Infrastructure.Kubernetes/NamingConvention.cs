namespace Infrastructure.Kubernetes;

public static class NamingConvention
{
    private const string NamespacePrefix = "user-";

    public const string DeploymentName = "llm-backend";
    public const string ServiceName = "llm-backend-svc";
    public const string IngressName = "llm-backend-ingress";
    public const string ModelCacheClaimName = "llm-backend-cache";

    public static string NamespaceName(Guid userId) => $"{NamespacePrefix}{userId:D}".ToLowerInvariant();

    public static bool IsUserNamespace(string namespaceName) =>
        namespaceName.StartsWith(NamespacePrefix, StringComparison.Ordinal) &&
        Guid.TryParse(namespaceName[NamespacePrefix.Length..], out _);

    public static string IngressHost(Guid userId, string baseDomain) => $"{userId:D}-api.{baseDomain}".ToLowerInvariant();
}
