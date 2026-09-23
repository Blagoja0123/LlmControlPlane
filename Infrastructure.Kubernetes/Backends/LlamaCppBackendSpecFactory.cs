using Domain.Provisioning;

namespace Infrastructure.Kubernetes.Backends;

/// <summary>
/// llama.cpp's built-in server. CPU-friendly by default with quantized GGUF models - the
/// one backend that can realistically run in a GPU-less dev/CI cluster.
/// ModelIdentifier is expected in "repo:quant" form (e.g. "Qwen/Qwen2.5-0.5B-Instruct-GGUF:Q4_K_M" -
/// note the HF *model repo* namespace, e.g. "Qwen", is unrelated to the "ggml-org" GHCR org the
/// llama.cpp *image* is published under below - mixing the two up produces a repo that doesn't exist),
/// passed straight through to llama-server's `-hf` flag, which resolves and downloads the matching GGUF file.
/// </summary>
public class LlamaCppBackendSpecFactory : IBackendSpecFactory
{
    private const int Port = 8080;
    private const string ModelCacheMountPath = "/root/.cache/llama.cpp";

    public BackendType SupportedBackend => BackendType.LlamaCpp;

    public ContainerSpec BuildContainerSpec(HostingConfig config) => new(
        Image: "ghcr.io/ggml-org/llama.cpp:server",
        Command: [],
        Args:
        [
            "-hf", config.ModelIdentifier,
            "--port", Port.ToString(),
            "--host", "0.0.0.0"
        ],
        ContainerPort: Port,
        Resources: config.ResourceRequests,
        RequiresGpu: false,
        ModelCacheMountPath: ModelCacheMountPath,
        HealthCheckPath: "/health",
        StartupProbeFailureThreshold: 30,
        StartupProbePeriodSeconds: 10);
}
