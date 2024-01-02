using DisCatSharp.Entities;

namespace EirBot_New.Events.Starboard;

[Serializable]
public struct StarboardSettings {
	public short minStars;
	public bool allowNSFW;
	public bool allowSelfStar;
	public bool removeWhenDeleted;
	public bool removeWhenUnstarred;
	public bool useWebhook;

	public ulong channelID = 0;
	public Dictionary<ulong, ulong> messageLookup = new();
	public Dictionary<ulong, ulong> webhookJumpMessageLookup = new();
	public List<ulong> ignoredChannels = new();

	public StarboardSettings() {
		this.minStars = 2;
		this.allowNSFW = false;
		this.allowSelfStar = false;
		this.removeWhenDeleted = true;
		this.removeWhenUnstarred = true;
		this.useWebhook = true;
	}
}