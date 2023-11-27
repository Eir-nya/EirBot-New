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
	public BotDataCollection savedData = new BotDataCollection();

	public static Dictionary<DiscordClient, Bot> botInstances = new Dictionary<DiscordClient, Bot>();
	public static Bot GetBot(DiscordClient cli) { return botInstances[cli]; }

	public Bot(DiscordShardedClient client) {
		this.client = client;
	}

	public async Task Init() {
		client.Ready += async (cli, e) => await Register(cli, this);
		await RegisterEvents(client);
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

	public static async Task Register(DiscordClient cli, Bot b) {
		await Task.Run(() => {
			botInstances[cli] = b;
		});
	}

	public static async Task SetInitialStatus(DiscordClient client, ReadyEventArgs args) {
		await client.UpdateStatusAsync(new DiscordActivity("with code", ActivityType.Playing));
	}

	public static async Task RegisterEvents(DiscordShardedClient client) {
		IReadOnlyDictionary<int, ApplicationCommandsExtension> commands = await client.UseApplicationCommandsAsync(new ApplicationCommandsConfiguration() {
			// DebugStartup = true,
			EnableDefaultHelp = false
		});
		// Register application commands
		await ApplicationCommandsStartup.Setup(client, commands);

		HashSet<Task> tasks = new HashSet<Task>();
		foreach (DiscordClient cli in client.ShardClients.Values) {
			tasks.Add(Task.Run(async () => {
				// Register event handlers
				cli.RegisterEventHandlers(Assembly.GetExecutingAssembly());
				// Register interactivity
				cli.UseInteractivity();
			}));
		}
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
