﻿using HassClient.Net.Models;
using HassClient.Net.Helpers;
using HassClient.Net.WSMessages;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace HassClient.Net.Tests.Mocks.HassServer
{
    internal class EntityRegistryStorageCollectionCommandProcessor
        : StorageCollectionCommandProcessor<EntityRegistryMessagesFactory, RegistryEntry>
    {
        private class MockRegistryEntity : RegistryEntry
        {
            [JsonIgnore]
            public RegistryEntryBase Entry;

            public MockRegistryEntity(string entityId, string originalName, string originalIcon = null, DisabledByEnum disabledBy = DisabledByEnum.None)
            : base(entityId, disabledBy)
            {
                this.OriginalName = originalName;
                this.OriginalIcon = originalIcon;
            }

            public MockRegistryEntity(RegistryEntryBase entry, DisabledByEnum disabledBy = DisabledByEnum.None)
            : this(entry.EntityId, entry.Name, entry.Icon, disabledBy)
            {
                this.Entry = entry;
                this.UniqueId = entry.UniqueId;
                this.Name = entry.Name;
                this.Icon = entry.Icon;
            }

            public new void Update(RegistryEntry updatedEntry, string newEntityId)
            {
                base.Update(updatedEntry, newEntityId);
                this.Entry.Name = this.Name;
                this.Entry.Icon = this.Icon;
                this.Entry.UniqueId = this.EntityId.SplitEntityId()[1];
            }
        }

        public EntityRegistryStorageCollectionCommandProcessor()
        {
        }

        protected override bool IsValidCommandType(string commandType)
        {
            return commandType.EndsWith("create") ||
                   commandType.EndsWith("list") ||
                   commandType.EndsWith("update") ||
                   commandType.EndsWith("get") ||
                   commandType.EndsWith("remove");
        }

        protected override IEnumerable<RegistryEntry> ProccessListCommand(MockHassServerRequestContext context, JToken merged)
        {
            return context.HassDB.GetAllEntityEntries().Select(x => x as RegistryEntry ?? RegistryEntry.CreateFromEntry(x));
        }

        protected override RegistryEntry ProccessUpdateCommand(MockHassServerRequestContext context, JToken merged)
        {
            var newEntityIdProperty = merged.FirstOrDefault(x => (x is JProperty property) && property.Name == "new_entity_id");
            var newEntityId = (string)newEntityIdProperty;
            newEntityIdProperty?.Remove();

            var updatedModel = this.DeserializeModel(merged);
            var result = this.FindRegistryEntry(context, updatedModel.EntityId, createIfNotFound: true);
            if (result != null)
            {
                if (newEntityId != null)
                {
                    context.HassDB.DeleteObject(result.Entry);
                }

                result.Update(updatedModel, newEntityId);

                if (newEntityId != null)
                {
                    context.HassDB.CreateObject(result.Entry);
                }
            }

            return result;
        }

        protected override object ProccessUnknownCommand(string commandType, MockHassServerRequestContext context, JToken merged)
        {
            var entityId = merged.Value<string>("entity_id");
            if (string.IsNullOrEmpty(entityId))
            {
                return ErrorCodes.InvalidFormat;
            }

            if (commandType.EndsWith("get"))
            {
                return this.FindRegistryEntry(context, entityId, createIfNotFound: true);
            }
            else if (commandType.EndsWith("remove"))
            {
                var mockEntry = this.FindRegistryEntry(context, entityId, createIfNotFound: false);
                if (mockEntry == null)
                {
                    return ErrorCodes.NotFound;
                }

                context.HassDB.DeleteObject(mockEntry);
                var result = context.HassDB.DeleteObject(mockEntry.Entry);
                return result ? null : (object)ErrorCodes.NotFound;
            }

            return base.ProccessUnknownCommand(commandType, context, merged);
        }

        private MockRegistryEntity FindRegistryEntry(MockHassServerRequestContext context, string entityId, bool createIfNotFound)
        {
            var hassDB = context.HassDB;
            var result = hassDB.GetObjects<MockRegistryEntity>()?.FirstOrDefault(x => x.EntityId == entityId);
            if (result != null)
            {
                return result;
            }

            var entry = hassDB.FindEntityEntry(entityId);
            if (entry == null)
            {
                return null;
            }

            result = new MockRegistryEntity(entry);

            if (createIfNotFound)
            {
                hassDB.CreateObject(result);
            }

            return result;
        }

        protected override void PrepareHassContext(MockHassServerRequestContext context)
        {
            base.PrepareHassContext(context);
            var hassDB = context.HassDB;
            hassDB.CreateObject(new MockRegistryEntity("weather.home", "Weather")
            {
                UniqueId = this.faker.RandomUUID(),
                ConfigEntryId = this.faker.RandomUUID(),
            });

            hassDB.CreateObject(new MockRegistryEntity("switch.fake_switch", "Fake Switch", "mdi: switch", DisabledByEnum.Integration)
            {
                UniqueId = this.faker.RandomUUID(),
                ConfigEntryId = this.faker.RandomUUID()
            });
        }
    }
}