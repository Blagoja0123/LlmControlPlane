namespace Infrastructure.Kubernetes;

public class ProvisioningOptions
{
    public const string SectionName = "Provisioning";

    /// <summary>Matches the wildcard certificate's SAN (see deploy/cluster-prereqs/wildcard-certificate.yaml).</summary>
    public string BaseDomain { get; set; } = "llmcontrol.local";

    public string IngressClassName { get; set; } = "nginx";

    /// <summary>Null uses the cluster's default StorageClass.</summary>
    public string? StorageClassName { get; set; }

    public string ModelCacheSize { get; set; } = "20Gi";
}
