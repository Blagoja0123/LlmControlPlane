using k8s;

namespace Infrastructure.Kubernetes;

public static class KubernetesClientFactory
{
    /// <summary>In-cluster ServiceAccount config when running as a Pod, falling back to the local kubeconfig for `dotnet run` against a dev cluster.</summary>
    public static IKubernetes Create()
    {
        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile();

        return new k8s.Kubernetes(config);
    }
}
