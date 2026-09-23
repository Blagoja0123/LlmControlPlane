using Domain.Provisioning;

namespace Infrastructure.Kubernetes.Backends;

/// <summary>
/// Hugging Face Text Generation Inference. GPU-only in practice - confirm current image
/// tag/CPU-variant availability against upstream docs before relying on either.
/// TODO: pin the image tag to a verified version before production use, instead of :latest.
/// </summary>
public class TgiBackendSpecFactory : IBackendSpecFactory
{
    private const int Port = 80;
    private const string ModelCacheMountPath = "/data";

    public BackendType SupportedBackend => BackendType.Tgi;

    public ContainerSpec BuildContainerSpec(HostingConfig config) => new(
        Image: "ghcr.io/huggingface/text-generation-inference:latest",
        Command: [],
        Args: [],
        ContainerPort: Port,
        Resources: config.ResourceRequests with { GpuCount = Math.Max(config.ResourceRequests.GpuCount, 1) },
        RequiresGpu: true,
        ModelCacheMountPath: ModelCacheMountPath,
        HealthCheckPath: "/health",
        EnvVars: new Dictionary<string, string> { ["MODEL_ID"] = config.ModelIdentifier },
        StartupProbeFailureThreshold: 90,
        StartupProbePeriodSeconds: 10);
}
