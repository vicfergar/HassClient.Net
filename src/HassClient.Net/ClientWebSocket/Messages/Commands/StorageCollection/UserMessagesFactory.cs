﻿using HassClient.Net.ClientWebSocket.Messages.Commands;
using HassClient.Net.Models;

namespace HassClient.Net.WSMessages
{
    internal class UserMessagesFactory : StorageCollectionMessagesFactory
    {
        public static UserMessagesFactory Instance = new UserMessagesFactory();

        public UserMessagesFactory()
            : base("config/auth", "user")
        {
        }

        public BaseOutgoingMessage CreateCreateMessage(User user)
        {
            var selectedProperties = new[] { nameof(User.Name), nameof(User.GroupIds) };
            return this.CreateCreateMessage(user, selectedProperties);
        }

        public BaseOutgoingMessage CreateUpdateMessage(User user)
        {
            var selectedProperties = new[] { nameof(User.Name), nameof(User.GroupIds) };
            return this.CreateUpdateMessage(user.Id, user, selectedProperties);
        }

        public BaseOutgoingMessage CreateDeleteMessage(User user)
        {
            return this.CreateDeleteMessage(user.Id);
        }
    }
}
