using DisCatSharp;
using DisCatSharp.ApplicationCommands;
// using EirBot_New.Attributes;
using System.Reflection;

namespace EirBot_New.Events;

public class ApplicationCommandsStartup {
	public static void Setup(DiscordShardedClient client, IReadOnlyDictionary<int, ApplicationCommandsExtension> commands) {
		#if DEBUG
		Console.WriteLine("DEBUG mode - registering with test server");
		#endif

		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			if (t.IsSubclassOf(typeof(ApplicationCommandsModule))) {
				#if !DEBUG
				/*
				GuildOnlyApplicationCommandsAttribute guildOnlyAttr = (GuildOnlyApplicationCommandsAttribute)t.GetCustomAttribute(typeof(GuildOnlyApplicationCommandsAttribute));
				if (guildOnlyAttr != null) {
					foreach (ulong id in guildOnlyAttr.guildList)
						commands.RegisterGuildCommands(t, id);
				} else
				*/
				commands.RegisterGlobalCommands(t);
				#else
				commands.RegisterGuildCommands(t, 294341563683831819); // My test erver
				#endif
			}
	}
}
