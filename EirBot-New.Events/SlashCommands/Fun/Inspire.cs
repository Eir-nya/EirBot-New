using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace EirBot_New.Events;
[SlashCommandGroup("Fun", "Fun and games", true, false), EventHandler]
public class InspireCommand : ApplicationCommandsModule {
	private const string INSPIROBOT_URL = "http://inspirobot.me/";
	private const string INSPIROBOT_API_URL = "http://inspirobot.me/api?generate=true";
	private const string INSPIROBOT_API_CHRISTMAS_URL = "http://inspirobot.me/api?generate=true&season=xmas";
	private const string INSPIROBOT_ICON_URL = "https://inspirobot.me/website/images/favicon.png";

	[SlashCommand("Inspire", "Generates a random inspirational image using inspirobot.me.", true, false)]
	public static async Task Inspire(InteractionContext context, [Option("Christmas", "Requests a christmas-themed inspirational image.")] bool christmas = false) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		HttpClient webClient = new HttpClient();
		HttpResponseMessage response = await webClient.GetAsync(new Uri(!christmas ? INSPIROBOT_API_URL : INSPIROBOT_API_CHRISTMAS_URL));

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithAuthor("Inspirobot", INSPIROBOT_URL, INSPIROBOT_ICON_URL)
				.WithImageUrl(await response.Content.ReadAsStringAsync())
				.WithFooter("Generated by inspirobot.me", context.Client.CurrentUser.AvatarUrl)
			)
		);
	}
}
