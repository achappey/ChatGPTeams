#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Models
{
    /// <summary>
    /// Represents a chat message with role and content.
    /// </summary>
    public class Message
    {
        public string Id { get; set; }

        public string ConversationId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the role of the sender of the chat message.
        /// </summary>
        public Role Role { get; set; }

        /// <summary>
        /// Gets or sets the content of the chat message.
        /// </summary>
        public string? Content { get; set; }

        public string? Name { get; set; }

        public string? TeamsId { get; set; }

        public string? ContextQuery { get; set; }        

        public ChatType ChatType { get; set; }

        public FunctionCall? FunctionCall { get; set; }

        public DateTimeOffset? Created { get; set; }

        public ConversationReference Reference { get; set; } = null!;
    }


    public enum ChatType
    {
        personal,
        channel,
        groupchat
    }

    public enum Role
    {
        user,
        assistant,
        function,
        system
    }

    public class FunctionCall
    {
        public string Name { get; set; } = null!;

        public string Arguments { get; set; } = null!;

    }
}
