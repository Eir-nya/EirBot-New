using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
// using DisCatSharp.Interactivity.Extensions;
using System.Reflection;

namespace EirBot_New;
public class Bot : IDisposable {
	private readonly DiscordShardedClient client;

	public Bot(DiscordShardedClient client) {
		this.client = client;
	}

	public async Task Init() {
		client.Ready += RegisterEvents;
		client.Ready += SetInitialStatus;
		await client.StartAsync();
		await Task.Delay(-1);
	}

	public void Dispose() {
		foreach (DiscordClient disClient in client.ShardClients.Values)
			disClient.Dispose();
		GC.SuppressFinalize(this);
		Environment.Exit(0);
	}

	public static async Task SetInitialStatus(DiscordClient client, ReadyEventArgs args) {
		await client.UpdateStatusAsync(new DiscordActivity("with code", ActivityType.Playing));
	}

	public static async Task RegisterEvents(DiscordClient client, ReadyEventArgs args) {
		// Register event handlers
		client.RegisterEventHandlers(Assembly.GetExecutingAssembly());
		// // Register interactivity
		// client.UseInteractivity();

		// Execute all "Ready" events that haven't been executed yet
		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
			if (t.GetCustomAttribute(typeof(EventHandlerAttribute)) != null) {
				foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
					Attribute? a = mi.GetCustomAttribute<DisCatSharp.EventAttribute>();
					if (a != null)
						if (a.Match(new EventAttribute(DiscordEvent.Ready)))
							mi.Invoke(null, new object[] { client, args });
				}
			}
		}
	}
}
