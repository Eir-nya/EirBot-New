using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using EirBot_New.Serialization;

namespace EirBot_New.Events.Starboard;

[EventHandler]
public class StarboardEvents {
	public static DiscordEmoji? starEmoji;

	// Initializes star emoji
	[Event(DiscordEvent.Ready)]
	public static async Task Ready(DiscordClient client, ReadyEventArgs args) {
		starEmoji = DiscordEmoji.FromName(client, ":star:");
	}

	// Reaction added: add to starboard and update
	[Event(DiscordEvent.MessageReactionAdded)]
	public static async Task ReactionAdded(DiscordClient client, MessageReactionAddEventArgs args) {
		if (args.Message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (await CountReactions(client, args.Message) < settings.Value.minStars)
			return;

		// Message already exists - update star count
		if (settings.Value.messageLookup.ContainsKey(args.Message.Id)) {
			DiscordMessage oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id], false);
			if (oldMessage.GuildId == null)
				oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id], true);
			if (oldMessage == null)
				return;
			await args.Message.ModifyAsync(await CreateStarboardMessage(client, args.Message));
		// Message doesn't exist - create it
		} else {
			DiscordMessage newMessage = await starboardChannel.SendMessageAsync(await CreateStarboardMessage(client, args.Message));
			settings.Value.messageLookup[args.Message.Id] = newMessage.Id;
			ServerData? serverData = ServerData.GetServerData(client, args.Guild);
			if (serverData == null)
				return;
			serverData.Save();
		}
	}

	// Reaction removed: remove from starboard or update
	[Event(DiscordEvent.MessageReactionRemoved)]
	public static async Task ReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs args) {
		if (args.Message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(args.Message.Id))
			return;

		DiscordMessage oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id], false);
		if (oldMessage.GuildId == null)
			oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id], true);
		short reactions = await CountReactions(client, args.Message);

		// Fell below minimum stars - remove
		if (reactions < settings.Value.minStars && settings.Value.removeWhenUnstarred) {
			await oldMessage.DeleteAsync();
			settings.Value.messageLookup.Remove(args.Message.Id);
			ServerData? serverData = ServerData.GetServerData(client, args.Guild);
			if (serverData == null)
				return;
			serverData.Save();
			return;
		}
		// Update star count
		await oldMessage.ModifyAsync(await CreateStarboardMessage(client, args.Message));
	}



	private static StarboardSettings? GetSettings(DiscordClient client, DiscordGuild guild) {
		ServerData? serverData = ServerData.GetServerData(client, guild);
		if (serverData == null)
			return null;
		return serverData.starboardSettings;
	}

	private static DiscordChannel? GetStarboardChannel(DiscordClient client, DiscordGuild guild) {
		StarboardSettings? settings = GetSettings(client, guild);
		if (settings == null)
			return null;
		return guild.GetChannel(settings.Value.channelID);
	}

	private static async Task<short> CountReactions(DiscordClient client, DiscordMessage message) {
		if (message == null || message.GuildId == null)
			return 0;
		DiscordGuild? guild = await Util.GetGuildAsync(client, message.GuildId.Value, false);
		if (guild == null)
			return 0;
		StarboardSettings? settings = GetSettings(client, guild);
		if (settings == null)
			return 0;

		short count = 0;
		IReadOnlyList<DiscordUser> reacters = await message.GetReactionsAsync(starEmoji);
		foreach (DiscordUser user in reacters)
			if (user != message.Author || settings.Value.allowSelfStar)
				count++;
		return count;
	}

	private static async Task<DiscordMessageBuilder> CreateStarboardMessage(DiscordClient client, DiscordMessage message) {
		short reactions = await CountReactions(client, message);

		bool hasNonGuildSticker = message.Stickers.Count == 1 && message.Stickers[0].Guild.Id != message.GuildId;

		string[] attachmentURLs = new string[message.Attachments.Count + (hasNonGuildSticker ? 1 : 0)];
		for (int i = 0; i < message.Attachments.Count; i++)
			attachmentURLs[i] = message.Attachments[i].Url;
		if (hasNonGuildSticker)
			attachmentURLs[attachmentURLs.Length - 1] = message.Stickers[0].Url;

		DiscordMessageBuilder mb = new DiscordMessageBuilder();
		if (attachmentURLs.Length > 0)
			mb.WithContent(string.Join("\n", attachmentURLs));
		if (message.Stickers.Count == 1 && !hasNonGuildSticker)
			mb.WithSticker(message.Stickers[0]);
		return mb
			.WithEmbed(new DiscordEmbedBuilder()
				.WithColor(await Util.GetMemberColor(client, message.Author, message.GuildId.GetValueOrDefault(0)))
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(message.Author.Username + " (⭐x" + reactions + ")", null, message.Author.AvatarUrl)
				.WithDescription(message.Content)
				.WithTimestamp(message.Timestamp)
			);
	}
}
