using DisCatSharp.Entities;

namespace EirBot_New.Events.Starboard;

[Serializable]
public struct StarboardSettings {
	public HashSet<string> acceptedEmoji;
	public short minStars;
	public bool allowNSFW;
	public bool allowSelfStar;
	public bool removeWhenDeleted;
	public bool removeWhenUnstarred;
	public bool useWebhook;

	public ulong channelID = 0;
	public Dictionary<ulong, ulong> messageLookup = new Dictionary<ulong, ulong>();
	public Dictionary<ulong, ulong> webhookJumpMessageLookup = new Dictionary<ulong, ulong>();
	public List<ulong> ignoredChannels = new List<ulong>();

	public StarboardSettings() {
		acceptedEmoji = new HashSet<string>() {":star:"};
		minStars = 2;
		allowNSFW = false;
		allowSelfStar = false;
		removeWhenDeleted = true;
		removeWhenUnstarred = true;
		useWebhook = true;
	}
}
