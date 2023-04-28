using DisCatSharp;
using DisCatSharp.EventArgs;
using DisCatSharp.ApplicationCommands;
using System.Reflection;

namespace EirBot_New.Events;
[EventHandler]
public class SlashCommandsStartup {
	[Event(DiscordEvent.Ready)]
	public static async Task SetupCommands(DiscordClient client, ReadyEventArgs args) {
		ApplicationCommandsExtension commands = client.UseApplicationCommands();

		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			if (t.IsSubclassOf(typeof(ApplicationCommandsModule)))
				commands.RegisterGlobalCommands(t);
	}
}
