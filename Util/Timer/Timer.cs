using Godot;
using Godot.Collections;

public partial class Timer : Label
{
	public static Timer Instance { get; private set; }

	private float _elapsedTime = 0;
	private bool _stopped = false;

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _Process(double delta)
	{
		if (_stopped) return;
		_elapsedTime += (float)delta;

		int mins = Mathf.FloorToInt(_elapsedTime / 60);
		int secs = Mathf.FloorToInt(_elapsedTime % 60);
		int millis = Mathf.FloorToInt((_elapsedTime % 1) * 100);

		if (mins == 0)
			this.Text = $"{secs:D2}.{millis:D3}";
		else
			this.Text = $"{mins:D2}:{secs:D2}.{millis:D3}";
	}

	public static void LevelComplete(int levelIndex)
	{
		Instance?.Stop();
	}

	private void Stop()
	{
		_stopped = true;
	}

	private void UpdateTime(string levelId, float time)
	{	
		const string savePath = "user://level_times.dat";
		const string encryptionKey = "5f10577476a988495d411916ce9bba76ace12b058af4aabceaa1b934489f3a51"; // we need some very strong encryption for this. very important files being stored. if anyone could read their scores the world would explode.

		Dictionary<string, float> timeData = new();

		if (FileAccess.FileExists(savePath))
		{
			using var readFile = FileAccess.OpenEncryptedWithPass(savePath, FileAccess.ModeFlags.Read, encryptionKey);
			if (readFile != null)
				timeData = (Dictionary<string,float>)readFile.GetVar();
		}

		if (time >= timeData[levelId]) return;
		timeData[levelId] = time;

		using var writeFile = FileAccess.OpenEncryptedWithPass(savePath, FileAccess.ModeFlags.Write, encryptionKey);
		if (writeFile == null)
		{
			GD.PrintErr($"Failed to save time :( Error code: {FileAccess.GetOpenError()}");
			return;
		}

		writeFile.StoreVar(timeData);
	}
}
