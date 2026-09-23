using Domain.Provisioning;
using k8s.Models;

namespace Infrastructure.Kubernetes.ManifestBuilders;

public static class DeploymentManifestBuilder
{
    private const string AppLabelKey = "app";
    private const string AppLabelValue = "llm-backend";
    private const string CacheVolumeName = "model-cache";
    private const string GpuResourceKey = "nvidia.com/gpu";
    private const string GpuNodeSelectorKey = "gpu-pool";

    public static V1Deployment Build(HostingConfig config, ContainerSpec spec)
    {
        var labels = new Dictionary<string, string> { [AppLabelKey] = AppLabelValue };

        var env = spec.EnvVars
            .Select(kv => new V1EnvVar { Name = kv.Key, Value = kv.Value })
            .Concat(spec.SecretEnvVars.Select(s => new V1EnvVar
            {
                Name = s.EnvName,
                ValueFrom = new V1EnvVarSource
                {
                    SecretKeyRef = new V1SecretKeySelector { Name = s.SecretName, Key = s.SecretKey }
                }
            }))
            .ToList();

        var resourceRequests = new Dictionary<string, ResourceQuantity>
        {
            ["cpu"] = new ResourceQuantity(spec.Resources.CpuRequest),
            ["memory"] = new ResourceQuantity(spec.Resources.MemoryRequest)
        };
        var resourceLimits = new Dictionary<string, ResourceQuantity>
        {
            ["cpu"] = new ResourceQuantity(spec.Resources.CpuLimit),
            ["memory"] = new ResourceQuantity(spec.Resources.MemoryLimit)
        };

        if (spec.RequiresGpu && spec.Resources.GpuCount > 0)
        {
            var gpuQuantity = new ResourceQuantity(spec.Resources.GpuCount.ToString());
            resourceRequests[GpuResourceKey] = gpuQuantity;
            resourceLimits[GpuResourceKey] = gpuQuantity;
        }

        var container = new V1Container
        {
            Name = AppLabelValue,
            Image = spec.Image,
            Command = spec.Command.Count > 0 ? spec.Command.ToList() : null,
            Args = spec.Args.Count > 0 ? spec.Args.ToList() : null,
            Ports = [new V1ContainerPort { ContainerPort = spec.ContainerPort }],
            Env = env,
            Resources = new V1ResourceRequirements { Requests = resourceRequests, Limits = resourceLimits },
            VolumeMounts = [new V1VolumeMount { Name = CacheVolumeName, MountPath = spec.ModelCacheMountPath }],
            // Model loading can take minutes; the startup probe gates readiness/liveness checks
            // until the backend actually answers its health endpoint, instead of getting killed mid-load.
            StartupProbe = new V1Probe
            {
                HttpGet = new V1HTTPGetAction { Path = spec.HealthCheckPath, Port = spec.ContainerPort },
                PeriodSeconds = spec.StartupProbePeriodSeconds,
                FailureThreshold = spec.StartupProbeFailureThreshold
            },
            ReadinessProbe = new V1Probe
            {
                HttpGet = new V1HTTPGetAction { Path = spec.HealthCheckPath, Port = spec.ContainerPort },
                PeriodSeconds = 10,
                FailureThreshold = 3
            }
        };

        var podSpec = new V1PodSpec
        {
            Containers = [container],
            Volumes =
            [
                new V1Volume
                {
                    Name = CacheVolumeName,
                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                    {
                        ClaimName = NamingConvention.ModelCacheClaimName
                    }
                }
            ]
        };

        if (spec.RequiresGpu)
        {
            // Convention only for now: nodes intended for LLM backends are labeled gpu-pool=true
            // and tainted nvidia.com/gpu=NoSchedule. Not yet enforced/created by this control plane.
            podSpec.NodeSelector = new Dictionary<string, string> { [GpuNodeSelectorKey] = "true" };
            podSpec.Tolerations =
            [
                new V1Toleration { Key = GpuResourceKey, OperatorProperty = "Exists", Effect = "NoSchedule" }
            ];
        }

        return new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = NamingConvention.DeploymentName,
                NamespaceProperty = NamingConvention.NamespaceName(config.UserId),
                Labels = labels
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = config.ReplicaCount,
                Selector = new V1LabelSelector { MatchLabels = labels },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = podSpec
                }
            }
        };
    }
}
