using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

namespace EirBot_New.Events;
[EventHandler]
public class Test {
	[Event(DiscordEvent.MessageCreated)]
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

[SlashCommandGroup("Test", "Test commands.", true, false)]
public class TestSlash : ApplicationCommandsModule {
	[SlashCommand("Ping", "Sends back \"Pong.\"", true, false)]
	public static async Task PingPong(InteractionContext context) {
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithTitle("!ping")
				.WithAuthor(context.User.Username, null, context.User.AvatarUrl)
				.WithDescription("Pong!")
				.WithTimestamp(context.Interaction.CreationTimestamp)
				.WithColor(DiscordColor.Blurple)
				)
		);
	}
}
