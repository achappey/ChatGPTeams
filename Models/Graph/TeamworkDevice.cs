
namespace achappey.ChatGPTeams.Models.Graph;

public class TeamworkDevice
{
    public string Id { get; set; }
    public string DeviceType { get; set; }
    public HardwareDetail HardwareDetail { get; set; }
    public string Notes { get; set; }
    public string HealthStatus { get; set; }
    public string ActivityState { get; set; }
    public Identity CurrentUser { get; set; }
}

public class HardwareDetail
{
    public string SerialNumber { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
}
