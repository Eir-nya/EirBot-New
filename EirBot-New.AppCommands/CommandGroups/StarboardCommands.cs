using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using EirBot_New.Attributes;

namespace EirBot_New.AppCommands;

[SlashCommandGroup("Starboard", "Starboard settings.", false, false), GuildOnlyApplicationCommands]
public partial class StarboardCommands : ApplicationCommandsModule {}
