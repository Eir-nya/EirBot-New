using DisCatSharp;
using DisCatSharp.Entities;
using EirBot_New.Events.Starboard;

namespace EirBot_New.Serialization;

[Serializable]
public class ServerData : Saveable {
	public ulong serverID;

	public override void SetFilename(string fileName) { serverID = Convert.ToUInt64(fileName); }
	public override string GetFilename() { return serverID.ToString(); }

	public static ServerData? GetServerData(DiscordClient client, DiscordGuild guild) {
		Bot b = Bot.GetBot(client);
		if (b == null)
			return null;
		ServerData serverData = b.savedData.Get<ServerData>(guild.Id.ToString());
		return serverData;
	}
}
