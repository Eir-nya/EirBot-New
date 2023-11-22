using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

namespace EirBot_New.Events;
[ApplicationCommandRequirePermissions(Permissions.ManageMessages)]
public class GaslightCommands : ApplicationCommandsModule {
	[ContextMenu(ApplicationCommandType.Message, "\"Edit\" message")]
	public static async Task Command(ContextMenuContext context) {
		DiscordWebhook? hook = await Util.GetOrCreateWebhook(context.Client, context.Channel);

		// Attempt to get author's display name in the server
		string name = context.TargetMessage.Author.Username;
		string avatarURL = context.TargetMessage.Author.AvatarUrl;

		// If webhook message, skip member check
		if (context.TargetMessage.WebhookMessage && context.TargetMessage.WebhookId == hook.Id) {
			name = context.TargetMessage.Author.Username;
			avatarURL = context.TargetMessage.Author.AvatarUrl;
		} else {
			DiscordMember member = await context.Guild.GetMemberAsync(context.TargetMessage.Author.Id, false);
			if (member == null)
				member = await context.Guild.GetMemberAsync(context.TargetMessage.Author.Id, true);
			if (member != null) {
				if (!string.IsNullOrEmpty(member.DisplayName))
					name = member.DisplayName;
				if (!string.IsNullOrEmpty(member.GuildAvatarUrl))
					avatarURL = member.GuildAvatarUrl;
			}
		}

		DiscordInteractionModalBuilder mb = new DiscordInteractionModalBuilder()
			.WithTitle("Speak as " + name)
			.WithCustomId("modal_say");
		mb.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Paragraph, "toSay", "New text", name + ": ", 1, 2000, true));

		await context.CreateModalResponseAsync(mb);
		InteractivityResult<ComponentInteractionCreateEventArgs> result = await context.Client.GetInteractivity().WaitForModalAsync(mb.CustomId);
		if (result.TimedOut) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("Modal timed out.")
			);
			return;
		}

		string toSay = string.Empty;
		foreach (DiscordComponentResult component in result.Result.Interaction.Data.Components)
			if (component.CustomId == "toSay") {
				toSay = component.Value;
				break;
			}

		if (string.IsNullOrEmpty(toSay)) {
			await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
				.AsEphemeral()
				.WithContent("No text entered.")
			);
			return;
		}

		// Check if message was sent by this bot hook originally
		if (context.TargetMessage.WebhookMessage && context.TargetMessage.WebhookId == hook.Id)
			await hook.EditMessageAsync(context.TargetMessage.Id, new DiscordWebhookBuilder().WithContent(toSay));
		else {
			// Send message
			await Util.ModifyWebhookAsync(hook, name, null, context.Channel.Id);
			await hook.ExecuteAsync(new DiscordWebhookBuilder()
				.WithUsername(name)
				.WithAvatarUrl(avatarURL)
				.WithContent(toSay));

			// Delete original if possible
			Permissions botPermsInChannel = context.Channel.PermissionsFor(await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id));
			if (botPermsInChannel.HasFlag(Permissions.ManageMessages))
				await context.TargetMessage.DeleteAsync();
		}

		await Util.CloseModal(result.Result.Interaction);
	}
}
