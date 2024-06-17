using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using System.Net;

namespace EirBot_New;
public static class Util {
	public const string WEBHOOK_DEFAULT_NAME = "{0} - General purpose webhook";

	public static async Task<DiscordMessage> VerifyMessage(DiscordMessage message, DiscordChannel channel) {
		if (message.Author != null)
			return message;
		return await channel.GetMessageAsync(message.Id, true);
	}

	public static async Task<DiscordMessage> GetMessageFixed(ulong id, DiscordChannel channel) {
		return await VerifyMessage(await channel.GetMessageAsync(id, false), channel);
	}

	public static async Task<DiscordGuild?> GetGuildAsync(DiscordClient client, ulong id, bool includeCount = false) {
		DiscordGuild? guild = await client.GetGuildAsync(id, includeCount, false);
		if (guild == null)
			guild = await client.GetGuildAsync(id, includeCount, true);
		if (guild == null)
			return null;
		return guild;
	}

	public static DiscordEmoji? GetEmojiFromString(DiscordClient client, string emojiString) {
		DiscordEmoji? newEmoji = null;
		bool created = false;
		try {
			created = DiscordEmoji.TryFromName(client, emojiString, out newEmoji);
		} catch {}
		if (!created)
			created = DiscordEmoji.TryFromUnicode(client, emojiString, out newEmoji);
		if (!created)
			if (emojiString.Length > 1 && emojiString.Substring(0, 2) == "<:" && emojiString.Substring(emojiString.Length - 1) == ">") {
				int idStartsAt = emojiString.IndexOf(":", 2) + 1;
				created = DiscordEmoji.TryFromGuildEmote(client, Convert.ToUInt64(emojiString.Substring(idStartsAt, emojiString.Length - idStartsAt - 1)), out newEmoji);
			}
		return newEmoji;
	}

	public static string GetUniversalEmojiString(DiscordEmoji emoji) {
		return "<:" + emoji.Name + ":" + emoji.Id + ">";
	}

	public static async Task<DiscordColor> GetMemberColor(DiscordClient client, DiscordUser user, ulong guildID) {
		DiscordGuild? guild = await GetGuildAsync(client, guildID);
		if (guild != null)
			return await GetMemberColor(user, guild);
		return DiscordColor.None;
	}
	public static async Task<DiscordColor> GetMemberColor(DiscordUser user, DiscordGuild guild) {
		if (guild != null) {
			try {
				DiscordMember member = await guild.GetMemberAsync(user.Id, false);
				if (member == null)
					member = await guild.GetMemberAsync(user.Id, true);
				if (member != null)
					return member.Color;
			// Member not found
			} catch (DisCatSharp.Exceptions.NotFoundException) {
				return DiscordColor.None;
			}
		}
		return DiscordColor.None;
	}
	public static Stream GetAvatar(string avatarURL) {
		return new MemoryStream(new WebClient().DownloadData(new Uri(avatarURL)));
	}

	public static async Task<bool> CheckWebhookPerms(DiscordClient client, DiscordChannel channel) {
		DiscordMember botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, false);
		if (botMember == null)
			botMember = await channel.Guild.GetMemberAsync(client.CurrentUser.Id, true);
		if (botMember == null)
			return false;
		return channel.PermissionsFor(botMember).HasFlag(Permissions.ManageWebhooks);
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
	public static async Task ModifyWebhookAsync(DiscordWebhook hook, string? name = null, Stream? avatar = null, ulong? channelID = null) {
		if (name == null)
			name = hook.Name;
		if (avatar == null)
			avatar = GetAvatar(hook.AvatarUrl);
		if (channelID == null)
			channelID = hook.ChannelId;
		await hook.ModifyAsync(name, avatar, channelID);
	}
	public static async Task<DiscordWebhook?> GetOrCreateWebhook(DiscordClient client, DiscordChannel channel) {
		if (!await CheckWebhookPerms(client, channel))
			return null;
		DiscordWebhook? hook = await GetWebhook(client, channel);
		// Create webhook
		if (hook == null)
			hook = await channel.CreateWebhookAsync(string.Format(WEBHOOK_DEFAULT_NAME, client.CurrentUser.Username), GetAvatar(client.CurrentUser.AvatarUrl), "General purpose webhook");
		return hook;
	}

	// This closes the modal without needing to send a message.
	public static async Task CloseModal(DiscordInteraction modalInteraction) {
		try {
			await modalInteraction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("_ _"));
		} catch {}
	}
}
