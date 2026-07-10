using Godot;

public partial class SwitchUi : CanvasLayer
{
	[Export] public bool SwitchesAreLimited = true;
	private Label label;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (SwitchesAreLimited)
			label = GetNode<Label>("Label");
	}

	public void UpdateSwitches(int remaining)
	{
		if (SwitchesAreLimited)
			label.Text = ""+remaining;
	}
}
