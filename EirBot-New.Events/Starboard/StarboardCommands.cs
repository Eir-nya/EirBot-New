using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using EirBot_New.Attributes;
using EirBot_New.Serialization;

namespace EirBot_New.Events.Starboard;
[SlashCommandGroup("Starboard", "Starboard settings.", false, false), EventHandler, GuildOnlyApplicationCommands]
public class Connect4Events : ApplicationCommandsModule {
	[SlashCommand("minStars", "Set minimum stars for adding to starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task MinStars(InteractionContext context, [Option("minimum-stars", "Minimum star amount to add a message to starboard.", false), MinimumValue(1), MaximumValue(25)] int minStars) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		serverData.starboardSettings.minStars = (short)minStars;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Set minimum star count to **" + minStars + "**.")
		);
	}

	[SlashCommand("allowNSFW", "Allow messages from NSFW channels in the starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task AllowNSFW(InteractionContext context, [Option("allow-NSFW", "Whether messages from NSFW channels should be posted in the starboard.", false)] bool allowNSFW) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		serverData.starboardSettings.allowNSFW = allowNSFW;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Starboard **" + (allowNSFW ? "will" : "will not") + "** track messages from NSFW channels.")
		);
	}

	[SlashCommand("allowSelfStar", "Count star reactions from the message sender.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task AllowSelfStar(InteractionContext context, [Option("allow-self-star", "Whether the message sender's own star reaction counts towards the starboard.", false)] bool allowSelfStar) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		serverData.starboardSettings.allowSelfStar = allowSelfStar;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Starboard **" + (allowSelfStar ? "will" : "will not") + "** count own star reactions.")
		);
	}

	[SlashCommand("removeWhenUnstarred", "Remove messages from the starboard when they fall below the minimum.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task RemoveWhenUnstarred(InteractionContext context, [Option("remove-when-unstarred", "Whether starboard messages should be removed when star count falls below the minimum star count.", false)] bool removeWhenUnstarred) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		serverData.starboardSettings.removeWhenUnstarred = removeWhenUnstarred;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Starboard **" + (removeWhenUnstarred ? "will" : "will not") + "** remove messages that fall below the minimum star count.")
		);
	}

	[SlashCommand("channel", "Set starboard channel.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task Channel(InteractionContext context, [Option("channel", "Channel to post starboard messages in.", false), ChannelTypes(ChannelType.Text)] DiscordChannel channel) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		Permissions botPermsInChannel = channel.PermissionsFor(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id));
		if (!botPermsInChannel.HasFlag(Permissions.SendMessages)) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("Cannot post messages in channel " + channel.Mention + ". Cancelled.")
			);
			return;
		}
		serverData.starboardSettings.channelID = channel.Id;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Starboard channel set to " + channel.Mention + ".")
		);
	}

	[SlashCommand("disable", "Disables starboard.", false, false), ApplicationCommandRequireUserPermissions(Permissions.ManageChannels)]
	public static async Task Disable(InteractionContext context) {
		ServerData? serverData = ServerData.GetServerData(context.Client, context.Guild);
		if (serverData == null)
			return;
		serverData.starboardSettings.channelID = 0;
		serverData.Save();

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("Starboard disabled.")
		);
	}
}
