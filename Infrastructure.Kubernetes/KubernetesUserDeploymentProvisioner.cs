using System.Net;
using Domain.Provisioning;
using Infrastructure.Kubernetes.Backends;
using Infrastructure.Kubernetes.ManifestBuilders;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kubernetes;

public class KubernetesUserDeploymentProvisioner(
    IKubernetes client,
    BackendSpecFactoryResolver backendResolver,
    OllamaModelPuller ollamaModelPuller,
    IOptions<ProvisioningOptions> options) : IUserDeploymentProvisioner
{
    private readonly ProvisioningOptions _options = options.Value;

    public async Task ProvisionAsync(HostingConfig config, CancellationToken cancellationToken = default)
    {
        var namespaceName = NamingConvention.NamespaceName(config.UserId);
        EnsureIsUserNamespace(namespaceName);

        var spec = backendResolver.Resolve(config.BackendType).BuildContainerSpec(config);

        await IgnoreConflict(() => client.CoreV1.CreateNamespaceAsync(
            NamespaceManifestBuilder.Build(config.UserId), cancellationToken: cancellationToken));

        await IgnoreConflict(() => client.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(
            PersistentVolumeClaimManifestBuilder.Build(config.UserId, _options), namespaceName, cancellationToken: cancellationToken));

        await CreateOrReplaceDeployment(config, spec, namespaceName, cancellationToken);

        await IgnoreConflict(() => client.CoreV1.CreateNamespacedServiceAsync(
            ServiceManifestBuilder.Build(config.UserId, spec), namespaceName, cancellationToken: cancellationToken));

        await IgnoreConflict(() => client.NetworkingV1.CreateNamespacedIngressAsync(
            IngressManifestBuilder.Build(config.UserId, _options), namespaceName, cancellationToken: cancellationToken));

        if (config.BackendType == BackendType.Ollama)
        {
            // Best-effort: kicks off the pull, doesn't block provisioning on it finishing.
            // GetStatusAsync only reflects the pod's basic health, not pull completion -
            // see the note there. Full pull-progress tracking needs persistence we don't have yet.
            await ollamaModelPuller.TryPullAsync(namespaceName, config.ModelIdentifier, cancellationToken);
        }
    }

    public async Task<DeploymentStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var namespaceName = NamingConvention.NamespaceName(userId);

        V1Namespace ns;
        try
        {
            ns = await client.CoreV1.ReadNamespaceAsync(namespaceName, cancellationToken: cancellationToken);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeploymentStatus(DeploymentPhase.NotFound);
        }

        if (ns.Metadata.DeletionTimestamp is not null)
        {
            return new DeploymentStatus(DeploymentPhase.Deprovisioning);
        }

        V1Deployment deployment;
        try
        {
            deployment = await client.AppsV1.ReadNamespacedDeploymentAsync(
                NamingConvention.DeploymentName, namespaceName, cancellationToken: cancellationToken);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeploymentStatus(DeploymentPhase.Provisioning, Message: "Namespace created, workload not scheduled yet");
        }

        var desiredReplicas = deployment.Spec.Replicas ?? 1;
        var readyReplicas = deployment.Status?.ReadyReplicas ?? 0;

        if (readyReplicas < desiredReplicas)
        {
            return new DeploymentStatus(DeploymentPhase.Provisioning, Message: $"{readyReplicas}/{desiredReplicas} replicas ready");
        }

        var endpointUrl = $"https://{NamingConvention.IngressHost(userId, _options.BaseDomain)}";
        return new DeploymentStatus(DeploymentPhase.Ready, EndpointUrl: endpointUrl);
    }

    public async Task DeprovisionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var namespaceName = NamingConvention.NamespaceName(userId);
        EnsureIsUserNamespace(namespaceName);

        await IgnoreNotFound(() => client.CoreV1.DeleteNamespaceAsync(namespaceName, cancellationToken: cancellationToken));
    }

    private async Task CreateOrReplaceDeployment(
        HostingConfig config, ContainerSpec spec, string namespaceName, CancellationToken cancellationToken)
    {
        var deployment = DeploymentManifestBuilder.Build(config, spec);

        try
        {
            await client.AppsV1.CreateNamespacedDeploymentAsync(deployment, namespaceName, cancellationToken: cancellationToken);
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
        {
            // Re-provisioning (e.g. model/replica-count change): patch the existing Deployment,
            // which triggers a normal rolling update - this is the only object kind we update in place.
            await client.AppsV1.ReplaceNamespacedDeploymentAsync(
                deployment, NamingConvention.DeploymentName, namespaceName, cancellationToken: cancellationToken);
        }
    }

    private static void EnsureIsUserNamespace(string namespaceName)
    {
        if (!NamingConvention.IsUserNamespace(namespaceName))
        {
            throw new InvalidOperationException(
                $"Refusing to operate on namespace '{namespaceName}': does not match the 'user-*' naming convention.");
        }
    }

    private static async Task IgnoreConflict(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
        {
            // already exists - creation is idempotent, we don't update these object kinds on re-provision
        }
    }

    private static async Task IgnoreNotFound(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
        {
            // already gone - deprovisioning is idempotent
        }
    }
}
