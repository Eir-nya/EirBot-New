using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using EirBot_New.Events.Emoji;

namespace EirBot_New.AppCommands;

public partial class EmojiCommands : AppCommandGroupBase {
	[SlashCommand("Send", "Sends an emoji to the chat as you.", true, false), ApplicationCommandRequireGuild]
	public static async Task Send(InteractionContext context, [Option("Search", "Text to search for emojis with.")] string search = "") {
		DiscordInteractionResponseBuilder rb = new DiscordInteractionResponseBuilder()
			.AsEphemeral()
			.WithContent("_ _");

		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, rb);

		// Get all emojis
		ulong id = (await context.GetOriginalResponseAsync()).Id;
		EmojiEvents.EmojiPickerData picker = new EmojiEvents.EmojiPickerData(context, id, await EmojiEvents.GetAllEmoji(context.Client, context.Guild, search));

		// No emojis found.
		if (picker.emojis.Count == 0) {
			await picker.context.EditResponseAsync(new DiscordWebhookBuilder()
				.WithContent("No emojis found for search \"" + search + "\".")
			);
			return;
		}

		EmojiEvents.activeEmojiPickers[id] = picker;

		await EmojiEvents.PaginateEmojis(picker);
	}
}
