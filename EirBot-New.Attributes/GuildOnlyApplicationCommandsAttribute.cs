namespace EirBot_New.Attributes;
public class GuildOnlyApplicationCommandsAttribute : Attribute {
	public ulong[] guildList;

	public GuildOnlyApplicationCommandsAttribute(params ulong[] guilds) : base() {
		guildList = guilds;
	}
}
