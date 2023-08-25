using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using achappey.ChatGPTeams.Models.Graph;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    public partial class GraphFunctionsClient
    {

        [MethodDescription("Searches for your Planner tasks based on title or description.")]
        public async Task<IEnumerable<Models.Graph.PlannerTask>> SearchMyPlannerTasks(
            [ParameterDescription("The task title to filter on.")] string title = null,
            [ParameterDescription("The description to filter on.")] string description = null)
        {
            var graphClient = GetAuthenticatedClient();

            var tasks = await graphClient.Me.Planner.Tasks
                                .Request()
                                .GetAsync();

            var filteredTasks = tasks.Where(task =>
                (string.IsNullOrEmpty(title) || task.Title.ToLower().Contains(title.ToLower())) &&
                (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(task.Details.Description) || task.Details.Description.ToLower().Contains(description.ToLower()))
            );

            return filteredTasks.Select(t => _mapper.Map<Models.Graph.PlannerTask>(t));
        }

        [MethodDescription("Creates a new Planner task with the given details.")]
        public async Task<Models.Graph.PlannerTask> CreatePlannerTask(
            [ParameterDescription("The ID of the Planner to create the task in.")] string plannerId,
            [ParameterDescription("The ID of the bucket to create the task in.")] string bucketId,
            [ParameterDescription("The title of the task.")] string title,
            [ParameterDescription("The description of the task.")] string description = null,
            [ParameterDescription("The due date of the task.")] DateTime? dueDate = null)
        {
            var graphClient = GetAuthenticatedClient();

            var newTask = new Microsoft.Graph.PlannerTask
            {
                Title = title,
                BucketId = bucketId,
                PlanId = plannerId,
                Details = new Microsoft.Graph.PlannerTaskDetails { Description = description },
                DueDateTime = dueDate
            };

            var createdTask = await graphClient.Planner.Tasks
                                    .Request()
                                    .AddAsync(newTask);

            return _mapper.Map<Models.Graph.PlannerTask>(createdTask);
        }

        [MethodDescription("Retrieves all user Planners, optionally filtered by a search term.")]
        public async Task<IEnumerable<Models.Graph.PlannerPlan>> GetAllPlanners(
            [ParameterDescription("The search term to filter planners by title (optional).")] string searchTerm = null)
        {
            var graphClient = GetAuthenticatedClient();

            var planners = await graphClient.Me.Planner.Plans
                                    .Request()
                                    .GetAsync();

            var filteredPlanners = planners.Where(p =>
                string.IsNullOrEmpty(searchTerm) || p.Title.ToLower().Contains(searchTerm.ToLower()));

            return filteredPlanners.Select(_mapper.Map<Models.Graph.PlannerPlan>);
        }

        [MethodDescription("Retrieves the buckets associated with a specific Planner.")]
        public async Task<IEnumerable<Models.Graph.PlannerBucket>> GetPlannerBuckets(
            [ParameterDescription("The ID of the Planner to get buckets from.")] string plannerId)
        {
            var graphClient = GetAuthenticatedClient();

            var buckets = await graphClient.Planner.Plans[plannerId].Buckets
                                .Request()
                                .GetAsync();

            return buckets.Select(b => _mapper.Map<Models.Graph.PlannerBucket>(b));
        }

        [MethodDescription("Retrieves all tasks from a specified bucket within a Planner.")]
        public async Task<IEnumerable<Models.Graph.PlannerTask>> GetTasksFromBucket(
            [ParameterDescription("The ID of the Planner containing the bucket.")] string plannerId,
            [ParameterDescription("The ID of the bucket to retrieve tasks from.")] string bucketId)
        {
            var graphClient = GetAuthenticatedClient();

            var tasks = await graphClient.Planner.Buckets[bucketId].Tasks
                                .Request()
                                .GetAsync();

            return tasks.Select(t => _mapper.Map<Models.Graph.PlannerTask>(t));
        }

        [MethodDescription("Creates a new Planner plan within the specified group.")]
        public async Task<Models.Graph.PlannerPlan> CreatePlanner(
            [ParameterDescription("The ID of the group to create the Planner plan in.")] string groupId,
            [ParameterDescription("The title of the Planner plan.")] string title)
        {
            var graphClient = GetAuthenticatedClient();

            var newPlan = new Microsoft.Graph.PlannerPlan
            {
                Title = title,
                Container = new Microsoft.Graph.PlannerPlanContainer { Url = $"https://graph.microsoft.com/beta/groups/{groupId}" }
            };

            var createdPlan = await graphClient.Planner.Plans
                                    .Request()
                                    .AddAsync(newPlan);

            return _mapper.Map<Models.Graph.PlannerPlan>(createdPlan);
        }

        [MethodDescription("Creates a new bucket within the specified Planner plan.")]
        public async Task<Models.Graph.PlannerBucket> CreateBucket(
            [ParameterDescription("The ID of the Planner plan to create the bucket in.")] string planId,
            [ParameterDescription("The name of the bucket.")] string bucketName)
        {
            var graphClient = GetAuthenticatedClient();

            var newBucket = new Microsoft.Graph.PlannerBucket
            {
                Name = bucketName,
                PlanId = planId // PlanId property must be set to the plan ID
            };

            var createdBucket = await graphClient.Planner.Buckets
                                    .Request()
                                    .AddAsync(newBucket);

            return _mapper.Map<Models.Graph.PlannerBucket>(createdBucket);
        }

        [MethodDescription("Copies all details, buckets, and tasks (including checklists) from a source Planner to a target Planner.")]
        public async Task<Models.Response> CopyPlanner(
            [ParameterDescription("The ID of the source Planner to copy from.")] string sourcePlannerId,
            [ParameterDescription("The ID of the target Planner to copy to.")] string targetPlannerId)
        {
            var graphClient = GetAuthenticatedClient();

            // Get all buckets from the source planner
            var sourceBuckets = await graphClient.Planner.Plans[sourcePlannerId].Buckets.Request().GetAsync();

            // Copy each bucket
            foreach (var sourceBucket in sourceBuckets)
            {
                var newBucket = new Microsoft.Graph.PlannerBucket
                {
                    Name = sourceBucket.Name,
                    PlanId = targetPlannerId
                };

                var createdBucket = await graphClient.Planner.Buckets.Request().AddAsync(newBucket);

                // Get all tasks from the source bucket
                var sourceTasks = await graphClient.Planner.Buckets[sourceBucket.Id].Tasks.Request().GetAsync();

                // Copy each task, including details and checklist
                foreach (var sourceTask in sourceTasks)
                {
                    var newTask = new Microsoft.Graph.PlannerTask
                    {
                        Title = sourceTask.Title,
                        BucketId = createdBucket.Id,
                        PlanId = targetPlannerId,
                        //  Details = new Microsoft.Graph.PlannerTaskDetails { Description = sourceTask.Details.Description }, 
                        DueDateTime = sourceTask.DueDateTime
                    };

                    var createdTask = await graphClient.Planner.Tasks.Request().AddAsync(newTask);

                    // Get task details including checklist
                    var sourceDetails = await graphClient.Planner.Tasks[sourceTask.Id].Details.Request().GetAsync();

                    // Copy task details including checklist
                    var newDetails = new Microsoft.Graph.PlannerTaskDetails
                    {

                        Description = sourceDetails.Description,
                        PreviewType = PlannerPreviewType.Checklist
                        //  Checklist = sourceDetails.Checklist.ToDictionary(t=> t.Key, t => new PlannerChecklistItem { Title = t.Value.Title }),
                    };
                    //    newDetails.PreviewType = PlannerPreviewType.Checklist;

                    if (newDetails.Checklist == null)
                    {
                        newDetails.Checklist = new PlannerChecklistItems();
                        newDetails.Checklist.AdditionalData = new Dictionary<string, object>();
                    }

                    foreach (var i in sourceDetails.Checklist)
                    {
                        newDetails.Checklist.AdditionalData.Add(Guid.NewGuid().ToString(),
                            new
                            {
                                OdataType = "microsoft.graph.plannerChecklistItem",
                                Title = i.Value.Title,
                                IsChecked = false
                            }
                         );
                    }

                    var request = graphClient.Planner.Tasks[createdTask.Id].Details.Request();
                    var currentDetails = await graphClient.Planner.Tasks[createdTask.Id].Details.Request().GetAsync();

                    var eTagId = currentDetails.GetEtag();

                    await request.Header("Prefer", "return=representation")
                    .Header("If-Match", eTagId)
                    .UpdateAsync(newDetails);

                    await request.UpdateAsync(newDetails);
                }
            }

            return SuccessResponse();
        }



    }
}