namespace DashboardApi.Models;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<StatusCheck> StatusChecks { get; set; } = new();
    public bool RequiresAuth { get; set; } = false;
    public string? HealthCheckUrl { get; set; }
}