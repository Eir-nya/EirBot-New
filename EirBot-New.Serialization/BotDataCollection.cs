namespace EirBot_New.Serialization;

public struct BotDataCollection {
	public Dictionary<Type, Dictionary<string, object>> allData { get; private set; } = new();

	// Loads all saved data
	public BotDataCollection() {
		if (!Directory.Exists("data"))
			return;

		string[] subfolders = Directory.GetDirectories("data");
		foreach (var sub in subfolders) {
			var subfolder = Path.GetRelativePath("data", sub);
			var type = Type.GetType(subfolder);
			if (type == null) {
				Console.WriteLine("FATAL ERROR: Could not parse subfolder " + subfolder + " to type.");
				continue;
			}

			this.allData[type] = new();
			foreach (var f in Directory.GetFiles("data/" + subfolder)) {
				var filename = Path.GetRelativePath("data/" + subfolder, f);
				this.allData[type][Path.GetFileNameWithoutExtension(filename)] = Convert.ChangeType(Saveable.Load(filename, type), type);
			}
		}
	}

	public T Get<T>(string fileName) {
		if (!this.allData.ContainsKey(typeof(T)))
			this.allData[typeof(T)] = new();
		if (!this.allData[typeof(T)].ContainsKey(fileName)) {
			this.allData[typeof(T)][fileName] = Activator.CreateInstance(typeof(T));
			((Saveable)this.allData[typeof(T)][fileName]).SetFilename(fileName);
			if (((Saveable)this.allData[typeof(T)][fileName]).Save())
				Console.WriteLine("Created new \"" + typeof(T).Name + "\" at \"" + fileName + "\".");
		}

		return (T)this.allData[typeof(T)][fileName];
	}
}