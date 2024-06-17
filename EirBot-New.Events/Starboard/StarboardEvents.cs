using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using EirBot_New.Attributes;
using EirBot_New.Serialization;

namespace EirBot_New.Events.Starboard;

// [EventHandler]
public class StarboardEvents {
	// Maximum size of attachments to attempt to download and reupload in starboard message
	public const int MAX_FILE_SIZE = 26214400;

	// Run on bot ready
	[RunOnStartup]
	private static void RunOnStartup(DiscordShardedClient client) {
		client.MessageReactionAdded += ReactionAdded;
		client.MessageReactionRemoved += ReactionRemoved;
		client.MessageUpdated += MessageUpdated;
		client.MessageDeleted += MessageRemoved;
	}

	// Reaction added: add to starboard and update
	// [Event(DiscordEvent.MessageReactionAdded)]
	public static async Task ReactionAdded(DiscordClient client, MessageReactionAddEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		// if (args.Message.Author == client.CurrentUser)
		// 	return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		if (!settings.Value.acceptedEmoji.Contains(args.Emoji.ToString()) && !settings.Value.acceptedEmoji.Contains(args.Emoji.GetDiscordName()))
			return;
		if (args.Channel.IsNsfw && !settings.Value.allowNSFW)
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
			hook = await Util.GetOrCreateWebhook(client, starboardChannel);

		// Message already exists, but its webhook status is not what we want, so delete it
		if (settings.Value.messageLookup.ContainsKey(message.Id)) {
			// Get starboard message, if it already exists
			DiscordMessage? starboardMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, false);
			DiscordMessage? starboardJumpMessage = null;
			if (settings.Value.webhookJumpMessageLookup.ContainsKey(message.Id))
				starboardJumpMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, true);
			if (starboardMessage != null)
				if (starboardMessage.WebhookMessage != (settings.Value.useWebhook && await Util.CheckWebhookPerms(client, starboardChannel))) {
					settings.Value.messageLookup.Remove(message.Id);
					try { await starboardMessage.DeleteAsync(); } catch {}
				}
			if (starboardJumpMessage != null)
				if (!settings.Value.useWebhook) {
					settings.Value.webhookJumpMessageLookup.Remove(message.Id);
					try { await starboardJumpMessage.DeleteAsync(); } catch {}
				}
		}

		// Message already exists - update star count
		if (settings.Value.messageLookup.ContainsKey(message.Id)) {
			// Get starboard message, if it already exists
			DiscordMessage? starboardMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, false);
			DiscordMessage? starboardJumpMessage = null;
			if (settings.Value.webhookJumpMessageLookup.ContainsKey(message.Id))
				starboardJumpMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, true);
			if (starboardMessage == null)
				return;
			if (settings.Value.useWebhook && hook != null)
				await UpdateStarboardJumpMessage(client, message, starboardJumpMessage, hook);
			else
				await UpdateStarboardMessage(client, message, starboardMessage, hook);
		// Message doesn't exist - create it
		} else {
			DiscordMessage newMessage, webhookJumpMessage = null;
			if (hook != null) {
				await Util.ModifyWebhookAsync(hook, null, null, starboardChannel.Id);
				DiscordWebhookBuilder dwb = await CreateStarboardMessageWebhook(client, message, true);
				newMessage = await hook.ExecuteAsync(dwb);
				DiscordWebhookBuilder jumpDwb = await CreateStarboardJumpMessage(client, message, true);
				webhookJumpMessage = await hook.ExecuteAsync(jumpDwb);
			} else
				newMessage = await starboardChannel.SendMessageAsync(await CreateStarboardMessage(client, message, true));

			settings.Value.messageLookup[message.Id] = newMessage.Id;
			if (webhookJumpMessage != null)
				settings.Value.webhookJumpMessageLookup[message.Id] = webhookJumpMessage.Id;
			ServerData? serverData = ServerData.GetServerData(client, args.Guild);
			if (serverData == null)
				return;
			serverData.Save();
		}
	}

	// Reaction removed: remove from starboard or update
	// [Event(DiscordEvent.MessageReactionRemoved)]
	public static async Task ReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		// if (message.Author == client.CurrentUser)
		// 	return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		if (!settings.Value.acceptedEmoji.Contains(args.Emoji.ToString()) && !settings.Value.acceptedEmoji.Contains(args.Emoji.GetDiscordName()))
			return;
		if (args.Channel.IsNsfw && !settings.Value.allowNSFW)
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
			hook = await Util.GetWebhook(client, starboardChannel);

		// Get starboard message, if it already exists
		DiscordMessage? starboardMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, false);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await Util.CheckWebhookPerms(client, starboardChannel))))
			return;
		DiscordMessage? starboardJumpMessage = null;
		if (settings.Value.webhookJumpMessageLookup.ContainsKey(message.Id))
			starboardJumpMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, true);

		short reactions = await CountReactions(client, message, args.Channel);

		// Fell below minimum stars - remove
		if (reactions < settings.Value.minStars && settings.Value.removeWhenUnstarred) {
			await DeleteStarboardMessage(client, starboardMessage, starboardJumpMessage, message.Id, settings.Value, args.Guild, hook);
			return;
		}

		// Update star count
		if (settings.Value.useWebhook && hook != null)
			await UpdateStarboardJumpMessage(client, message, starboardJumpMessage, hook);
		else
			await UpdateStarboardMessage(client, message, starboardMessage, hook);
	}

	// [Event(DiscordEvent.MessageUpdated)]
	public static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		DiscordMessage message = await Util.VerifyMessage(args.Message, args.Channel);
		if (message.Author == client.CurrentUser)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		if (args.Channel.IsNsfw && !settings.Value.allowNSFW)
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
			hook = await Util.GetWebhook(client, starboardChannel);

		// Get starboard message, if it already exists
		DiscordMessage? starboardMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, false);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await Util.CheckWebhookPerms(client, starboardChannel))))
			return;
		DiscordMessage? starboardJumpMessage = null;
		if (settings.Value.webhookJumpMessageLookup.ContainsKey(message.Id))
			starboardJumpMessage = await GetStarboardMessage(client, message.Id, settings.Value, starboardChannel, hook, true);

		// Update message
		await UpdateStarboardMessage(client, message, starboardMessage, hook);
	}

	// [Event(DiscordEvent.MessageDeleted)]
	public static async Task MessageRemoved(DiscordClient client, MessageDeleteEventArgs args) {
		if (args.Channel.IsPrivate)
			return;
		StarboardSettings? settings = GetSettings(client, args.Guild);
		if (settings == null)
			return;
		if (args.Channel.IsNsfw && !settings.Value.allowNSFW)
			return;
		DiscordChannel? starboardChannel = GetStarboardChannel(client, args.Guild);
		if (starboardChannel == null)
			return;
		if (!settings.Value.messageLookup.ContainsKey(args.Message.Id))
			return;

		// Get webhook if possible
		DiscordWebhook? hook = null;
		if (settings.Value.useWebhook)
			hook = await Util.GetWebhook(client, starboardChannel);

		DiscordMessage? starboardMessage = await GetStarboardMessage(client, args.Message.Id, settings.Value, starboardChannel, hook, false);
		if (starboardMessage == null || (starboardMessage.WebhookMessage && (!settings.Value.useWebhook || !await Util.CheckWebhookPerms(client, starboardChannel))))
			return;
		DiscordMessage? starboardJumpMessage = null;
		if (settings.Value.webhookJumpMessageLookup.ContainsKey(args.Message.Id))
			starboardJumpMessage = await GetStarboardMessage(client, args.Message.Id, settings.Value, starboardChannel, hook, true);

		// Remove
		if (settings.Value.removeWhenDeleted)
			await DeleteStarboardMessage(client, starboardMessage, starboardJumpMessage, args.Message.Id, settings.Value, args.Guild, hook);
	}


	private static async Task UpdateStarboardMessage(DiscordClient client, DiscordMessage starredMessage, DiscordMessage starboardMessage, DiscordWebhook? hook) {
		// Webhook
		if (hook != null)
			await hook.EditMessageAsync(starboardMessage.Id, await CreateStarboardMessageWebhook(client, starredMessage, false));
		// No webhook
		else
			await starboardMessage.ModifyAsync(await CreateStarboardMessage(client, starredMessage, false));
	}

	private static async Task UpdateStarboardJumpMessage(DiscordClient client, DiscordMessage starredMessage, DiscordMessage starboardJumpMessage, DiscordWebhook hook) {
		await hook.EditMessageAsync(starboardJumpMessage.Id, await CreateStarboardJumpMessage(client, starredMessage, false));
	}

	private static async Task DeleteStarboardMessage(DiscordClient client, DiscordMessage starboardMessage, DiscordMessage starboardJumpMessage, ulong starredMessageID, StarboardSettings settings, DiscordGuild guild, DiscordWebhook? hook) {
		if (hook != null) {
			await hook.DeleteMessageAsync(starboardMessage.Id);
			await hook.DeleteMessageAsync(starboardJumpMessage.Id);
		} else
			await starboardMessage.DeleteAsync();
		settings.messageLookup.Remove(starredMessageID);
		ServerData? serverData = ServerData.GetServerData(client, guild);
		if (serverData == null)
			return;
		serverData.Save();
	}

	private static async Task<DiscordMessage?> GetStarboardMessage(DiscordClient client, ulong starredMessageID, StarboardSettings settings, DiscordChannel starboardChannel, DiscordWebhook? hook, bool jumpMessage) {
		DiscordMessage? starboardMessage = null;
		// Webhook
		if (hook != null)
			try {
				starboardMessage = await hook.GetMessageAsync((jumpMessage ? settings.webhookJumpMessageLookup : settings.messageLookup)[starredMessageID]);
			} catch {}
		// No webhook
		else
			try {
				starboardMessage = await Util.GetMessageFixed((jumpMessage ? settings.webhookJumpMessageLookup : settings.messageLookup)[starredMessageID], starboardChannel);
			} catch {}
		if (starboardMessage != null)
			return starboardMessage;
		// Remove missing starboard message from message lookup
		(jumpMessage ? settings.webhookJumpMessageLookup : settings.messageLookup).Remove(starredMessageID);
		ServerData? serverData = ServerData.GetServerData(client, starboardChannel.Guild);
		if (serverData == null)
			return null;
		serverData.Save();
		return null;
	}



	public static StarboardSettings? GetSettings(DiscordClient client, DiscordGuild guild) {
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

	private static async Task<short> CountReactions(DiscordClient client, string emoji, DiscordMessage message, DiscordChannel channel) {
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
		IReadOnlyList<DiscordUser> reacters = await message.GetReactionsAsync(Util.GetEmojiFromString(client, emoji));
		foreach (DiscordUser user in reacters)
			if (user != message.Author || settings.Value.allowSelfStar)
				count++;
		return count;
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

		// Keep track of reacting users and check on all enabled emojis
		HashSet<DiscordUser> reactingUsers = new HashSet<DiscordUser>();
		foreach (string emoji in settings.Value.acceptedEmoji) {
			IReadOnlyList<DiscordUser> reacters = await message.GetReactionsAsync(Util.GetEmojiFromString(client, emoji));
			foreach (DiscordUser user in reacters)
				if (user != message.Author || settings.Value.allowSelfStar)
					reactingUsers.Add(user);
		}
		return (short)reactingUsers.Count;
	}

	// Returns "attachments string" for embedding too-large attachments (>25 MB)
	private static async Task<(string, Dictionary<string, Stream>)> HandleAttachments(DiscordClient client, DiscordMessage originalMessage, bool downloadFiles, bool readContent) {
		string attachmentString = readContent ? originalMessage.Content : string.Empty;
		Dictionary<string, Stream> files = new Dictionary<string, Stream>();

		foreach (DiscordAttachment attachment in originalMessage.Attachments) {
			// Retrieve file
			if (attachment.FileSize.GetValueOrDefault(0) <= MAX_FILE_SIZE) {
				if (downloadFiles)
					files[attachment.Filename] = await client.RestClient.GetStreamAsync(attachment.Url);
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

		bool hasNonGuildSticker = message.Stickers.Count == 1 && (message.Stickers[0].GuildId != message.Channel.Guild.Id || message.Stickers[0].GuildId is null);

		(string, Dictionary<string, Stream>) attachmentData = await HandleAttachments(client, message, newMessage, false);
		string attachmentString = attachmentData.Item1;
		Dictionary<string, Stream> newAttachments = attachmentData.Item2;
		if (hasNonGuildSticker) {
			string toAdd = "\n" + message.Stickers[0].Url;
			if (attachmentString.Length + toAdd.Length > 2000 - 10)
				toAdd = "\n:warning:";
			attachmentString += toAdd;
		}

		DiscordMessageBuilder mb = new DiscordMessageBuilder()
			.WithContent(attachmentString)
			.KeepAttachments(true);
		foreach (DiscordEmbed e in message.Embeds)
			if (e.Url == null || !attachmentString.Contains(e.Url.OriginalString))
				mb.AddEmbed(e);
		if (newMessage)
			if (newAttachments.Count > 0)
				mb.WithFiles(newAttachments);
		if (message.Stickers.Count == 1 && !hasNonGuildSticker)
			mb.WithSticker(message.Stickers[0]);

		// Attempt to get author's display name in the server
		string name = message.Author.Username;
		string avatarURL = message.Author.AvatarUrl;
		DiscordMember member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, false);
		if (member == null)
			member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, true);
		if (member != null) {
			if (!string.IsNullOrEmpty(member.DisplayName))
				name = member.DisplayName;
			if (!string.IsNullOrEmpty(member.GuildAvatarUrl))
				avatarURL = member.GuildAvatarUrl;
		}

		return mb
			.WithEmbed(new DiscordEmbedBuilder()
				.WithColor(await Util.GetMemberColor(message.Author, message.Channel.Guild))
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(name + (message.Author.IsBot ? " [BOT]" : "") + " (⭐x" + reactions + ")", null, avatarURL)
				.WithDescription(message.Content)
				.WithTimestamp(message.Timestamp)
			);
	}

	private static async Task<DiscordWebhookBuilder> CreateStarboardMessageWebhook(DiscordClient client, DiscordMessage message, bool newMessage) {
		bool hasStickerAndFiles = message.Stickers.Count == 1 && message.Attachments.Count > 0;

		(string, Dictionary<string, Stream>) attachmentData = await HandleAttachments(client, message, newMessage, true);
		string attachmentString = attachmentData.Item1;
		Dictionary<string, Stream> newAttachments = attachmentData.Item2;
		if (hasStickerAndFiles) {
			string toAdd = "\n" + message.Stickers[0].Url;
			if (attachmentString.Length + toAdd.Length > 2000 - 10)
				toAdd = "\n:warning:";
			attachmentString += toAdd;
		}

		DiscordWebhookBuilder wb = new DiscordWebhookBuilder()
			.WithContent(attachmentString)
			.KeepAttachments(true);
		foreach (DiscordEmbed e in message.Embeds)
			if (e.Url == null || !attachmentString.Contains(e.Url.OriginalString))
				wb.AddEmbed(e);
		if (newMessage)
			if (newAttachments.Count > 0)
				wb.AddFiles(newAttachments);
		if (message.Stickers.Count == 1 && !hasStickerAndFiles)
			wb.AddFile(message.Stickers[0].Name, await client.RestClient.GetStreamAsync(message.Stickers[0].Url), false, message.Stickers[0].Description);
		if (newMessage) {
			// Attempt to get author's display name in the server
			string name = message.Author.Username;
			string avatarURL = message.Author.AvatarUrl;
			DiscordMember member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, false);
			if (member == null)
				member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, true);
			if (member != null) {
				if (!string.IsNullOrEmpty(member.DisplayName))
					name = member.DisplayName;
				if (!string.IsNullOrEmpty(member.GuildAvatarUrl))
					avatarURL = member.GuildAvatarUrl;
			}

			wb.Username = name;
			wb.AvatarUrl = avatarURL;
		}
		return wb;
	}

	private static async Task<DiscordWebhookBuilder> CreateStarboardJumpMessage(DiscordClient client, DiscordMessage message, bool newMessage) {
		// Attempt to get author's display name in the server
		string name = message.Author.Username;
		string avatarURL = message.Author.AvatarUrl;
		DiscordMember member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, false);
		if (member == null)
			member = await message.Channel.Guild.GetMemberAsync(message.Author.Id, true);
		if (member != null) {
			if (!string.IsNullOrEmpty(member.DisplayName))
				name = member.DisplayName;
			if (!string.IsNullOrEmpty(member.GuildAvatarUrl))
				avatarURL = member.GuildAvatarUrl;
		}

		// Emoji react string
		StarboardSettings? settings = GetSettings(client, message.Channel.Guild);
		string reactDescription = String.Empty;
		if (settings != null)
			foreach (string emoji in settings.Value.acceptedEmoji) {
				short reactions = await CountReactions(client, emoji, message, message.Channel);
				if (reactions > 0)
					reactDescription += "## " + emoji + " x" + reactions + "\n";
			}

		DiscordWebhookBuilder wb = new DiscordWebhookBuilder();
		if (newMessage) {
			wb.Username = name;
			wb.AvatarUrl = avatarURL;
		}
		return wb
			.AddEmbed(new DiscordEmbedBuilder()
				.WithColor(await Util.GetMemberColor(message.Author, message.Channel.Guild))
				.WithTitle("Jump to message")
				.WithUrl(message.JumpLink)
				.WithAuthor(name + (message.Author.IsBot ? " [BOT]" : ""), null, avatarURL)
				.WithDescription(reactDescription)
				.WithTimestamp(message.Timestamp)
			);
	}
}
