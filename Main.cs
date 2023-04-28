using DisCatSharp;

namespace EirBot_New;
public class Program {
	public static void Main(string[] args) {
		MainAsync().GetAwaiter().GetResult();
	}

	public static async Task MainAsync() {
		DiscordShardedClient client = new DiscordShardedClient(new DiscordConfiguration() {
			Token = File.ReadAllText("token.txt"),
			TokenType = TokenType.Bot,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
			MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
		});
		Bot bot = new Bot(client);
		await bot.Init();
	}
}
