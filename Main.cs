using DisCatSharp;
using DisCatSharp.Enums;

namespace EirBot_New;

public class Program {
	public static void Main(string[] args) {
		MainAsync().GetAwaiter().GetResult();
	}

	public async static Task MainAsync() {
		var client = new DiscordShardedClient(new() {
			Token = File.ReadAllText("token.txt"),
			TokenType = TokenType.Bot,
			Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContent,
			MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
		});
		var bot = new Bot(client);
		await bot.Init();
	}
}