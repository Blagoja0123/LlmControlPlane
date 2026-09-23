using Domain.Provisioning;

namespace Infrastructure.Kubernetes.Backends;

public class BackendSpecFactoryResolver(IEnumerable<IBackendSpecFactory> factories)
{
    private readonly Dictionary<BackendType, IBackendSpecFactory> _factoriesByType =
        factories.ToDictionary(f => f.SupportedBackend);

    public IBackendSpecFactory Resolve(BackendType backendType)
    {
        if (!_factoriesByType.TryGetValue(backendType, out var factory))
        {
            throw new NotSupportedException($"No {nameof(IBackendSpecFactory)} registered for backend '{backendType}'.");
        }

        return factory;
    }
}
