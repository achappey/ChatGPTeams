#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Models
{
    public class Conversation
    {
        public string Id { get; set; } = null!;

        public Assistant? Assistant { get; set; }

        public IEnumerable<Message>? Messages { get; set; }

        public IEnumerable<Resource>? Resources { get; set; }

        public IEnumerable<FunctionDefinition>? FunctionDefinitions { get; set; }

        public IEnumerable<Function>? Functions { get; set; }

        public float Temperature { get; set; }

        public ChatType ChatType { get; set; }

        public string Title { get; set; } = null!;

        public DateTimeOffset? CutOff { get; set; }

        public IEnumerable<Resource>? AllResources
        {
            get
            {
                var resources = new List<Resource>();

                if (Assistant?.Resources != null)
                {
                    resources.AddRange(Assistant?.Resources!);
                }

                if (Resources != null)
                {
                    resources.AddRange(Resources);
                }

                return resources;
            }
        }

        public IEnumerable<string>? AllFunctionNames
        {
            get
            {
                var functionNames = new List<string>();

                if (Functions != null)
                {
                    functionNames.AddRange(Functions.Select(y => y.Name));
                }

                if (Assistant?.Functions != null)
                {
                    functionNames.AddRange(Assistant.Functions.Select(y => y.Name));
                }

                return functionNames.Distinct();
            }
        }
    }

    public class ConversationContext
    {
        public string Id { get; set; } = null!;

        public string? TeamsId { get; set; }

        public string? MessageId { get; set; }
        public string? UserDisplayName { get; set; }

        public string? ChannelId { get; set; }

        public string? ReplyToId { get; set; }

        public ChatType ChatType { get; set; }
    }
}
