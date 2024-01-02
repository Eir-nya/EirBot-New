using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

using EirBot_New.Events.Connect4;

namespace EirBot_New.AppCommands;

public partial class ContextMenuCommands : AppCommandGroupBase {
	[ContextMenu(ApplicationCommandType.User, "Connect4"), ApplicationCommandRequireGuild]
	public async static Task Challenge(ContextMenuContext context) {
		if (context.TargetUser.IsBot) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("Cannot invite bot users.")
			);
			return;
		}

		var rb = await Connect4Events.Challenge(context.Client, context.User, context.TargetUser, context.Guild);
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, rb);
	}
}

public partial class FunCommands : AppCommandGroupBase {
	[SlashCommand("connect4", "Play Connect Four with another user.", true, false), ApplicationCommandRequireGuild]
	public async static Task Connect(InteractionContext context, [Option("Opponent", "Opponent to play against.\nThey will be yellow, you will be red.", false)] DiscordUser opponent) {
		if (opponent.IsBot) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("Cannot invite bot users.")
			);
			return;
		}

		var rb = await Connect4Events.Challenge(context.Client, context.User, opponent, context.Guild);
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, rb);
	}
}