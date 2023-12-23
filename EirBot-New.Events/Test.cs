using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

namespace EirBot_New.Events {
[EventHandler]
public class TestCommands {
	[Event(DiscordEvent.MessageCreated)]
	public async Task PingPong(DiscordClient client, MessageCreateEventArgs args) {
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
}

namespace EirBot_New.AppCommands {
public partial class TestCommands : ApplicationCommandsModule {
	[SlashCommand("Ping", "Sends back \"Pong.\"", true, false)]
	public async Task PingPong(InteractionContext context) {
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

	[SlashCommand("SayModal", "Modal test that sends a message as the bot somewhere.", true, false), ApplicationCommandRequireTeamOwner]
	public async Task SayModal(InteractionContext context) {
		DiscordInteractionModalBuilder mb = new DiscordInteractionModalBuilder()
			.WithTitle("Speak as " + context.Client.CurrentUser.Username)
			.WithCustomId("modal_say");
		mb.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Paragraph, "toSay", "Say...", context.Client.CurrentUser.Username + ": ", 1, 2000, true));

		await context.CreateModalResponseAsync(mb);
		InteractivityResult<ComponentInteractionCreateEventArgs> result = await context.Client.GetInteractivity().WaitForModalAsync(mb.CustomId);
		if (result.TimedOut) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("Modal timed out.")
			);
			return;
		}

		string toSay = string.Empty;
		foreach (DiscordComponentResult component in result.Result.Interaction.Data.Components)
			if (component.CustomId == "toSay") {
				toSay = component.Value;
				break;
			}

		if (string.IsNullOrEmpty(toSay)) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("No text entered.")
			);
			return;
		}

		await Util.CloseModal(result.Result.Interaction);

		await context.Channel.SendMessageAsync(new DiscordMessageBuilder()
			.WithContent(toSay)
		);
	}
}
}
