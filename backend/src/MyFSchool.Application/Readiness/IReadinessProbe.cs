namespace MyFSchool.Application.Readiness;

public interface IReadinessProbe
{
    Task<ReadinessReport> CheckAsync(CancellationToken cancellationToken);
}

public sealed record ReadinessReport(IReadOnlyList<ReadinessComponent> Components)
{
    public bool IsReady => Components.All(component => component.IsReady);
}

public sealed record ReadinessComponent(string Name, bool IsReady);
