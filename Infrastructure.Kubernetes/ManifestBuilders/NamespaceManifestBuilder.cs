using k8s.Models;

namespace Infrastructure.Kubernetes.ManifestBuilders;

public static class NamespaceManifestBuilder
{
    public static V1Namespace Build(Guid userId) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = NamingConvention.NamespaceName(userId),
            Labels = new Dictionary<string, string>
            {
                ["app.kubernetes.io/managed-by"] = "llm-control-plane",
                ["llm-control-plane/user-id"] = userId.ToString("D")
            }
        }
    };
}
