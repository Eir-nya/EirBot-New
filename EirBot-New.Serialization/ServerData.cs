namespace EirBot_New.Serialization;

public class ServerData : Saveable {
	public uint serverID;

	public override string GetFilename() {
		return serverID.ToString();
	}
}
