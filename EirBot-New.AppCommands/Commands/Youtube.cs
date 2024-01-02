using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Google.Apis.YouTube.v3.Data;

// https://stackoverflow.com/a/21249261

namespace EirBot_New.Events {
using DisCatSharp;
using DisCatSharp.EventArgs;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

[EventHandler]
public static class YoutubeEvents {
	private const string YOUTUBE_VIDEO_PREFIX = "https://www.youtube.com/watch?v=";

	private static readonly Dictionary<ulong, YTSearchData> storedSearchResults = new Dictionary<ulong, YTSearchData>();
	private static readonly Dictionary<int, string> buttonEmojis = new Dictionary<int, string>() {
		[0] = ":one:",
		[1] = ":two:",
		[2] = ":three:",
		[3] = ":four:",
		[4] = ":five:"
	};

	public struct YTSearchData {
		public InteractionContext context;
		public videoData[] results;
		public int selected;
	}

	public struct videoData {
		public SearchResult result;
		public string title;
		public string url;
	}

	public static string apiKey;

	// Initializes API Key
	[Event(DiscordEvent.Ready)]
	public static async Task Ready(DiscordClient client, ReadyEventArgs args) {
		if (File.Exists("youtube_api_key.txt"))
			apiKey = File.ReadAllText("youtube_api_key.txt");
	}

	// Interaction with buttons on existing message
	[Event(DiscordEvent.ComponentInteractionCreated)]
	public static async Task ButtonClicked(DiscordClient client, ComponentInteractionCreateEventArgs args) {
		if (!args.Id.StartsWith("youtube_queryLink_"))
			return;

		ulong id = 0;
		int newSelection = 0;
		string[] splitParts = args.Id.Split("_", StringSplitOptions.None);
		if (splitParts.Length > 2)
			id = (ulong)Convert.ToInt64(splitParts[2]);
		newSelection = (int)Convert.ToInt16(splitParts[3]);

		// Try to fetch stored YTSearchData
		YTSearchData activePicker;
		bool found = storedSearchResults.TryGetValue(args.Message.Id, out activePicker);
		if (found && activePicker.context.User == args.User)
			await ChangeVideo(activePicker, newSelection);
	}

	public static async Task<YTSearchData?> NewSearchData(InteractionContext context, string query) {
		// Send initial message
		await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
			.WithContent(":hourglass:")
		);

		// Find results
		YouTubeService yt = new YouTubeService(new BaseClientService.Initializer() {
			ApplicationName = typeof(YoutubeEvents).ToString(),
			ApiKey = apiKey
		});
		SearchResource.ListRequest listRequest = yt.Search.List("snippet");
		listRequest.Q = query;
		listRequest.MaxResults = 5;
		listRequest.Type = "video";
		SearchListResponse response = listRequest.Execute();

		YTSearchData data = new YTSearchData() {
			context = context,
			results = response.Items.Select<SearchResult, videoData>(sr => ParseResult(sr)).ToArray(),
			selected = 0
		};
		storedSearchResults[(await context.GetOriginalResponseAsync()).Id] = data;
		return data;
	}

	public static async Task ChangeVideo(YTSearchData data, int newSelection) {
		// Set up buttons
		DiscordButtonComponent[] buttons = new DiscordButtonComponent[5];

		ulong id = (await data.context.GetOriginalResponseAsync()).Id;
		for (int i = 0; i < data.results.Length; i++) {
			DiscordButtonComponent butt = new DiscordButtonComponent(
				ButtonStyle.Secondary,
				"youtube_queryLink_" + id + "_" + i,
				data.results[i].title,
				newSelection == i,
				new DiscordComponentEmoji(DiscordEmoji.FromName(data.context.Client, buttonEmojis[i]))
			);
			buttons[i] = butt;
		}

		DiscordWebhookBuilder wb = new DiscordWebhookBuilder().WithContent(data.results[newSelection].url);
		foreach (DiscordButtonComponent b in buttons)
			wb.AddComponents(b);
		await data.context.EditResponseAsync(wb);
	}

	private static videoData ParseResult(SearchResult result) {
		JObject json = JObject.Parse(JsonConvert.SerializeObject(result));
		string title = result.Snippet.Title;
		string url = YOUTUBE_VIDEO_PREFIX + json["id"]?["videoId"]?.ToString();

		videoData o = new videoData() {
			result = result,
			title = title,
			url = url
		};

		return o;
	}
}
}

namespace EirBot_New.AppCommands {
using EirBot_New.Events;

public partial class GenericCommands : AppCommandGroupBase {
	[SlashCommand("yt", "Quickly searches Youtube for up to 5 matching videos.", true, false)]
	public static async Task ytSearch(InteractionContext context, [Option("Query", "Search text.")] string query) {
		if (string.IsNullOrEmpty(YoutubeEvents.apiKey)) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
				new DiscordInteractionResponseBuilder()
					.AsEphemeral()
					.WithContent(":x: Error: Youtube API key not set up.")
			);
			return;
		}

		YoutubeEvents.YTSearchData? data = await YoutubeEvents.NewSearchData(context, query);
		if (data != null)
			await YoutubeEvents.ChangeVideo(data.Value, 0);
	}
}
}
