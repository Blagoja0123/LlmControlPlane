using k8s.Models;

namespace Infrastructure.Kubernetes.ManifestBuilders;

public static class PersistentVolumeClaimManifestBuilder
{
    public static V1PersistentVolumeClaim Build(Guid userId, ProvisioningOptions options) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = NamingConvention.ModelCacheClaimName,
            NamespaceProperty = NamingConvention.NamespaceName(userId)
        },
        Spec = new V1PersistentVolumeClaimSpec
        {
            AccessModes = ["ReadWriteOnce"],
            StorageClassName = options.StorageClassName,
            Resources = new V1VolumeResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    ["storage"] = new ResourceQuantity(options.ModelCacheSize)
                }
            }
        }
    };
}
