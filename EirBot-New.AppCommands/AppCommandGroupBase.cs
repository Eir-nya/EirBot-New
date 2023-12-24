using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using System.Reflection;

namespace EirBot_New.AppCommands;

public class AppCommandGroupBase : ApplicationCommandsModule {
	private static MethodInfo? FetchAppCommandByName(string name, string subName, string subSubName) {
		foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
			// Look for SlashCommandGroup if it exists, assume this is "name"
			SlashCommandGroupAttribute? sCGAttr = t.GetCustomAttribute<SlashCommandGroupAttribute>();
			if (sCGAttr != null && sCGAttr.Name == name) {
				// Look for a nested type, 1 layer down.
				if (!string.IsNullOrEmpty(subSubName)) {
					foreach (Type t2 in t.GetNestedTypes())
						foreach (MethodInfo mi in t2.GetMethods()) {
							SlashCommandAttribute? sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
							if (sCAttr != null && sCAttr.Name == subSubName)
								return mi;
						}
				// Otherwise, look for method here
				} else
					foreach (MethodInfo mi in t.GetMethods()) {
						SlashCommandAttribute? sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
						if (sCAttr != null && sCAttr.Name == subName)
							return mi;
					}
			}
			// Otherwise, just look for "name" as an immediate child
			else
				foreach (MethodInfo mi in t.GetMethods()) {
					SlashCommandAttribute? sCAttr = mi.GetCustomAttribute<SlashCommandAttribute>();
					if (sCAttr != null && sCAttr.Name == name)
						return mi;
				}
		}

		return null;
	}

	private static bool RequireGuild(string name, string subName, string subSubName) {
		MethodInfo? mi = FetchAppCommandByName(name, subName, subSubName);

		return mi != null && mi.GetCustomAttribute<ApplicationCommandRequireGuildAttribute>() != null;
	}

	///

	public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx) {
		bool requiresGuild = RequireGuild(ctx.CommandName, ctx.SubCommandName, ctx.SubSubCommandName);
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

	public override async Task<bool> BeforeContextMenuExecutionAsync(ContextMenuContext ctx) {
		bool requiresGuild = RequireGuild(ctx.CommandName, ctx.SubCommandName, ctx.SubSubCommandName);
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
