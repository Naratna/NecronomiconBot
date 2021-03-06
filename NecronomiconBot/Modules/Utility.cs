﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using Discord.WebSocket;
using NecronomiconBot.Logic;
using NecronomiconBot.Settings;
using NecronomiconBot.Logic.Distribution;

namespace NecronomiconBot.Modules
{
    public class Utility: NecroModuleBase<SocketCommandContext>
    {
        public async Task OnMention()
        {
            string naturalPrefix = CommandHandler.GetPrefix("natural", Context.Guild.Id, Context.Channel.Id, Context.User.Id);
            string syntheticPrefix = CommandHandler.GetPrefix("synthetic", Context.Guild.Id, Context.Channel.Id, Context.User.Id);
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>()
            {
                new EmbedFieldBuilder(){Value = $"{syntheticPrefix}help", Name = "Synthetic prefix:"},
                new EmbedFieldBuilder(){Value = $"{naturalPrefix} help | {naturalPrefix}, help", Name = "Natural prefix:"},
                new EmbedFieldBuilder(){Value = $"{Context.Client.CurrentUser.Mention} help", Name = "Mention:"}
            };
            var eb = new EmbedBuilder()
            {
                Title = "Use the help command to know what I can do",
                Fields = fields
                
            };
            await ReplyAsync(embed:eb.Build());
        }

        [Command("say")]
        [Alias("echo")]
        [Summary("Send a message in chat.")]
        public Task Say([Remainder][Summary("The texto to say")] string message)
            => ReplyAsync(message);

        [Command("avatar")]
        [Alias("profile", "pfp")]
        public Task Avatar([Remainder]SocketUser user)
            => ReplyAsync(user.GetAvatarUrl(size: 256));

        [Command("unedit")]
        public async Task UneditAsync()
        {
            IMessage message = await GetParentMessageAsync(Context.Message);
            if (message.EditedTimestamp is null)
                return;
            if (BotSettings.Instance.GetChannelSettingOrDefault<bool>("unedit:opt-out", Context.Guild.Id, Context.Channel.Id)[0])
            {
                await ReplyAsync("Sorry, that command is disabled for this channel or server");
                return;
            }
            if (BotSettings.Instance.GetUserSettingOrDefault<bool>("unedit:opt-out", Context.Guild.Id, message.Author.Id)[0])
            {
                await ReplyAsync("Sorry, the targeted user has opted out of this feature");
                return;
            }
            var messages = Logic.MessageHistory.Instance.GetHistory(message);
            if (messages is null)
                return;
            _ = SendAllHistory(messages);
        }
        [Command("undelete")]
        public async Task UndeleteAsync()
        {
            if (BotSettings.Instance.GetChannelSettingOrDefault<bool>("undelete:opt-out", Context.Guild.Id, Context.Channel.Id)[0])
            {
                await ReplyAsync("Sorry, that command is disabled for this channel or server");
                return;
            }
            var messages = Logic.MessageHistory.Instance.GetLastDeletedHistory(Context.Message.Channel);
            if (messages is null)
                return;
            if (BotSettings.Instance.GetUserSettingOrDefault<bool>("undelete:opt-out", Context.Guild.Id, messages.First.Value.Author.Id)[0])
            {
                await ReplyAsync("Sorry, the latest user to have deleted a message in this channel has opted out of this feature");
                return;
            }
            _ = SendAllHistory(messages);
        }
        private async Task SendAllHistory(ICollection<IMessage> messages)
        {
            foreach (var item in messages)
            {
                var eb = Quote(item.Author, item.Content, item.Timestamp);
                await ReplyAsync(embed: eb.Build());
            }
        }

        [Command("secret santa")]
        [Alias("secret-santa")]
        public async Task SecretSanta(SocketRole participatingRole, [Remainder]string message = null)
        {
            DateTime lastSent = BotSettings.Instance.GetGuildSettingOrDefault<DateTime>("_secret-santa:last-sent", Context.Guild.Id)[0];
            if (Context.User.Id != Context.Guild.OwnerId && DateTime.Today - lastSent < TimeSpan.FromDays(1))
            {
                await ReplyAsync("This command can only be used once every 24 hours per server, only the owner of the server may bypass this restriction");
                return;
            }

            List<IUser> participatingUsers = new List<IUser>();
            foreach (var user in participatingRole.Members)
                if (!user.IsBot)
                    participatingUsers.Add(user);

            if (participatingUsers.Count <= 1)
            {
                await ReplyAsync("You need at least four people for a Secret Santa.");
                return;
            }
            if (participatingUsers.Count <= 3)
                await ReplyAsync("This isn't much of a \"Secret\" Santa, but whatever.");
            if (participatingUsers.Count > 30)
            {
                await ReplyAsync("Sorry, I can only do Secret Santas with 30 people or fewer.");
            }
            List<IUser> derrangedList = new List<IUser>(participatingUsers);
            derrangedList.Derrange();
            Embed organizerMessage = null;
            if (message != null)
                organizerMessage = new EmbedBuilder() { Description = message }.Build();

            await ReplyAsync($"I will be messageing the members of `{participatingRole.Name}` with information about the draw!");
            IList<string> setting = BotSettings.Instance.GetGuildSetting("_secret-santa:last-sent", Context.Guild.Id);
            string now = DateTime.Today.ToString();
            if (setting.Count < 1)
                setting.Add(now);
            else
                setting[0] = now;

            int i = 0;
            foreach (var gifter in participatingUsers)
            {
                _ = SendSecretSantaMessage(gifter, derrangedList[i++], organizerMessage, participatingRole);
            }
        }

        private async Task SendSecretSantaMessage(IUser gifter, IUser giftee, Embed organizerMessage, IRole participatingRole)
        {
            var DMChannel = await gifter.GetOrCreateDMChannelAsync();
            string message = $"Hello! These are the results of the Secret Santa draw created by {Context.Message.Author.Mention} ({Context.Message.Author.Username}#{Context.Message.Author.Discriminator})" +
                $" in the server `{Context.Guild.Name}`, among the members of role `{participatingRole.Name}`\n" +
                $"Click this black box to find out the username of the person you will be gifting to:\n||You will be gifting {giftee.Mention} ({giftee.Username}#{giftee.Discriminator})!||";
            if (organizerMessage != null)
                message += "\nThe organizer for this Secret Santa has attached a message for you:\n";
            await DMChannel.SendMessageAsync(message, embed:organizerMessage);
        }

        [Command("help")]
        [Alias("h","-h","--help","halp")]
        public async Task Help()
        {
            await ReplyAsync("Sorry, this command has not yet been implemented");
        }

    }
}
