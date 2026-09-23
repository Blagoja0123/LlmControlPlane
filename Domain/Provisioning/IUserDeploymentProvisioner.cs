namespace Domain.Provisioning;

public interface IUserDeploymentProvisioner
{
    Task ProvisionAsync(HostingConfig config, CancellationToken cancellationToken = default);

    Task<DeploymentStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeprovisionAsync(Guid userId, CancellationToken cancellationToken = default);
}
