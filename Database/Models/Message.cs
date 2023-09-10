#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace achappey.ChatGPTeams.Database.Models
{
    public class Message
    {
        public int  Id { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public string ConversationId { get; set; } = null!;

        public Role Role { get; set; }

        public string? Content { get; set; }

        public string? Name { get; set; }

        public string? TeamsId { get; set; }

       // public ChatType ChatType { get; set; }

        public string? FunctionCall { get; set; }

        public DateTimeOffset? Created { get; set; }

        public string Reference { get; set; } = null!;

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

}
