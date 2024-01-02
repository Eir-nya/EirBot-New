using DisCatSharp;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;

namespace EirBot_New.Events.Emoji;

[EventHandler]
public class EmojiEvents {
	public const int ROWS_PER_PAGE = 4;
	internal protected static Dictionary<ulong, EmojiPickerData> activeEmojiPickers = new();

	public class EmojiPickerData {
		public InteractionContext context;
		public ulong id;
		public int page;
		public List<DiscordEmoji> emojis;
		public readonly int totalPages;

		public EmojiPickerData(InteractionContext context, ulong id, List<DiscordEmoji> emojis) {
			this.context = context;
			this.id = id;
			this.page = 0;
			this.emojis = emojis;
			this.totalPages = (int)MathF.Ceiling((float)emojis.Count / (5 * ROWS_PER_PAGE));
		}
	}

	private static DiscordButtonComponent[] PageButtons(EmojiPickerData picker) {
		return new DiscordButtonComponent[] {
			new(ButtonStyle.Primary, "emoji_Send_" + picker.id + "_Left", "<<", picker.page == 0, new(898279635379449856)),
			new(ButtonStyle.Primary, null, string.Format("Page {0}/{1}", picker.page + 1, picker.totalPages), true, new(963546868619571200)),
			new(ButtonStyle.Primary, "emoji_Send_" + picker.id + "_Right", ">>", picker.page == picker.totalPages - 1, new(898284896102015006))
		};
	}

	public async static Task PaginateEmojis(EmojiPickerData picker) {
		var wb = new DiscordWebhookBuilder()
			.AddComponents(PageButtons(picker));

		for (var i = picker.page * 5 * ROWS_PER_PAGE; i < Math.Min(picker.emojis.Count, (picker.page + 1) * 5 * ROWS_PER_PAGE); i += 5) {
			List<DiscordButtonComponent> newButtons = new();
			for (var j = i; j < Math.Min(i + 5, picker.emojis.Count); j++)
				newButtons.Add(new(ButtonStyle.Secondary, "emoji_Send_" + picker.id + "_" + picker.emojis[j].Id, picker.emojis[j].Name, false, new(picker.emojis[j])));
			wb.AddComponents(newButtons);
		}

		await picker.context.EditResponseAsync(wb);
	}

	[Event(DiscordEvent.ComponentInteractionCreated)]
	public async static Task ButtonClicked(DiscordClient client, ComponentInteractionCreateEventArgs args) {
		if (!args.Id.StartsWith("emoji_Send_"))
			return;

		ulong id = 0;
		var arg = string.Empty;
		string[] splitParts = args.Id.Split("_", StringSplitOptions.None);
		if (splitParts.Length > 2)
			id = (ulong)Convert.ToInt64(splitParts[2]);
		arg = splitParts[3];

		if (!activeEmojiPickers.ContainsKey(id))
			return;

		// Acknowledge the interaction
		await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

		var picker = activeEmojiPickers[id];
		if (arg == "Left") {
			picker.page--;
			await PaginateEmojis(picker);
		} else if (arg == "Right") {
			picker.page++;
			await PaginateEmojis(picker);
		} else {
			var emoji = DiscordEmoji.FromGuildEmote(picker.context.Client, Convert.ToUInt64(arg));
			var hook = await Util.GetOrCreateWebhook(picker.context.Client, picker.context.Channel);
			await Util.ModifyWebhookAsync(hook, null, null, picker.context.Channel.Id);

			// Attempt to get author's display name in the server
			var name = picker.context.User.Username;
			var avatarURL = picker.context.User.AvatarUrl;
			var member = await args.Channel.Guild.GetMemberAsync(args.User.Id, false);
			if (member == null)
				member = await args.Channel.Guild.GetMemberAsync(args.User.Id, true);
			if (member != null) {
				if (!string.IsNullOrEmpty(member.DisplayName))
					name = member.DisplayName;
				if (!string.IsNullOrEmpty(member.GuildAvatarUrl))
					avatarURL = member.GuildAvatarUrl;
			}

			await hook.ExecuteAsync(new DiscordWebhookBuilder()
				.WithUsername(name)
				.WithAvatarUrl(avatarURL)
				.WithContent(emoji.Url + "?size=48")
			);
			activeEmojiPickers.Remove(picker.id);
			await picker.context.DeleteResponseAsync();
		}
	}

	internal protected async static Task<List<DiscordEmoji>> GetAllEmoji(DiscordClient client, DiscordGuild currentGuild, string search) {
		List<DiscordEmoji> emoji = new();
		foreach (var g in client.Guilds.Values)
		foreach (DiscordEmoji e in await g.GetEmojisAsync())
			if (e.IsAvailable && (g != currentGuild || e.IsAnimated))
				if (e.Name.ToLower().Contains(search.ToLower()))
					emoji.Add(e);
		emoji.Sort((emoji1, emoji2) => emoji1.Name.CompareTo(emoji2.Name));
		return emoji;
	}
}