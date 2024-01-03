using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using EirBot_New.Attributes;

namespace EirBot_New.Events;

// [EventHandler]
public class TestCommands {
	// Run on bot ready
	[RunOnStartup]
	private static void RunOnStartup(DiscordShardedClient client) {
		client.MessageCreated += PingPong;
	}

	// [Event(DiscordEvent.MessageCreated)]
	public static async Task PingPong(DiscordClient client, MessageCreateEventArgs args) {
		if (args.Message.Content.ToLower().StartsWith("!ping")) {
			await new DiscordMessageBuilder()
				.WithReply(args.Message.Id, true)
				.WithEmbed(new DiscordEmbedBuilder()
					.WithTitle("!ping")
					.WithAuthor(args.Message.Author.Username, null, args.Message.Author.AvatarUrl)
					.WithDescription("Pong!")
					.WithTimestamp(args.Message.Timestamp)
					.WithColor(DiscordColor.Blurple)
					)
				.SendAsync(args.Channel);
		}
	}
}
