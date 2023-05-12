using DisCatSharp;
using DisCatSharp.Entities;

namespace EirBot_New;
public static class Util {
	public static async Task<DiscordGuild?> GetGuildAsync(DiscordClient client, ulong id, bool includeCount = false) {
		DiscordGuild? guild = await client.GetGuildAsync(id, includeCount, false);
		if (guild == null)
			guild = await client.GetGuildAsync(id, includeCount, true);
		if (guild == null)
			return null;
		return guild;
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
			} catch (DisCatSharp.Exceptions.NotFoundException e) {
				return DiscordColor.None;
			}
		}
		return DiscordColor.None;
	}
}
