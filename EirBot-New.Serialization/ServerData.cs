using DisCatSharp;
using DisCatSharp.Entities;

using EirBot_New.Events.Starboard;

namespace EirBot_New.Serialization;

[Serializable]
public class ServerData : Saveable {
	public ulong serverID;

	public override void SetFilename(string fileName) {
		this.serverID = Convert.ToUInt64(fileName);
	}

	public override string GetFilename() =>
		this.serverID.ToString();

	public StarboardSettings starboardSettings = new();

	public static ServerData? GetServerData(DiscordClient client, DiscordGuild guild) {
		var b = Bot.GetBot(client);
		if (b == null)
			return null;

		var serverData = b.savedData.Get<ServerData>(guild.Id.ToString());
		return serverData;
	}
}