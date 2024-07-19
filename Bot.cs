using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity.Extensions;
using EirBot_New.Attributes;
using EirBot_New.Events;
using EirBot_New.Serialization;
using System.Reflection;

namespace EirBot_New;
public class Bot : IDisposable {
	private readonly DiscordShardedClient client;
	public BotDataCollection savedData = new BotDataCollection();

	public static Dictionary<DiscordClient, Bot> botInstances = new Dictionary<DiscordClient, Bot>();
	public static Bot GetBot(DiscordClient cli) { return botInstances[cli]; }

	private Queue<Func<Task>> taskQueue = new Queue<Func<Task>>();
	private Task queueTask;

	public Bot(DiscordShardedClient client) {
		this.client = client;
	}

	public async Task Init() {
		client.GuildDownloadCompleted += async (cli, e) => await Register(cli, this);
		await RegisterEvents(client);
		client.GuildDownloadCompleted += SetInitialStatus;

		bool connected = false;
		client.Zombied += async (DiscordClient client, ZombiedEventArgs args) => { connected = false; };

		while (true) {
			try {
				queueTask = Task.Run(DoTaskQueue);
				await client.StartAsync();
				connected = true;
			// Couldn't connect to Discord.
			} catch (System.TimeoutException) {}
			do
				await Task.Delay(60 * 1000);
			while (connected);

			queueTask.Dispose();
			taskQueue.Clear();
			await client.StopAsync();
		}
	}

	public void AddTask(Func<Task> f) {
		taskQueue.Enqueue(f);
	}

	private async Task DoTaskQueue() {
		while (true) {
			if (taskQueue.Count == 0)
				await Task.Delay(1000);
			else
				await taskQueue.Dequeue()();
		}
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

	public static async Task SetInitialStatus(DiscordClient client, GuildDownloadCompletedEventArgs args) {
		await client.UpdateStatusAsync(new DiscordActivity("with code", ActivityType.Playing));
	}

	public async Task RegisterEvents(DiscordShardedClient client) {
		IReadOnlyDictionary<int, ApplicationCommandsExtension> commands = await client.UseApplicationCommandsAsync(new ApplicationCommandsConfiguration() {
			// DebugStartup = true,
			EnableDefaultHelp = false
		});
		// Register application commands
		ApplicationCommandsStartup.Setup(client, commands);

		// Execute all "RunOnStartup" methods
		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
			foreach (MethodInfo mi in t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)) {
				if (mi.GetCustomAttribute(typeof(RunOnStartupAttribute)) != null)
					mi.Invoke(null, new object[] { client, this });
			}
		}

		// Register interactivity
		await client.UseInteractivityAsync();

		HashSet<Task> tasks = new();
		foreach (DiscordClient cli in client.ShardClients.Values) {
			tasks.Add(Task.Run(() => {
				// Register event handlers
				cli.RegisterEventHandlers(Assembly.GetExecutingAssembly());
			}));
		}
		await Task.WhenAll(tasks);
	}
}
