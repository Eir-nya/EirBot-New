using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace EirBot_New.AppCommands {
public partial class FunCommands : AppCommandGroupBase {
	private const string TEXT_URL = "https://cooltext.com/PostChange";
	private const string TEXT_FORMAT = "LogoID=4&Text={0}&FontSize=70&Color1_color=%23FF0000&Integer1=15&Boolean1=on&Integer9=1&Integer13=on&Integer12=on&BackgroundColor_color=$23FFFFFF";

	[SlashCommand("BurningText", "Generates burning text.", true, false)]
	public static async Task BurningText(InteractionContext context, [Option("text", "Text to use.")] string text) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		try {
			HttpResponseMessage post = await new HttpClient().PostAsync(TEXT_URL, new StringContent(String.Format(TEXT_FORMAT, text), new MediaTypeHeaderValue("application/x-www-form-urlencoded")));
			post.EnsureSuccessStatusCode();

			string response = await post.Content.ReadAsStringAsync();
			JObject result = JObject.Parse(response);

			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddFile("text.gif", await new HttpClient().GetStreamAsync(result["renderLocation"]?.ToString()))
			);
		} catch (Exception e) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddEmbed(new DiscordEmbedBuilder()
					.WithDescription("Failed to receive burning text data.\nReason:\n" + e.GetType() + ":\n" + e.Message)
				)
			);
		}
	}
}
}
