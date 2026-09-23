using k8s.Models;

namespace Infrastructure.Kubernetes.ManifestBuilders;

public static class IngressManifestBuilder
{
    // spec.tls is intentionally omitted: ingress-nginx is configured with a wildcard
    // certificate as its default SSL certificate (see deploy/cluster-prereqs), so every
    // per-user host gets HTTPS without needing its own TLS secret in this namespace.
    public static V1Ingress Build(Guid userId, ProvisioningOptions options) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = NamingConvention.IngressName,
            NamespaceProperty = NamingConvention.NamespaceName(userId)
        },
        Spec = new V1IngressSpec
        {
            IngressClassName = options.IngressClassName,
            Rules =
            [
                new V1IngressRule
                {
                    Host = NamingConvention.IngressHost(userId, options.BaseDomain),
                    Http = new V1HTTPIngressRuleValue
                    {
                        Paths =
                        [
                            new V1HTTPIngressPath
                            {
                                Path = "/",
                                PathType = "Prefix",
                                Backend = new V1IngressBackend
                                {
                                    Service = new V1IngressServiceBackend
                                    {
                                        Name = NamingConvention.ServiceName,
                                        Port = new V1ServiceBackendPort { Number = 80 }
                                    }
                                }
                            }
                        ]
                    }
                }
            ]
        }
    };
}
