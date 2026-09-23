using Domain.Provisioning;

namespace Service;

public class DeploymentOrchestrationService(IUserDeploymentProvisioner provisioner)
{
    public Task RequestProvisioning(HostingConfig config, CancellationToken cancellationToken = default)
    {
        // TODO: once the data model exists, persist a deployment record via Repository here
        // (currently GetStatus reads live Kubernetes state instead of any stored record).
        return provisioner.ProvisionAsync(config, cancellationToken);
    }

    public Task<DeploymentStatus> GetStatus(Guid userId, CancellationToken cancellationToken = default) =>
        provisioner.GetStatusAsync(userId, cancellationToken);

    public Task Deprovision(Guid userId, CancellationToken cancellationToken = default) =>
        provisioner.DeprovisionAsync(userId, cancellationToken);
}
