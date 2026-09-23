namespace Domain.Provisioning;

public interface IBackendSpecFactory
{
    BackendType SupportedBackend { get; }

    ContainerSpec BuildContainerSpec(HostingConfig config);
}
