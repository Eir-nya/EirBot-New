using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using EirBot_New.Attributes;
using System.Reflection;

namespace EirBot_New.Events;

public class ApplicationCommandsStartup {
	public static async Task Setup(DiscordShardedClient client, IReadOnlyDictionary<int, ApplicationCommandsExtension> commands) {
		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			if (t.IsSubclassOf(typeof(ApplicationCommandsModule))) {
				GuildOnlyApplicationCommandsAttribute guildOnlyAttr = (GuildOnlyApplicationCommandsAttribute)t.GetCustomAttribute(typeof(GuildOnlyApplicationCommandsAttribute));
				if (guildOnlyAttr != null) {
					foreach (ulong id in guildOnlyAttr.guildList)
						commands.RegisterGuildCommands(t, id);
				} else
					commands.RegisterGlobalCommands(t);
			}
	}
}
