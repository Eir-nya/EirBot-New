using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.ApplicationCommands;
using EirBot_New.Attributes;
using System.Reflection;

namespace EirBot_New.Events;
[EventHandler]
public class SlashCommandsStartup {
	[Event(DiscordEvent.Ready)]
	public static async Task SetupCommands(DiscordClient client, ReadyEventArgs args) {
		ApplicationCommandsExtension commands = client.UseApplicationCommands();

		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			if (t.IsSubclassOf(typeof(ApplicationCommandsModule))) {
				if (t.GetCustomAttribute(typeof(GuildOnlyApplicationCommandsAttribute)) != null) {
					foreach (DiscordGuild g in client.Guilds.Values)
						commands.RegisterGuildCommands(t, g.Id);
				} else
					commands.RegisterGlobalCommands(t);
			}
	}
}
