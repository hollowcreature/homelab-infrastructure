namespace DashboardApi.Models;

public class StatusCheck
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public DateTime CheckedAt { get; set; }
    public bool IsUp { get; set; }
    public int ResponseTimeMs { get; set; }
}