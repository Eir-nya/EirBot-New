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
			DiscordMessage oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id]);
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
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(args.Message.Id))
			return;

		DiscordMessage oldMessage = await starboardChannel.GetMessageAsync(settings.Value.messageLookup[args.Message.Id]);
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
		DiscordGuild? guild = await client.GetGuildAsync(message.GuildId.Value);
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

		string[] attachmentURLs = new string[message.Attachments.Count];
		for (int i = 0; i < message.Attachments.Count; i++)
			attachmentURLs[i] = message.Attachments[i].Url;

		DiscordMessageBuilder mb = new DiscordMessageBuilder();
		if (attachmentURLs.Length > 0)
			mb.WithContent(string.Join("\n", attachmentURLs));
		if (message.Stickers.Count > 0)
			if (message.Stickers[0].Guild.Id == message.GuildId)
				mb.WithSticker(message.Stickers[0]);
		return mb
			.WithEmbed(new DiscordEmbedBuilder()
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(message.Author.Username + " (⭐x" + reactions + ")", null, message.Author.AvatarUrl)
				.WithDescription(message.Content)
				.WithTimestamp(message.Timestamp)
			);
	}
}
