using Domain.Provisioning;
using Microsoft.AspNetCore.Mvc;
using Service;

namespace Web.Controllers.Api;

// NOTE: no auth yet - userId is trusted as given. Acceptable for dev-cluster infra
// build-out, must be closed before any public exposure (see plan risks).
[ApiController]
[Route("api/deployments")]
public class DeploymentsController(DeploymentOrchestrationService orchestration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeploymentRequest request, CancellationToken cancellationToken)
    {
        var config = new HostingConfig(
            UserId: request.UserId,
            BackendType: request.BackendType,
            ModelIdentifier: request.ModelIdentifier,
            ReplicaCount: request.ReplicaCount ?? 1,
            ResourceRequests: request.ResourceRequests);

        await orchestration.RequestProvisioning(config, cancellationToken);

        return AcceptedAtAction(nameof(GetStatus), new { userId = request.UserId }, null);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetStatus(Guid userId, CancellationToken cancellationToken)
    {
        var status = await orchestration.GetStatus(userId, cancellationToken);

        return status.Phase == DeploymentPhase.NotFound ? NotFound() : Ok(status);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        await orchestration.Deprovision(userId, cancellationToken);

        return Accepted();
    }
}

public record CreateDeploymentRequest(
    Guid UserId,
    BackendType BackendType,
    string ModelIdentifier,
    int? ReplicaCount = null,
    ResourceRequirements? ResourceRequests = null);
