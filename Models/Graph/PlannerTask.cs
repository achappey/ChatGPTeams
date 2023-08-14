using System;

namespace achappey.ChatGPTeams.Models.Graph;

public class PlannerTask
{
    public string Title { get; set; }
    public string BucketId { get; set; }
    public string Id { get; set; }
    public string PlanId { get; set; }
    public PlannerTaskDetails Details { get; set; }
    public DateTimeOffset? CompletedDateTime { get; set; }
    public DateTimeOffset? DueDateTime { get; set; }
}

public class PlannerTaskDetails
{
    public string Description { get; set; }
}