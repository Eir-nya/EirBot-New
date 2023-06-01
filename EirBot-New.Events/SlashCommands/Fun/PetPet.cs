using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using System.Text.Json;

namespace EirBot_New.Events;
[SlashCommandGroup("Fun", "Fun and games", true, false)]
public class PetPetCommand : ApplicationCommandsModule {
	private const string PETPET_URL = "https://api.obamabot.me/v2/image/petpet?image={0}";

	[SlashCommand("Petpet", "Pets someone.", true, false)]
	public static async Task PetPet(InteractionContext context, [Option("target", "User to pet.")] DiscordUser target) {
		await PetPet(context, target.AvatarUrl);
	}
	[ContextMenu(ApplicationCommandType.User, "Petpet")]
	public static async Task PetPet(ContextMenuContext context) {
		await PetPet(context, context.TargetUser.AvatarUrl);
	}
	[SlashCommand("Petpet", "Pets an image.", true, false)]
	public static async Task PetPet(InteractionContext context, [Option("url", "Url of image to pet.")] string url) {
		await PetPet(context, url);
	}

	private static async Task PetPet(BaseContext context, string url) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		HttpClient webClient = new HttpClient();
		HttpResponseMessage response = await webClient.GetAsync(string.Format(PETPET_URL, url.Split("webp")[0] + "png"));
		string data = await response.Content.ReadAsStringAsync();
		PetpetJson result;
		try {
			result = JsonSerializer.Deserialize<PetpetJson>(data);
			if (result.error)
				throw new Exception();
		} catch {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.WithContent("Failed to receive petpet data.")
			);
			return;
		}

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.KeepAttachments(true)
			.WithContent(result.url)
		);
	}

	private struct PetpetJson {
		public bool error;
		public string message;
		public int status;
		public string url;
	}
}
