using DisCatSharp;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Enums;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using EirBot_New.Attributes;

namespace EirBot_New.Events.Emoji;

// [EventHandler]
public class EmojiEvents {
	public const int ROWS_PER_PAGE = 4;
	protected internal static Dictionary<ulong, EmojiPickerData> activeEmojiPickers = new Dictionary<ulong, EmojiPickerData>();

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

	// Run on bot ready
	[RunOnStartup]
	private static void RunOnStartup(DiscordShardedClient client, Bot bot) {
		client.ComponentInteractionCreated += async (DiscordClient client, ComponentInteractionCreateEventArgs args) => bot.AddTask(() => ButtonClicked(client, args));
	}

	private static DiscordButtonComponent[] PageButtons(EmojiPickerData picker) {
		return new DiscordButtonComponent[] {
			new DiscordButtonComponent(ButtonStyle.Primary, "emoji_Send_" + picker.id + "_Left", "<<", picker.page == 0, new DiscordComponentEmoji(898279635379449856)),
			new DiscordButtonComponent(ButtonStyle.Primary, null, string.Format("Page {0}/{1}", picker.page + 1, picker.totalPages), true, new DiscordComponentEmoji(963546868619571200)),
			new DiscordButtonComponent(ButtonStyle.Primary, "emoji_Send_" + picker.id + "_Right", ">>", picker.page == picker.totalPages - 1, new DiscordComponentEmoji(898284896102015006))
		};
	}

	public static async Task PaginateEmojis(EmojiPickerData picker) {
		DiscordWebhookBuilder wb = new DiscordWebhookBuilder()
			.AddComponents(PageButtons(picker));

		for (int i = picker.page * (5 * ROWS_PER_PAGE); i < Math.Min(picker.emojis.Count, (picker.page + 1) * (5 * ROWS_PER_PAGE)); i += 5) {
			List<DiscordButtonComponent> newButtons = new List<DiscordButtonComponent>();
			for (int j = i; j < Math.Min(i + 5, picker.emojis.Count); j++)
				newButtons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, "emoji_Send_" + picker.id + "_" + picker.emojis[j].Id, picker.emojis[j].Name, false, new DiscordComponentEmoji(picker.emojis[j])));
			wb.AddComponents(newButtons);
		}

		await picker.context.EditResponseAsync(wb);
	}

	// [Event(DiscordEvent.ComponentInteractionCreated)]
	public static async Task ButtonClicked(DiscordClient client, ComponentInteractionCreateEventArgs args) {
		if (!args.Id.StartsWith("emoji_Send_"))
			return;

		ulong id = 0;
		string arg = string.Empty;
		string[] splitParts = args.Id.Split("_", StringSplitOptions.None);
		if (splitParts.Length > 2)
			id = (ulong)Convert.ToInt64(splitParts[2]);
		arg = splitParts[3];

		if (!activeEmojiPickers.ContainsKey(id))
			return;

		// Acknowledge the interaction
		await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

		EmojiPickerData picker = activeEmojiPickers[id];
		if (arg == "Left") {
			picker.page--;
			await PaginateEmojis(picker);
		} else if (arg == "Right") {
			picker.page++;
			await PaginateEmojis(picker);
		} else {
			DiscordEmoji emoji = DiscordEmoji.FromGuildEmote(picker.context.Client, Convert.ToUInt64(arg));
			DiscordWebhook? hook = await Util.GetOrCreateWebhook(picker.context.Client, picker.context.Channel);
			await Util.ModifyWebhookAsync(hook, null, null, picker.context.Channel.Id);

			// Attempt to get author's display name in the server
			string name = picker.context.User.Username;
			string avatarURL = picker.context.User.AvatarUrl;
			DiscordMember member = await args.Channel.Guild.GetMemberAsync(args.User.Id, false);
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

	protected internal static async Task<List<DiscordEmoji>> GetAllEmoji(DiscordClient client, DiscordGuild currentGuild, string search) {
		List<DiscordEmoji> emoji = new List<DiscordEmoji>();
		foreach (DiscordGuild g in client.Guilds.Values)
			foreach (DiscordEmoji e in await g.GetEmojisAsync())
				if (e.IsAvailable && (g != currentGuild || e.IsAnimated))
					if (e.Name.ToLower().Contains(search.ToLower()))
						emoji.Add(e);
		emoji.Sort((emoji1, emoji2) => emoji1.Name.CompareTo(emoji2.Name));
		return emoji;
	}
}
