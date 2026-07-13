using Godot;

public partial class SwitchCounter : Node2D
{
	[Export] public int Limit;

	private int switches = 0;

	public int GetRemaining()
	{
		return Mathf.Max(Limit - switches, 0);
	}

	public void Switched()
	{
		switches += 1;
	}
}
