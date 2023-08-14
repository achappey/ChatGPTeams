// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Attributes;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Services.Graph
{
    // This class is a wrapper for the Microsoft Graph API
    // See: https://developer.microsoft.com/en-us/graph
    public partial class GraphFunctionsClient
    {

        // Create a new channel message.
        [MethodDescription("Creates a new channel message.")]
        public async Task<ChatMessage> NewChannelMessage(
            [ParameterDescription("The ID of the team.")] string teamId,
            [ParameterDescription("The ID of the channel.")] string channelId,
            [ParameterDescription("The html content of the message.")] string messageContent)
        {
            var graphClient = GetAuthenticatedClient();

            var newMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    Content = messageContent,
                    ContentType = BodyType.Html
                }
            };

            return await graphClient.Teams[teamId].Channels[channelId].Messages
                .Request()
                .AddAsync(newMessage);
        }

        [MethodDescription("Creates a reply to a specific message in a team's channel.")]
        public async Task<Models.Graph.ChatMessage> NewChannelMessageReply(
    [ParameterDescription("The ID of the team.")] string teamId,
    [ParameterDescription("The ID of the channel.")] string channelId,
    [ParameterDescription("The ID of the message.")] string messageId,
    [ParameterDescription("The html content of the reply.")] string replyContent)
        {
            var graphClient = GetAuthenticatedClient();
            var replyMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = replyContent
                }
            };

            var newReply = await graphClient.Teams[teamId].Channels[channelId].Messages[messageId].Replies
                .Request()
                .AddAsync(replyMessage);

            return this._mapper.Map<achappey.ChatGPTeams.Models.Graph.ChatMessage>(newReply);
        }

        // Add a user to a team.
        [MethodDescription("Adds a member to a team based on the user's ID and team ID.")]
        public async Task<Models.Response> AddMemberToTeam(
            [ParameterDescription("The ID of the user.")] string userId,
            [ParameterDescription("The ID of the team.")] string teamId)
        {
            var graphClient = GetAuthenticatedClient();

            var member = new AadUserConversationMember
            {
                ODataType = "#microsoft.graph.aadUserConversationMember",
                Roles = new List<string>
                {
                    "member",
                },
                AdditionalData = new Dictionary<string, object>
                {
                    {
                        "user@odata.bind" , $"https://graph.microsoft.com/v1.0/users('{userId}')"
                    },
                },
            };

            await graphClient.Teams[teamId].Members
                .Request()
                .AddAsync(member);

            return SuccessResponse();
        }

        [MethodDescription("Adds an owner to a team based on the user's ID and team ID.")]
        public async Task<Models.Response> AddOwnerToTeam(
         [ParameterDescription("The ID of the user.")] string userId,
         [ParameterDescription("The ID of the team.")] string teamId)
        {
            var graphClient = GetAuthenticatedClient();

            var member = new AadUserConversationMember
            {
                ODataType = "#microsoft.graph.aadUserConversationMember",
                Roles = new List<string>
                {
                    "owner",
                },
                AdditionalData = new Dictionary<string, object>
                {
                    {
                        "user@odata.bind" , $"https://graph.microsoft.com/v1.0/users('{userId}')"
                    },
                },
            };

            await graphClient.Teams[teamId].Members
                .Request()
                .AddAsync(member);

            return SuccessResponse();
        }

        // Get information about a specific team by their ID.
        [MethodDescription("Gets information about a specific team based on the ID.")]
        public async Task<Models.Graph.Team> GetTeam(
            [ParameterDescription("The ID of the team.")] string teamId)
        {
            var graphClient = GetAuthenticatedClient();
            var item = await graphClient.Teams[teamId]
                .Request()
                .Select("id,displayName,description")
                .GetAsync();

            return _mapper.Map<Models.Graph.Team>(item);
        }

        [MethodDescription("Gets the channels of a specific team based on the ID.")]
        public async Task<IEnumerable<Models.Graph.Channel>> GetTeamChannels(
            [ParameterDescription("The ID of the team.")] string teamId)
        {
            var graphClient = GetAuthenticatedClient();
            var items = await graphClient.Teams[teamId].Channels
                .Request()
                .Select("id,displayName,description")
                .GetAsync();

            return items.Select(a => _mapper.Map<Models.Graph.Channel>(a));
        }

        [MethodDescription("Gets the messages of a specific channel in a team.")]
        public async Task<IEnumerable<Models.Graph.ChatMessage>> GetTeamChannelMessages(
      [ParameterDescription("The ID of the team.")] string teamId,
      [ParameterDescription("The ID of the channel.")] string channelId)
        {
            var graphClient = GetAuthenticatedClient();
            var items = await graphClient.Teams[teamId].Channels[channelId].Messages
                .Request()
                .Top(10)
                .GetAsync();

            return items.CurrentPage.Select(a => _mapper.Map<Models.Graph.ChatMessage>(a));
        }

        [MethodDescription("Gets the last 25 messages from a specific chat.")]
        public async Task<IEnumerable<Models.Graph.ChatMessage>> GetChatMessages(
    [ParameterDescription("The ID of the chat.")] string chatId)
        {
            var graphClient = GetAuthenticatedClient();
            var items = await graphClient.Chats[chatId].Messages
                .Request()
                .Top(25)
                .OrderBy("createdDateTime DESC")
                .GetAsync();

            return items.CurrentPage
                .OrderBy(a => a.CreatedDateTime)
                .Select(a => _mapper.Map<Models.Graph.ChatMessage>(a));
        }

        [MethodDescription("Creates a new message in a specific chat.")]
        public async Task<Models.Response> NewChatMessage(
           [ParameterDescription("The ID of the chat.")] string chatId,
           [ParameterDescription("The html content of the message.")] string content)
        {
            var graphClient = GetAuthenticatedClient();
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Html,
                },
            };

            await graphClient.Chats[chatId].Messages
                .Request()
                .AddAsync(chatMessage);

            return SuccessResponse();
        }

        [MethodDescription("Gets the replies to a specific message in a team's channel.")]
        public async Task<IEnumerable<Models.Graph.ChatMessage>> GetChannelMessageReplies(
            [ParameterDescription("The ID of the team.")] string teamId,
            [ParameterDescription("The ID of the channel.")] string channelId,
            [ParameterDescription("The ID of the message.")] string messageId)
        {
            var graphClient = GetAuthenticatedClient();

            var items = await graphClient.Teams[teamId].Channels[channelId].Messages[messageId].Replies
                .Request()
                .Top(10)
                .GetAsync();

            return items.CurrentPage.Select(reply => this._mapper.Map<Models.Graph.ChatMessage>(reply));
        }


        [MethodDescription("Updates a channel of a specific team based on the team ID and channel ID.")]
        public async Task<Models.Graph.Channel> UpdateTeamChannel(
            [ParameterDescription("The ID of the team.")] string teamId,
            [ParameterDescription("The ID of the channel.")] string channelId,
            [ParameterDescription("The new display name for the channel.")] string newDisplayName,
            [ParameterDescription("The new description for the channel.")] string newDescription)
        {
            var graphClient = GetAuthenticatedClient();

            var updatedChannel = new Channel()
            {
                DisplayName = newDisplayName,
                Description = newDescription,
                Id = channelId
            };

            var channel = await graphClient.Teams[teamId].Channels[channelId]
                .Request()
                .UpdateAsync(updatedChannel);

            return this._mapper.Map<Models.Graph.Channel>(channel);
        }

        [MethodDescription("Updates a team based on the team ID.")]
        public async Task<Models.Graph.Team> UpdateTeam(
    [ParameterDescription("The ID of the team.")] string teamId,
    [ParameterDescription("The new display name for the team.")] string newDisplayName,
    [ParameterDescription("The new description for the team.")] string newDescription)
        {
            var graphClient = GetAuthenticatedClient();

            var updatedTeam = new Team()
            {
                DisplayName = newDisplayName,
                Description = newDescription,
                Id = teamId
            };

            var team = await graphClient.Teams[teamId]
                .Request()
                .UpdateAsync(updatedTeam);

            return _mapper.Map<Models.Graph.Team>(team);
        }


        [MethodDescription("Retrieves all teamwork devices. Optionally filters by the current signed-in user.")]
        public async Task<IEnumerable<Models.Graph.TeamworkDevice>> GetTeamworkDevices()
        {
            var graphClient = GetAuthenticatedClient();

            var devices = await graphClient.Teamwork.Devices.Request().GetAsync();

            return devices.Select(t => _mapper.Map<Models.Graph.TeamworkDevice>(t));
        }

        [MethodDescription("Restarts a specific teamwork device by device ID.")]
        public async Task<Models.Response> RestartTeamworkDevice(
            [ParameterDescription("The ID of the teamwork device to restart.")] string deviceId)
        {
            var graphClient = GetAuthenticatedClient();

            // Make a POST request to the restart endpoint for the specified device
            await graphClient.Teamwork.Devices[deviceId].Restart().Request().PostAsync();

            return SuccessResponse();
        }


    }
}