namespace Domain.Provisioning;

public record SecretEnvVar(string EnvName, string SecretName, string SecretKey);

public record ContainerSpec(
    string Image,
    IReadOnlyList<string> Command,
    IReadOnlyList<string> Args,
    int ContainerPort,
    ResourceRequirements Resources,
    bool RequiresGpu,
    string ModelCacheMountPath,
    string HealthCheckPath,
    IReadOnlyDictionary<string, string>? EnvVars = null,
    IReadOnlyList<SecretEnvVar>? SecretEnvVars = null,
    int StartupProbeFailureThreshold = 60,
    int StartupProbePeriodSeconds = 10)
{
    public IReadOnlyDictionary<string, string> EnvVars { get; init; } = EnvVars ?? new Dictionary<string, string>();
    public IReadOnlyList<SecretEnvVar> SecretEnvVars { get; init; } = SecretEnvVars ?? [];
}
