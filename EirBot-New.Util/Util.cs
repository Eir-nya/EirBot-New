using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Net;

namespace EirBot_New;

public static class Util {
	public const string WEBHOOK_DEFAULT_NAME = "{0} - General purpose webhook";

	public async static Task<DiscordMessage> VerifyMessage(DiscordMessage message, DiscordChannel channel) {
		if (message.Author != null)
			return message;

		return await channel.GetMessageAsync(message.Id, true);
	}

	public async static Task<DiscordMessage> GetMessageFixed(ulong id, DiscordChannel channel) =>
		await VerifyMessage(await channel.GetMessageAsync(id, false), channel);

	public async static Task<DiscordGuild?> GetGuildAsync(DiscordClient client, ulong id, bool includeCount = false) {
		var guild = await client.GetGuildAsync(id, includeCount, false);
		if (guild == null)
			guild = await client.GetGuildAsync(id, includeCount, true);
		if (guild == null)
			return null;

		return guild;
	}

	public async static Task<DiscordColor> GetMemberColor(DiscordClient client, DiscordUser user, ulong guildID) {
		var guild = await GetGuildAsync(client, guildID);
		if (guild != null)
			return await GetMemberColor(user, guild);

		return DiscordColor.None;
	}

	public async static Task<DiscordColor> GetMemberColor(DiscordUser user, DiscordGuild guild) {
		if (guild != null)
			try {
				var member = await guild.GetMemberAsync(user.Id, false);
				if (member == null)
					member = await guild.GetMemberAsync(user.Id, true);
				if (member != null)
					return member.Color;
				// Member not found
			} catch (DisCatSharp.Exceptions.NotFoundException) {
				return DiscordColor.None;
			}

		return DiscordColor.None;
	}

	public static Stream GetAvatar(string avatarURL) =>
		new MemoryStream(new WebClient().DownloadData(new Uri(avatarURL)));

	public async static Task<bool> CheckWebhookPerms(DiscordClient client, DiscordChannel channel) {
		var botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, false);
		if (botMember == null)
			botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, true);
		if (botMember == null)
			return false;

		return channel.PermissionsFor(botMember).HasFlag(Permissions.ManageWebhooks);
	}

	public async static Task<DiscordWebhook?> GetWebhook(DiscordClient client, DiscordChannel channel) {
		if (!await CheckWebhookPerms(client, channel))
			return null;

		var hooks = await channel.Guild.GetWebhooksAsync();
		foreach (var hook in hooks)
			if (hook.User == client.CurrentUser)
				return hook;

		return null;
	}

	public async static Task ModifyWebhookAsync(DiscordWebhook hook, string? name = null, Stream? avatar = null, ulong? channelID = null) {
		if (name == null)
			name = hook.Name;
		if (avatar == null)
			avatar = GetAvatar(hook.AvatarUrl);
		if (channelID == null)
			channelID = hook.ChannelId;
		await hook.ModifyAsync(name, avatar, channelID);
	}

	public async static Task<DiscordWebhook?> GetOrCreateWebhook(DiscordClient client, DiscordChannel channel) {
		if (!await CheckWebhookPerms(client, channel))
			return null;

		var hook = await GetWebhook(client, channel);
		// Create webhook
		if (hook == null)
			hook = await channel.CreateWebhookAsync(string.Format(WEBHOOK_DEFAULT_NAME, client.CurrentUser.Username), GetAvatar(client.CurrentUser.AvatarUrl), "General purpose webhook");
		return hook;
	}

	// This closes the modal without needing to send a message.
	public async static Task CloseModal(DiscordInteraction modalInteraction) {
		try {
			await modalInteraction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("_ _"));
		} catch { }
	}

	public static string GetUniversalEmojiString(DiscordEmoji emoji) =>
		"<:" + emoji.Name + ":" + emoji.Id + ">";
}