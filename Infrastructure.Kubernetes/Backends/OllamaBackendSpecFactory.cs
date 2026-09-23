using Domain.Provisioning;

namespace Infrastructure.Kubernetes.Backends;

/// <summary>
/// Ollama. CPU-friendly, but the model isn't a boot-time argument - the server starts via
/// its default entrypoint and the model must be pulled afterwards via POST /api/pull.
/// That pull is driven by KubernetesUserDeploymentProvisioner once the pod is Running
/// (see DeploymentStatus.PullingModel), not by anything in this ContainerSpec.
/// </summary>
public class OllamaBackendSpecFactory : IBackendSpecFactory
{
    private const int Port = 11434;
    private const string ModelCacheMountPath = "/root/.ollama";

    public BackendType SupportedBackend => BackendType.Ollama;

    public ContainerSpec BuildContainerSpec(HostingConfig config) => new(
        Image: "ollama/ollama:latest",
        Command: [],
        Args: [],
        ContainerPort: Port,
        Resources: config.ResourceRequests,
        RequiresGpu: false,
        ModelCacheMountPath: ModelCacheMountPath,
        // Only proves the server process is up, not that a model has been pulled -
        // model-pull completion is tracked separately by the provisioner.
        HealthCheckPath: "/",
        EnvVars: new Dictionary<string, string> { ["OLLAMA_MODELS"] = ModelCacheMountPath },
        StartupProbeFailureThreshold: 30,
        StartupProbePeriodSeconds: 5);
}
