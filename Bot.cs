using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity.Extensions;

using EirBot_New.Events;
using EirBot_New.Serialization;

using System.Reflection;

namespace EirBot_New;

public class Bot : IDisposable {
	private readonly DiscordShardedClient client;
	public BotDataCollection savedData = new();

	public static Dictionary<DiscordClient, Bot> botInstances = new();

	public static Bot GetBot(DiscordClient cli) =>
		botInstances[cli];

	public Bot(DiscordShardedClient client) {
		this.client = client;
	}

	public async Task Init() {
		this.client.GuildDownloadCompleted += async (cli, e) => await Register(cli, this);
		await RegisterEvents(this.client);
		this.client.GuildDownloadCompleted += SetInitialStatus;
		await this.client.StartAsync();
		await Task.Delay(-1);
	}

	public void Dispose() {
		foreach (var disClient in this.client.ShardClients.Values)
			disClient.Dispose();
		GC.SuppressFinalize(this);
		Environment.Exit(0);
	}

	public async static Task Register(DiscordClient cli, Bot b) {
		await Task.Run(() => { botInstances[cli] = b; });
	}

	public async static Task SetInitialStatus(DiscordClient client, GuildDownloadCompletedEventArgs args) {
		await client.UpdateStatusAsync(new("with code", ActivityType.Playing));
	}

	public async static Task RegisterEvents(DiscordShardedClient client) {
		var commands = await client.UseApplicationCommandsAsync(new() {
			// DebugStartup = true,
			EnableDefaultHelp = false
		});
		// Register application commands
		ApplicationCommandsStartup.Setup(client, commands);

		HashSet<Task> tasks = new();
		foreach (var cli in client.ShardClients.Values)
			tasks.Add(Task.Run(() => {
				// Register event handlers
				cli.RegisterEventHandlers(Assembly.GetExecutingAssembly());
				// Register interactivity
				cli.UseInteractivity();
			}));
		await Task.WhenAll(tasks);

		/*
		// Execute all "Ready" events that haven't been executed yet
		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
			if (t.GetCustomAttribute(typeof(EventHandlerAttribute)) != null) {
				foreach (MethodInfo mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
					Attribute? a = mi.GetCustomAttribute<DisCatSharp.Enums.EventAttribute>();
					if (a != null)
						if (a.Match(new EventAttribute(DiscordEvent.Ready)))
							mi.Invoke(null, new object[] { client, args });
				}
			}
		}
		*/
	}
}