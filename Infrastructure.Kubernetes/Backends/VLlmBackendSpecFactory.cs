using Domain.Provisioning;

namespace Infrastructure.Kubernetes.Backends;

/// <summary>
/// OpenAI-compatible serving via vLLM. GPU-only in practice - vLLM's CPU backend isn't
/// reliable enough across releases to depend on (see plan risks); treat as GPU-required.
/// TODO: pin the image tag to a verified version before production use, instead of :latest.
/// </summary>
public class VLlmBackendSpecFactory : IBackendSpecFactory
{
    private const int Port = 8000;
    private const string ModelCacheMountPath = "/root/.cache/huggingface";

    public BackendType SupportedBackend => BackendType.VLlm;

    public ContainerSpec BuildContainerSpec(HostingConfig config) => new(
        Image: "vllm/vllm-openai:latest",
        Command: [],
        Args:
        [
            "--model", config.ModelIdentifier,
            "--served-model-name", config.ModelIdentifier,
            "--port", Port.ToString()
        ],
        ContainerPort: Port,
        Resources: config.ResourceRequests with { GpuCount = Math.Max(config.ResourceRequests.GpuCount, 1) },
        RequiresGpu: true,
        ModelCacheMountPath: ModelCacheMountPath,
        HealthCheckPath: "/health",
        EnvVars: new Dictionary<string, string> { ["HF_HOME"] = ModelCacheMountPath },
        // Model downloads/loads can take several minutes for larger models.
        StartupProbeFailureThreshold: 90,
        StartupProbePeriodSeconds: 10);
}
