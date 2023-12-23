using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace EirBot_New.AppCommands {
using EirBot_New.Events;
public partial class ContextMenuCommands : ApplicationCommandsModule {
	[ContextMenu(ApplicationCommandType.User, "Pet"), ApplicationCommandRequireGuild]
	public static async Task PetPet(ContextMenuContext context) {
		await PetPetCommand.DoPetPet(context, context.TargetUser.AvatarUrl);
	}
}

public partial class FunCommands : ApplicationCommandsModule {
	[SlashCommand("Pet", "Pets someone.", true, false), ApplicationCommandRequireGuild]
	public static async Task PetPet(InteractionContext context, [Option("target", "User to pet.")] DiscordUser target) {
		await PetPetCommand.DoPetPet(context, target.AvatarUrl);
	}

	[SlashCommand("Petpet", "Pets an image.", true, false)]
	public static async Task PetPet(InteractionContext context, [Option("url", "Url of image to pet.")] string url) {
		await PetPetCommand.DoPetPet(context, url);
	}
}
}

namespace EirBot_New.Events {
public class PetPetCommand {
	private const string PETPET_URL = "https://api.obamabot.me/v1/image/petpet?avatar={0}";

	public static async Task DoPetPet(BaseContext context, string url) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		if (url.IndexOf("?") > -1)
			url = url.Split("?")[0];

		// v1
		try {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddFile("Petpet.gif", await new HttpClient().GetStreamAsync(string.Format(PETPET_URL, url)))
			);
		} catch (Exception e) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddEmbed(new DiscordEmbedBuilder()
					.WithDescription("Failed to receive petpet data.\nReason:\n" + e.GetType() + ":\n" + e.Message)
				)
			);
			return;
		}
		// v2
		/*
		HttpResponseMessage response = await new HttpClient().GetAsync(string.Format(PETPET_URL, url));
		string data = await response.Content.ReadAsStringAsync();
		PetpetJson result;
		try {
			result = JsonSerializer.Deserialize<PetpetJson>(data);
			if (result.error)
				throw new Exception(result.message);
		} catch (Exception e) {
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddEmbed(new DiscordEmbedBuilder()
					.WithDescription("Failed to receive petpet data.\nReason:\n" + e.GetType() + ":\n" + e.Message)
				)
			);
			return;
		}

		await context.EditResponseAsync(new DiscordWebhookBuilder()
			.AddFile(result.url.Split("/")[result.url.Count(c => c == '/')], await new HttpClient().GetStreamAsync(result.url))
		);
		*/
	}

	/*
	private class PetpetJson {
		public bool error { get; set; }
		public string message { get; set; }
		public int status { get; set; }
		public string url { get; set; }
	}
	*/
}
}
