using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using EirBot_New.Serialization;

namespace EirBot_New.AppCommands;
using EirBot_New.Events.Starboard;
public partial class StarboardCommands : AppCommandGroupBase {
	[SlashCommand("List", "Lists all config.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task List(InteractionContext context) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		StarboardSettings? settings = StarboardEvents.GetSettings(context.Client, context.Guild);
		if (settings == null) {
			await context.DeleteResponseAsync();
			return;
		}

		DiscordChannel? starboardChannel = context.Guild.GetChannel(settings.Value.channelID);
		List<string> ignoredChannelNames = new List<string>();
		foreach (ulong id in settings.Value.ignoredChannels)
			ignoredChannelNames.Add(context.Guild.GetChannel(id).Name);

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithTitle("Starboard settings")
				.WithAuthor(context.Guild.Name, null, context.Guild.IconUrl)
				.WithDescription(
					":gear: Enabled: " + ((starboardChannel != null && !string.IsNullOrEmpty(starboardChannel.Name)) ? ":white_check_mark:" : ":x:") + "\n" +
					":hash: Starboard channel: " + (starboardChannel != null ? starboardChannel.Mention : ":x:") + "\n" +
					":no_bell: Ignored channels: " + (settings.Value.ignoredChannels.Count > 0 ? string.Join(", ", ignoredChannelNames) : ":x:") + "\n" +
					":star: Emojis to check for: " + string.Join(", ", settings.Value.acceptedEmoji) + "\n" +
					":1234: Minimum reacts: " + settings.Value.minStars + "\n" +
					":underage: Allow posts from NSFW channels: " + (settings.Value.allowNSFW ? ":white_check_mark:" : ":x:") + "\n" +
					":index_pointing_at_the_viewer: Count stars from message author: " + (settings.Value.allowSelfStar ? ":white_check_mark:" : ":x:") + "\n" +
					":hammer: Remove on message delete: " + (settings.Value.removeWhenDeleted ? ":white_check_mark:" : ":x:") + "\n" +
					":dizzy: Remove when star count falls below minimum stars: " + (settings.Value.removeWhenUnstarred ? ":white_check_mark:" : ":x:") + "\n" +
					":robot: Use webhook for starboard messages: " + (settings.Value.useWebhook ? ":white_check_mark:" : ":x:")
				)
			)
		);
	}

	[SlashCommand("minStars", "Set minimum stars for adding to starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task MinStars(InteractionContext context, [Option("minimum-stars", "Minimum star amount to add a message to starboard.", false), MinimumValue(1), MaximumValue(25)] int minStars) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.minStars = (short)minStars;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Set minimum star count to **" + minStars + "**.")
		);
	}

	[SlashCommand("allowNSFW", "Allow messages from NSFW channels in the starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task AllowNSFW(InteractionContext context, [Option("allow-NSFW", "Whether messages from NSFW channels should be posted in the starboard.", false)] bool allowNSFW) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.allowNSFW = allowNSFW;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard **" + (allowNSFW ? "will" : "will not") + "** track messages from NSFW channels.")
		);
	}

	[SlashCommand("allowSelfStar", "Count star reactions from the message sender.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task AllowSelfStar(InteractionContext context, [Option("allow-self-star", "Whether the message sender's own star reaction counts towards the starboard.", false)] bool allowSelfStar) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.allowSelfStar = allowSelfStar;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard **" + (allowSelfStar ? "will" : "will not") + "** count own star reactions.")
		);
	}

	[SlashCommand("removeWhenUnstarred", "Remove messages from the starboard when they fall below the minimum.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task RemoveWhenUnstarred(InteractionContext context, [Option("remove-when-unstarred", "Whether starboard messages should be removed when star count falls below the minimum star count.", false)] bool removeWhenUnstarred) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.removeWhenUnstarred = removeWhenUnstarred;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard **" + (removeWhenUnstarred ? "will" : "will not") + "** remove messages that fall below the minimum star count.")
		);
	}

	[SlashCommand("removeWhenDeleted", "Remove messages from the starboard when the original messages are deleted.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task RemoveWhenDeleted(InteractionContext context, [Option("remove-when-deleted", "Whether starboard messages should be removed when original message is deleted.", false)] bool removeWhenDeleted) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.removeWhenDeleted = removeWhenDeleted;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard **" + (removeWhenDeleted ? "will" : "will not") + "** remove messages after original messages are deleted.")
		);
	}

	[SlashCommand("useWebhook", "Use a webhook when posting to starboard (requires \"Manage Webhooks\" perm)", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageWebhooks), ApplicationCommandRequireGuild]
	public static async Task UseWebhook(InteractionContext context, [Option("use-webhook", "Whether a webhook should be used when posting to the starboard", false)] bool useWebhook) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.useWebhook = useWebhook;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard **" + (useWebhook ? "will" : "will not") + "** use a webhook when posting.")
		);
	}

	[SlashCommand("channel", "Set starboard channel.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task Channel(InteractionContext context, [Option("channel", "Channel to post starboard messages in.", false), ChannelTypes(ChannelType.Text)] DiscordChannel channel) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		Permissions botPermsInChannel = channel.PermissionsFor(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id));
		if (!botPermsInChannel.HasFlag(Permissions.SendMessages)) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.WithContent("Cannot post messages in channel " + channel.Mention + ". Cancelled.")
			);
			return;
		}
		serverData.starboardSettings.channelID = channel.Id;
		serverData.Save();

		// Set webhook channel to new channel if possible
		DiscordWebhook? hook = await Util.GetWebhook(context.Client, channel);
		if (hook != null)
			await Util.ModifyWebhookAsync(hook, null, null, channel.Id);

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard channel set to " + channel.Mention + ".")
		);
	}

	[SlashCommand("ignorechannel", "Ignore channels for starboard tracking.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task IgnoreChannel(InteractionContext context, [Option("channel", "Channel to change ignore status of.", false), ChannelTypes(ChannelType.Text)] DiscordChannel channel, [Option("ignore", "Whether to ignore this channel in starboard operations.", false)] bool ignore = true) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		if (ignore) {
			if (!serverData.starboardSettings.ignoredChannels.Contains(channel.Id))
				serverData.starboardSettings.ignoredChannels.Add(channel.Id);
		} else
			if (serverData.starboardSettings.ignoredChannels.Contains(channel.Id))
				serverData.starboardSettings.ignoredChannels.Remove(channel.Id);
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Channel " + channel.Mention + " is now **" + (ignore ? "ignored" : "not ignored") + "** for starboard.")
		);
	}

	[SlashCommand("disable", "Disables starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task Disable(InteractionContext context) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}
		serverData.starboardSettings.channelID = 0;
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Starboard disabled.")
		);
	}

	[SlashCommand("setemoji", "Sets allowed emoji for starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels), ApplicationCommandRequireGuild]
	public static async Task SetEmoji(InteractionContext context, [Option("emojistring", "List of emojis to use.", false)] string message) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null) {
			await context.DeleteResponseAsync();
			return;
		}

		// Parse incoming message for emojis
		HashSet<DiscordEmoji> emojis = new HashSet<DiscordEmoji>();
		string[] words = message.Split(" ");
		foreach (string word in words) {
			DiscordEmoji? newEmoji = Util.GetEmojiFromString(context.Client, word);
			if (newEmoji != null)
				emojis.Add(newEmoji);
		}

		// Require at least one emoji
		if (emojis.Count == 0) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.WithContent("At least one emoji is required. To reset, simply enter `:star:`.")
			);
			return;
		}

		// Try set emojis
		serverData.starboardSettings.acceptedEmoji = new HashSet<string>();
		foreach (DiscordEmoji emoji in emojis) {
			if (emoji.ToString().Length != 1)
				serverData.starboardSettings.acceptedEmoji.Add(emoji.ToString());
			else
				serverData.starboardSettings.acceptedEmoji.Add(emoji.GetDiscordName());
		}
		serverData.Save();

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.WithContent("Emojis updated.\n" + string.Join(", ", serverData.starboardSettings.acceptedEmoji))
		);
	}
}
