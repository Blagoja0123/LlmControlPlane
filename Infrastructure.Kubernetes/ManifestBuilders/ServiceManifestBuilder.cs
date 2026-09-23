using Domain.Provisioning;
using k8s.Models;

namespace Infrastructure.Kubernetes.ManifestBuilders;

public static class ServiceManifestBuilder
{
    private const string AppLabelKey = "app";
    private const string AppLabelValue = "llm-backend";

    public static V1Service Build(Guid userId, ContainerSpec spec) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = NamingConvention.ServiceName,
            NamespaceProperty = NamingConvention.NamespaceName(userId)
        },
        Spec = new V1ServiceSpec
        {
            Type = "ClusterIP",
            Selector = new Dictionary<string, string> { [AppLabelKey] = AppLabelValue },
            Ports =
            [
                new V1ServicePort
                {
                    Protocol = "TCP",
                    Port = 80,
                    TargetPort = spec.ContainerPort
                }
            ]
        }
    };
}
