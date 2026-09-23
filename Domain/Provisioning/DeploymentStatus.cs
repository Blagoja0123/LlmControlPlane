namespace Domain.Provisioning;

public enum DeploymentPhase
{
    NotFound,
    Provisioning,
    PullingModel,
    Ready,
    Failed,
    Deprovisioning
}

public record DeploymentStatus(DeploymentPhase Phase, string? EndpointUrl = null, string? Message = null);
