namespace DashboardApi.Models;

public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? LastKnownStatus { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public int? LastResponseTimeMs { get; set; }
    public double? UptimePercent24h { get; set; }
}