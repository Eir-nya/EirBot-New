using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using EirBot_New.Serialization;
using System.Net;

namespace EirBot_New.Events.Starboard;

[EventHandler]
public class StarboardEvents {
	// Maximum size of attachments to attempt to download and reupload in starboard message
	public const int MAX_FILE_SIZE = 26214400;
	public const string WEBHOOK_DEFAULT_NAME = "{0} - Starboard";

	public static DiscordEmoji? starEmoji;

	// Initializes star emoji
	[Event(DiscordEvent.Ready)]
	public static async Task Ready(DiscordClient client, ReadyEventArgs args) {
		starEmoji = DiscordEmoji.FromName(client, ":star:");
	}

	// Reaction added: add to starboard and update
	[Event(DiscordEvent.MessageReactionAdded)]
	public static async Task ReactionAdded(DiscordClient client, MessageReactionAddEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		if (args.Emoji != starEmoji)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		if (args.Message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (settings.Value.ignoredChannels.Contains(args.Channel.Id))
			return;
		if (await CountReactions(client, message, args.Channel) < settings.Value.minStars)
			return;

		// Get or create webhook if possible
		DiscordWebhook? hook = null;
		if (settings.Value.useWebhook)
			hook = await GetOrCreateWebhook(client, starboardChannel);

		// Message already exists, but its webhook status is not what we want, so delete it
		if (settings.Value.messageLookup.ContainsKey(message.Id)) {
			// Get starboard message, if it already exists
			DiscordMessage? starboardMessage = await GetStarboardMessage(message.Id, settings.Value, starboardChannel, hook);
			if (starboardMessage != null)
				if (starboardMessage.WebhookMessage != settings.Value.useWebhook && await CheckWebhookPerms(client, starboardChannel)) {
					settings.Value.messageLookup.Remove(message.Id);
					try { await starboardMessage.DeleteAsync(); } catch {}
				}
		}

		// Message already exists - update star count
		if (settings.Value.messageLookup.ContainsKey(message.Id)) {
			// Get starboard message, if it already exists
			DiscordMessage? starboardMessage = await GetStarboardMessage(message.Id, settings.Value, starboardChannel, hook);
			if (starboardMessage == null)
				return;
			await UpdateStarboardMessage(client, message, starboardMessage, hook);
		// Message doesn't exist - create it
		} else {
			DiscordMessage newMessage;
			if (hook != null)
				newMessage = await hook.ExecuteAsync(await CreateStarboardMessageWebhook(client, message, true));
			else
				newMessage = await starboardChannel.SendMessageAsync(await CreateStarboardMessage(client, message, true));

			settings.Value.messageLookup[message.Id] = newMessage.Id;
			ServerData? serverData = ServerData.GetServerData(client, args.Guild);
			if (serverData == null)
				return;
			serverData.Save();
		}
	}

	// Reaction removed: remove from starboard or update
	[Event(DiscordEvent.MessageReactionRemoved)]
	public static async Task ReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		if (args.Emoji != starEmoji)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		if (message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(message.Id))
			return;
		if (settings.Value.ignoredChannels.Contains(args.Channel.Id))
			return;

		// Get webhook if possible
		DiscordWebhook? hook = null;
		if (settings.Value.useWebhook)
			hook = await GetWebhook(client, starboardChannel);

		// Get starboard message, if it already exists
		DiscordMessage? starboardMessage = await GetStarboardMessage(message.Id, settings.Value, starboardChannel, hook);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await CheckWebhookPerms(client, starboardChannel))))
			return;

		short reactions = await CountReactions(client, message, args.Channel);

		// Fell below minimum stars - remove
		if (reactions < settings.Value.minStars && settings.Value.removeWhenUnstarred) {
			await DeleteStarboardMessage(client, starboardMessage, message.Id, settings.Value, args.Guild, hook);
			return;
		}

		// Update star count
		await UpdateStarboardMessage(client, message, starboardMessage, hook);
	}

	[Event(DiscordEvent.MessageUpdated)]
	public static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		if (message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(message.Id))
			return;
		if (settings.Value.ignoredChannels.Contains(args.Channel.Id))
			return;

		// Get webhook if possible
		DiscordWebhook? hook = null;
		if (settings.Value.useWebhook)
			hook = await GetWebhook(client, starboardChannel);

		// Get starboard message, if it already exists
		DiscordMessage? starboardMessage = await GetStarboardMessage(message.Id, settings.Value, starboardChannel, hook);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await CheckWebhookPerms(client, starboardChannel))))
			return;

		// Update message
		await UpdateStarboardMessage(client, message, starboardMessage, hook);
	}

	[Event(DiscordEvent.MessageDeleted)]
	public static async Task MessageRemoved(DiscordClient client, MessageDeleteEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(args.Message.Id))
			return;

		// Get webhook if possible
		DiscordWebhook? hook = null;
		if (settings.Value.useWebhook)
			hook = await GetWebhook(client, starboardChannel);

		DiscordMessage? starboardMessage = await GetStarboardMessage(args.Message.Id, settings.Value, starboardChannel, hook);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await CheckWebhookPerms(client, starboardChannel))))
			return;

		// Remove
		if (settings.Value.removeWhenDeleted)
			await DeleteStarboardMessage(client, starboardMessage, args.Message.Id, settings.Value, args.Guild, hook);
	}


	private static async Task UpdateStarboardMessage(DiscordClient client, DiscordMessage starredMessage, DiscordMessage starboardMessage, DiscordWebhook? hook) {
		// Webhook
		if (hook != null)
			await hook.EditMessageAsync(starboardMessage.Id, await CreateStarboardMessageWebhook(client, starredMessage, false));
		// No webhook
		else
			await starboardMessage.ModifyAsync(await CreateStarboardMessage(client, starredMessage, false));
	}

	private static async Task DeleteStarboardMessage(DiscordClient client, DiscordMessage starboardMessage, ulong starredMessageID, StarboardSettings settings, DiscordGuild guild, DiscordWebhook? hook) {
		if (hook != null)
			await hook.DeleteMessageAsync(starboardMessage.Id);
		else
			await starboardMessage.DeleteAsync();
		settings.messageLookup.Remove(starredMessageID);
		ServerData? serverData = ServerData.GetServerData(client, guild);
		if (serverData == null)
			return;
		serverData.Save();
	}

	private static async Task<DiscordMessage?> GetStarboardMessage(ulong starredMessageID, StarboardSettings settings, DiscordChannel starboardChannel, DiscordWebhook? hook) {
		DiscordMessage? starboardMessage;
		// Webhook
		if (hook != null)
			starboardMessage = await hook.GetMessageAsync(settings.messageLookup[starredMessageID]);
		// No webhook
		else
			starboardMessage = await Util.GetMessageFixed(settings.messageLookup[starredMessageID], starboardChannel);
		return starboardMessage;
	}



	private static async Task<DiscordWebhook?> GetOrCreateWebhook(DiscordClient client, DiscordChannel channel) {
		if (!await CheckWebhookPerms(client, channel))
			return null;
		DiscordWebhook? hook = await GetWebhook(client, channel);
		// Create webhook
		if (hook == null)
			hook = await channel.CreateWebhookAsync(string.Format(WEBHOOK_DEFAULT_NAME, client.CurrentUser.Username), new MemoryStream(new WebClient().DownloadData(new Uri(client.CurrentUser.AvatarUrl))), "Starboard");
		return hook;
	}

	public static async Task<DiscordWebhook?> GetWebhook(DiscordClient client, DiscordChannel channel) {
		if (!await CheckWebhookPerms(client, channel))
			return null;
		IReadOnlyList<DiscordWebhook> hooks = await channel.Guild.GetWebhooksAsync();
		foreach (DiscordWebhook hook in hooks)
			if (hook.User == client.CurrentUser)
				return hook;
		return null;
	}

	private static async Task<bool> CheckWebhookPerms(DiscordClient client, DiscordChannel channel) {
		DiscordMember botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, false);
		if (botMember == null)
			botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, true);
		if (botMember == null)
			return false;
		return channel.PermissionsFor(botMember).HasFlag(Permissions.ManageWebhooks);
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

	private static async Task<short> CountReactions(DiscordClient client, DiscordMessage message, DiscordChannel channel) {
		if (message.Channel.Guild == null) {
			message = await Util.GetMessageFixed(message.Id, channel);
			if (message == null || message.Channel.Guild == null)
				return 0;
		}
		DiscordGuild guild = message.Channel.Guild;
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

	// Returns "attachments string" for embedding too-large attachments (>25 MB)
	private static async Task<(string, Dictionary<string, Stream>)> HandleAttachments(DiscordMessage originalMessage, bool downloadFiles, bool readContent) {
		string attachmentString = readContent ? originalMessage.Content : string.Empty;
		Dictionary<string, Stream> files = new Dictionary<string, Stream>();

		foreach (DiscordAttachment attachment in originalMessage.Attachments) {
			// Retrieve file
			if (attachment.FileSize.GetValueOrDefault(0) <= MAX_FILE_SIZE) {
				if (downloadFiles)
					files[attachment.FileName] = await new HttpClient().GetStreamAsync(attachment.Url);
			// Add to attachment string
			} else
				attachmentString += attachment.Url + "\n";
		}
		if (attachmentString.EndsWith("\n"))
			attachmentString = attachmentString.Substring(0, attachmentString.Length - 2);

		return (attachmentString, files);
	}

	private static async Task<DiscordMessageBuilder> CreateStarboardMessage(DiscordClient client, DiscordMessage message, bool newMessage) {
		short reactions = await CountReactions(client, message, message.Channel);

		bool hasNonGuildSticker = message.Stickers.Count == 1 && message.Stickers[0].Guild.Id != message.Channel.Guild.Id;

		(string, Dictionary<string, Stream>) attachmentData = await HandleAttachments(message, newMessage, false);
		string attachmentString = attachmentData.Item1;
		Dictionary<string, Stream> newAttachments = attachmentData.Item2;
		if (hasNonGuildSticker) {
			string toAdd = "\n" + message.Stickers[0].Url;
			if (attachmentString.Length + toAdd.Length > 2000 - 10)
				toAdd = "\n:warning:";
			attachmentString += toAdd;
		}

		DiscordMessageBuilder mb = new DiscordMessageBuilder()
			.WithContent(attachmentString);
		if (newMessage)
			if (newAttachments.Count > 0)
				mb.WithFiles(newAttachments);
		if (message.Stickers.Count == 1 && !hasNonGuildSticker)
			mb.WithSticker(message.Stickers[0]);
		return mb
			.WithEmbed(new DiscordEmbedBuilder()
				.WithColor(await Util.GetMemberColor(message.Author, message.Channel.Guild))
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(message.Author.Username + (message.Author.IsBot ? " [BOT]" : "") + " (⭐x" + reactions + ")", null, message.Author.AvatarUrl)
				.WithDescription(message.Content)
				.WithTimestamp(message.Timestamp)
			);
	}

	private static async Task<DiscordWebhookBuilder> CreateStarboardMessageWebhook(DiscordClient client, DiscordMessage message, bool newMessage) {
		short reactions = await CountReactions(client, message, message.Channel);

		bool hasStickerAndFiles = message.Stickers.Count == 1 && message.Attachments.Count > 0;

		(string, Dictionary<string, Stream>) attachmentData = await HandleAttachments(message, newMessage, true);
		string attachmentString = attachmentData.Item1;
		Dictionary<string, Stream> newAttachments = attachmentData.Item2;
		if (hasStickerAndFiles) {
			string toAdd = "\n" + message.Stickers[0].Url;
			if (attachmentString.Length + toAdd.Length > 2000 - 10)
				toAdd = "\n:warning:";
			attachmentString += toAdd;
		}

		DiscordWebhookBuilder wb = new DiscordWebhookBuilder()
			.WithContent(attachmentString);
		if (newMessage)
			if (newAttachments.Count > 0)
				wb.AddFiles(newAttachments);
		if (message.Stickers.Count == 1 && !hasStickerAndFiles)
			wb.AddFile(message.Stickers[0].Name, await new HttpClient().GetStreamAsync(message.Stickers[0].Url), false, message.Stickers[0].Description);
		if (newMessage) {
			wb.Username = message.Author.Username;
			wb.AvatarUrl = message.Author.AvatarUrl;
		}
		return wb
			.AddEmbed(new DiscordEmbedBuilder()
				.WithColor(await Util.GetMemberColor(message.Author, message.Channel.Guild))
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(message.Author.Username + (message.Author.IsBot ? " [BOT]" : "") + " (⭐x" + reactions + ")", null, message.Author.AvatarUrl)
				.WithTimestamp(message.Timestamp)
			);
	}
}
