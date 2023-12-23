using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;

namespace EirBot_New.AppCommands;

[SlashCommandGroup("Roll", "Roll dice.", true, false)]
public partial class RollCommands : AppCommandGroupBase {}
