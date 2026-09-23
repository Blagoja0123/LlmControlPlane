namespace Domain.Provisioning;

public record ResourceRequirements(
    string CpuRequest,
    string CpuLimit,
    string MemoryRequest,
    string MemoryLimit,
    int GpuCount = 0)
{
    public static ResourceRequirements Default { get; } = new(
        CpuRequest: "500m",
        CpuLimit: "2",
        MemoryRequest: "1Gi",
        MemoryLimit: "4Gi");
}
