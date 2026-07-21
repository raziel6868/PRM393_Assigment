namespace MyFSchool.Api.Contracts;

public sealed record HealthResponse(string Status, string TraceId);

public sealed record ReadinessResponse(
    string Status,
    IReadOnlyList<ReadinessComponentResponse> Components,
    string TraceId);

public sealed record ReadinessComponentResponse(string Name, string Status);
