using Godot;

public partial class BreakableBreakEffect : Node2D
{
	private struct Chunk
	{
		public Vector2 Position, Velocity, Size;
		public float Rotation, AngularVelocity;
	}

	private Vector2 _gravityDir = Vector2.Down;
	private bool _allOff = false;

	private Breakable _breakable;
	private Chunk[] _chunks;
	private Color _color;

	public void Init(Breakable breakable, Color color)
	{
		_breakable = breakable;
		_color = color;

		RandomNumberGenerator rng = new();
		rng.Randomize();

		Vector2 dims = _breakable.Scale;
		_chunks = new Chunk[(int)dims.X * 10/2];

		for (int i = 0; i < _chunks.Length; i++)
		{
			float w = rng.RandfRange(8f, 20f);
			float h = rng.RandfRange(4f, 12f);
			
			float angle = rng.RandfRange(0.25f*Mathf.Pi, 0.75f*Mathf.Pi);
			float speed = rng.RandfRange(80f, 200f);

			Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
			vel -= _gravityDir * rng.RandfRange(0f, 140f);

			_chunks[i] = new Chunk
			{
				Position = new(
					rng.RandfRange(-breakable.Scale.X*10, breakable.Scale.X*10),
					rng.RandfRange(-breakable.Scale.Y / 2f, breakable.Scale.Y / 2f)
				),
				Velocity = vel,
				Rotation = rng.RandfRange(0f, Mathf.Tau),
				AngularVelocity = rng.RandfRange(0f, Mathf.Tau),
				Size = new Vector2(w,h)
			};
		}
	}

	public override void _Process(double delta)
	{
		if (_allOff) return;
		float dt = (float)delta;
		
		const float chunkGrav = 550f;
		for (int i = 0; i < _chunks.Length; i++)
		{
			_chunks[i].Velocity += _gravityDir * chunkGrav * dt;
			_chunks[i].Position += _chunks[i].Velocity * dt;
			_chunks[i].Rotation += _chunks[i].AngularVelocity * dt;
		}

		_allOff = true;
		foreach (Chunk c in _chunks)
		{
			if (GetViewport().GetVisibleRect().HasPoint(ToGlobal(c.Position)))
			{
				_allOff = false;
				break;
			}
		}

		if (_allOff)
			QueueFree();

		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_allOff) return;
		foreach (Chunk c in _chunks)
		{
			DrawSetTransform(c.Position, c.Rotation);
			DrawRect(new Rect2(-c.Size / 2f, c.Size), _color);
		}
		DrawSetTransform(Vector2.Zero, 0f);
	}
}
