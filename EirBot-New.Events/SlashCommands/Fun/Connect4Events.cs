using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using EirBot_New.Attributes;

namespace EirBot_New.Events.Connect4;
[SlashCommandGroup("Fun", "Fun and games", true, false), EventHandler, GuildOnlyApplicationCommands]
public class Connect4Events : ApplicationCommandsModule {
	private static Dictionary<long, Connect4Game> games = new Dictionary<long, Connect4Game>();

	[SlashCommand("Connect4", "Play Connect Four with another user.", true, false)]
	public static async Task Connect4(InteractionContext context, [Option("Opponent", "Opponent to play against.\nThey will be yellow, you will be red.", false)] DiscordUser opponent) {
		// Create game
		DateTimeOffset timestamp = DateTimeOffset.Now;
		long id = timestamp.ToUnixTimeSeconds();
		Connect4Game game = new Connect4Game() { client = context.Client, gameID = id, timestamp = timestamp };
		game.SetPlayers(context.User, opponent);
		games[game.gameID] = game;

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithColor(DiscordColor.Blurple)
				.WithAuthor(context.User.Username, null, context.User.AvatarUrl)
				.WithDescription(String.Format("{0}, {1} has challenged you to a Connect Four match!\nDo you accept?", opponent.Mention, context.User.Username))
			)
			.AddComponents(new DiscordComponent[] {
				new DiscordButtonComponent(ButtonStyle.Success, "Connect4_start_" + id, "You're on!", false, new DiscordComponentEmoji(1087449113202806834)),
				new DiscordButtonComponent(ButtonStyle.Danger, "Connect4_refuse_" + id, "Pass", false, new DiscordComponentEmoji(865085988162371624))
				})
			);
	}

	[Event(DiscordEvent.ComponentInteractionCreated)]
	public static async Task AcceptDecline(DiscordClient client, ComponentInteractionCreateEventArgs args) {
		if (args.Message.Embeds.Count != 1)
			return;

		long id = 0;
		string[] splitParts = args.Id.Split("_", StringSplitOptions.None);
		if (splitParts.Length > 2)
			id = Convert.ToInt64(splitParts[2]);

		if (games.ContainsKey(id))
			if (args.Interaction.User != games[id].yellowPlayer)
				return;

		// Decline invite
		if (args.Id.StartsWith("Connect4_refuse")) {
			args.Handled = true;
			if (!games.ContainsKey(id))
				await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
					.WithContent("*(Game no longer active.)*")
					);
			else
				await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
					.WithContent("*Game refused.*")
					);
		// Accept invite
		} else if (args.Id.StartsWith("Connect4_start")) {
			args.Handled = true;
			if (!games.ContainsKey(id))
				await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
					.WithContent("*(Game no longer active.)*")
					);
			else {
				Connect4Game game = games[id];
				game.message = args.Message;
				game.Init();

				await args.Message.ModifyAsync(game.Display());
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":one:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":two:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":three:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":four:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":five:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":six:"));
				await args.Message.CreateReactionAsync(DiscordEmoji.FromName(client, ":seven:"));
			}
		}
	}

	[Event(DiscordEvent.MessageReactionAdded)]
	public static async Task UpdateGame(DiscordClient client, MessageReactionAddEventArgs args) {
		if (args.User == client.CurrentUser)
			return;
		if (args.Message.Embeds.Count < 1)
			return;
		if (args.Message.Embeds[0].Timestamp == null)
			return;
		if (!games.ContainsKey(args.Message.Embeds[0].Timestamp.Value.ToUnixTimeSeconds()))
			return;

		Connect4Game game = games[args.Message.Embeds[0].Timestamp.Value.ToUnixTimeSeconds()];
		if (game.gameOver)
			return;

		if ((game.nextTurnYellow && args.User == game.yellowPlayer) || args.User == game.redPlayer) {
			int column = game.ParseColumn(args.Emoji);
			if (column > -1 && game.ColumnFree(column)) {
				game.Place(column);
				await args.Message.ModifyAsync(game.Display());
				if (game.gameOver)
					new Thread(async () => await RemoveOwnReactionsAsync(client, args.Message)).Start();
			}
		}

		await args.Message.DeleteReactionAsync(args.Emoji, args.User);
	}

	private static async Task RemoveOwnReactionsAsync(DiscordClient client, DiscordMessage msg) {
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":one:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":two:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":three:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":four:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":five:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":six:"));
		await msg.DeleteOwnReactionAsync(DiscordEmoji.FromName(client, ":seven:"));
	}
}
