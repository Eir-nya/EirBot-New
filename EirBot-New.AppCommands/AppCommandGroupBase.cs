using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using System.Reflection;

namespace EirBot_New.AppCommands;

public class AppCommandGroupBase : ApplicationCommandsModule {
	private static MethodInfo? FetchAppCommandByName(string name, string subName, string subSubName) {
		foreach (var t in Assembly.GetExecutingAssembly().GetTypes()) {
			// Look for SlashCommandGroup if it exists, assume this is "name"
			var sCGAttr = t.GetCustomAttribute<SlashCommandGroupAttribute>();
			if (sCGAttr != null && sCGAttr.Name == name) {
				// Look for a nested type, 1 layer down.
				if (!string.IsNullOrEmpty(subSubName))
					foreach (var t2 in t.GetNestedTypes())
					foreach (var mi in t2.GetMethods()) {
						var sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
						if (sCAttr != null && sCAttr.Name == subSubName)
							return mi;
					}
				// Otherwise, look for method here
				else
					foreach (var mi in t.GetMethods()) {
						var sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
						if (sCAttr != null && sCAttr.Name == subName)
							return mi;
					}
			}
			// Otherwise, just look for "name" as an immediate child
			else
				foreach (var mi in t.GetMethods()) {
					var sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
					if (sCAttr != null && sCAttr.Name == name)
						return mi;
				}
		}

		return null;
	}

	private static bool RequireGuild(string name, string subName, string subSubName) {
		var mi = FetchAppCommandByName(name, subName, subSubName);

		return mi != null && mi.GetCustomAttribute<ApplicationCommandRequireGuildAttribute>() != null;
	}

	///
	public async override Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx) {
		var requiresGuild = RequireGuild(ctx.CommandName, ctx.SubCommandName, ctx.SubSubCommandName);
		if (requiresGuild)
			if (ctx.Guild == null || ctx.Channel.Guild == null) {
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
					.AsEphemeral()
					.WithContent("Command can only be used in servers.")
				);
				return false;
			}

		return true;
	}

	public async override Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx) {
		var requiresGuild = RequireGuild(ctx.CommandName, ctx.SubCommandName, ctx.SubSubCommandName);
		if (requiresGuild)
			if (ctx.Guild == null || ctx.Channel.Guild == null) {
				await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
					.AsEphemeral()
					.WithContent("Context menu action can only be used in servers.")
				);
				return false;
			}

		return true;
	}
}