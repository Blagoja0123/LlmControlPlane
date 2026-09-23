namespace Domain.Provisioning;

public record HostingConfig(
    Guid UserId,
    BackendType BackendType,
    string ModelIdentifier,
    int ReplicaCount = 1,
    ResourceRequirements? ResourceRequests = null)
{
    public ResourceRequirements ResourceRequests { get; init; } = ResourceRequests ?? ResourceRequirements.Default;
}
