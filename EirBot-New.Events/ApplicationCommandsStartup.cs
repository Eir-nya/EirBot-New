using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using EirBot_New.AppCommands;
using System.Reflection;

namespace EirBot_New.Events;

public class ApplicationCommandsStartup {
	public static void Setup(DiscordShardedClient client, IReadOnlyDictionary<int, ApplicationCommandsExtension> commands) {
		#if DEBUG
		Console.WriteLine("DEBUG mode - registering with test server");
		#endif

		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
			if (t.IsSubclassOf(typeof(AppCommandGroupBase))) {
				#if !DEBUG
				commands.RegisterGlobalCommands(t);
				#else
				commands.RegisterGuildCommands(t, 294341563683831819); // My test server
				#endif
			}
	}
}
