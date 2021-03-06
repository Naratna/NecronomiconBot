﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NecronomiconBot.Modules
{
    public class NecroModuleBase<T> : ModuleBase<T> where T: class, ICommandContext
    {
        public new T Context { get => GetContext(); }
        private T context;

        private T GetContext()
        {
            return context ?? base.Context;
        }
        public void SetContext(T context)
        {
            this.context = context;
        }
        protected override async Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            return await Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
        }

        protected async Task<IMessage> GetPreviousMessageAsync(IMessage message)
        {
            IMessage previousMessage = null;
            var asyncMessages = Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, 1);
            var messages = await AsyncEnumerableExtensions.FlattenAsync(asyncMessages);
            foreach (var item in messages)
            {
                previousMessage = item;
            }
            return previousMessage;
        }

        protected async Task<IMessage> GetReferencedMessageAsync(IMessage message)
        {
            var reference = message.Reference;
            if (reference == null)
                return null;

            var guild = await Context.Client.GetGuildAsync(reference.GuildId.Value);
            var channel = await guild.GetTextChannelAsync(reference.ChannelId);
            return await channel.GetMessageAsync(reference.MessageId.Value);
        }

        protected async Task<IMessage> GetParentMessageAsync(IMessage message)
        {
            if (message.Reference == null)
                return await GetPreviousMessageAsync(message);
            else
                return await GetReferencedMessageAsync(message);
        }

        protected EmbedBuilder Quote(IUser author, string text, DateTimeOffset? timestamp = null)
        {
            var eab = new EmbedAuthorBuilder()
            {
                IconUrl = author.GetAvatarUrl(),
                Name = author.Username
            };
            var eb = new EmbedBuilder()
            {
                Author = eab,
                Timestamp = timestamp,
                Description = text
            };
            return eb;
        }
    }
}
