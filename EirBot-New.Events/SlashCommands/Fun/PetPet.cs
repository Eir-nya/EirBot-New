using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using EirBot_New.Attributes;
using System.Text.Json;

namespace EirBot_New.Events;
[SlashCommandGroup("Fun", "Fun and games", true, false), GuildOnlyApplicationCommands]
public class PetPetCommandGuildOnly : ApplicationCommandsModule {
	[SlashCommand("Pet", "Pets someone.", true, false)]
	public static async Task PetPet(InteractionContext context, [Option("target", "User to pet.")] DiscordUser target) {
		await PetPetCommand.DoPetPet(context, target.AvatarUrl);
	}
}

[SlashCommandGroup("Fun", "Fun and games", true, false)]
public class PetPetCommand : ApplicationCommandsModule {
	private const string PETPET_URL = "https://api.obamabot.me/v2/image/petpet?image={0}";

	[ContextMenu(ApplicationCommandType.User, "Pet2")]
	public static async Task PetPet(ContextMenuContext context) {
		await DoPetPet(context, context.TargetUser.AvatarUrl);
	}

	[SlashCommand("Petpet", "Pets an image.", true, false)]
	public static async Task PetPet(InteractionContext context, [Option("url", "Url of image to pet.")] string url) {
		await DoPetPet(context, url);
	}

	public static async Task DoPetPet(BaseContext context, string url) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		if (url.IndexOf("?") > -1)
			url = url.Split("?")[0];

		HttpClient webClient = new HttpClient();
		HttpResponseMessage response = await webClient.GetAsync(string.Format(PETPET_URL, url));
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
			.AddFile(result.url.Split("/")[result.url.Count(c => c == '/')], await new HttpClient().GetStreamAsync(result.url))
		);
	}

	private class PetpetJson {
		public bool error { get; set; }
		public string message { get; set; }
		public int status { get; set; }
		public string url { get; set; }
	}
}
