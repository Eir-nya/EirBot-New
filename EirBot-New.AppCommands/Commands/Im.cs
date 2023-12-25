using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using FFmpegArgs.Executes;

namespace EirBot_New.AppCommands;

public partial class ContextMenuCommandsCommands : AppCommandGroupBase {
	private static FFmpegRenderConfig ffmpegConfig = new FFmpegRenderConfig().WithFFmpegBinaryPath("/usr/bin/ffmpeg");

	[ContextMenu(ApplicationCommandType.Message, "ffmpeg: Invert")]
	public static async Task Invert(ContextMenuContext context) { await TemplateCommand(context, "-vf negate"); }



	private static async Task TemplateCommand(ContextMenuContext context, string command) {
		await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		string? mediaURL = await GetMediaURLFromMessage(context);
		if (mediaURL != null) {
			FileStream outStream = await SimpleFFMPEGOperation(mediaURL, command);
			await context.EditResponseAsync(new DiscordWebhookBuilder()
				.AddFile(outStream.Name, outStream)
			);

			try {
				File.Delete(outStream.Name);
				File.Delete(outStream.Name.Replace("_out", ""));
			} catch (Exception e) {
				Console.WriteLine("EXCEPTION: " + e);
			}
		}
	}

	private static async Task<string?> GetMediaURLFromMessage(ContextMenuContext context) {
		// Attachments are prioritized.
		if (context.TargetMessage.Attachments.Count > 0)
			return context.TargetMessage.Attachments[0].Url;
		// Then embeds.
		else if (context.TargetMessage.Embeds.Count > 0)
			return context.TargetMessage.Embeds[0].Url.ToString();

		await context.FollowUpAsync(new DiscordFollowupMessageBuilder()
			.AsEphemeral()
			.WithContent("Targeted message did not have a valid media URL.")
		);
		await context.DeleteResponseAsync();
		return null;
	}

	private static async Task<FileStream> SimpleFFMPEGOperation(string url, string command) {
		string newName = await DownloadFileTemp(url);
		string outputName = await FFMPEGExecute(newName, command, Path.GetExtension(url).Split('?')[0]);
		return new FileStream(outputName, FileMode.Open, FileAccess.Read);
	}

	private static async Task<string> DownloadFileTemp(string url) {
		string newName = Guid.NewGuid().ToString() + Path.GetExtension(url).Split('?')[0];

		HttpClient webby = new HttpClient();
		HttpResponseMessage response = await webby.GetAsync(url);
		FileStream fs = new FileStream("temp/" + newName, FileMode.CreateNew);
		await response.Content.CopyToAsync(fs);
		await fs.DisposeAsync();
		webby.Dispose();

		return "temp/" + newName;
	}

	private static async Task<string> FFMPEGExecute(string inputFileName, string command, string extension) {
		// For some reason this never seems to work.
		Directory.CreateDirectory("temp");

		string outputName = "temp/" + Path.GetFileNameWithoutExtension(inputFileName) + "_out" + extension;
		try {
			string fullCommand = "-y -i \"" + inputFileName + "\" " + command + " \"" + outputName + "\"";
			FFmpegRender task = FFmpegRender.FromArguments(fullCommand, ffmpegConfig);
			await task.ExecuteAsync();
			return outputName;
		} catch(Exception e) {
			Console.WriteLine("FFMPEG ERROR: " + e.Message);
			if (File.Exists(outputName))
				File.Delete(outputName);
			throw e;
		}
	}
}
