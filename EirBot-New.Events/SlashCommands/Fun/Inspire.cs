using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace EirBot_New.Events;
[SlashCommandGroup("Fun", "Fun and games", true, false)]
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
		string url = await response.Content.ReadAsStringAsync();

		if (string.IsNullOrEmpty(url) || !response.IsSuccessStatusCode) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddEmbed(new DiscordEmbedBuilder()
					.WithAuthor("Inspirobot", INSPIROBOT_URL, INSPIROBOT_ICON_URL)
					.WithDescription("Failed to retrieve data from inspirobot.\nReason:\n" + response.ReasonPhrase)
				)
			);
			return;
		}

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.AddEmbed(new DiscordEmbedBuilder()
				.WithAuthor("Inspirobot", INSPIROBOT_URL, INSPIROBOT_ICON_URL)
				.WithImageUrl(url)
				.WithFooter("Generated by inspirobot.me", context.Client.CurrentUser.AvatarUrl)
			)
		);

		// Force error check - sometimes image width/height will just be 0.
		// If so, remove the image from the embed and just upload the image as an attachment.
		// Thanks discord.
		DiscordEmbedImage image = (await context.GetOriginalResponseAsync()).Embeds[0].Image;
		if (image.Width == 0 && image.Height == 0) {
			// Retrieve image
			Stream imageData = await new HttpClient().GetStreamAsync(url);

			string filename = url;
			string[] splitResults = url.Split(new string[] { "/a/" }, StringSplitOptions.None);
			if (splitResults.Length > 1)
				filename = splitResults[1];

			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddFile(filename, imageData)
			);
		}
	}
}
