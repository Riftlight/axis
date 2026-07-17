using Godot;

public partial class SlimeDeathEffect : Node2D
{
	private const float ParticleDuration = 0.65f;
	private const float FadeDuration = 0.30f;

	private class Chunk
	{
		public Vector2 Position, Velocity, Size;
		public float Rotation, AngularVelocity, Life, MaxLife;
	}

	private Chunk[] _chunks;
	private Color _color;
	private Vector2 _gravityDir;
	private float _elapsed;
	private bool _fadeStarted;
	private ColorRect _fadeRect;

	public void Init(Vector2 gravityDir, Color color, float baseScale)
	{
		_gravityDir = gravityDir;
		_color = color;

		RandomNumberGenerator rng = new();
		rng.Randomize();

		_chunks = new Chunk[12];
		for (int i = 0; i < 12; i++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float speed = rng.RandfRange(80f, 400f);

			Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
			vel -= _gravityDir * rng.RandfRange(0f, 140f);

			bool isDroplet = i >= 8;
			float w = isDroplet ? rng.RandfRange(2f, 5f) : rng.RandfRange(5f, 14f);
			float h = isDroplet ? rng.RandfRange(2f, 4f) : rng.RandfRange(4f, 10f);
			_chunks[i] = new Chunk
			{
				Position = new Vector2(rng.RandfRange(-5f, 5f), rng.RandfRange(-5f, 5f)) * baseScale,
				Velocity = vel,
				Rotation = rng.RandfRange(0f, Mathf.Tau),
				AngularVelocity = rng.RandfRange(-8f, 8f),
				Size = new Vector2(w,h) * baseScale,
				Life = 0,
				MaxLife = rng.RandfRange(ParticleDuration / 2, ParticleDuration)
			};
		}

		CanvasLayer layer = new CanvasLayer { Layer = 128 };
		GetTree().CurrentScene.AddChild(layer);

		_fadeRect = new ColorRect
		{
			Color = Colors.Black,
			Modulate = new Color(1f, 1f, 1f, 0f), // start transparent
			AnchorRight = 1f,
			AnchorBottom = 1f
		};
		layer.AddChild(_fadeRect);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_elapsed += dt;

		const float chunkGravity = 550f;
		for (int i = 0; i < _chunks.Length; i++)
		{
			_chunks[i].Life += dt;
			_chunks[i].Velocity += _gravityDir * chunkGravity * dt;
			_chunks[i].Position += _chunks[i].Velocity * dt;
			_chunks[i].Rotation += _chunks[i].AngularVelocity * dt;
		}

		QueueRedraw();

		if (_elapsed >= ParticleDuration && !_fadeStarted)
		{
			_fadeStarted = true;
			Tween tween = CreateTween();
			tween.TweenProperty(_fadeRect, "modulate:a", 1f, FadeDuration);
			tween.TweenInterval(0.05f);
			tween.TweenCallback(Callable.From(() => LevelManager.Instance.RestartLevel()));
		}
	}


	public override void _Draw()
	{
		foreach (Chunk c in _chunks)
		{
			float t = Mathf.Clamp(c.Life / c.MaxLife, 0f, 1f);
			float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
			if (alpha <= 0f) continue;

			DrawSetTransform(c.Position, c.Rotation);
			DrawRect(new Rect2(-c.Size / 2f, c.Size), new Color (_color.R, _color.G, _color.B, alpha));
		}
		DrawSetTransform(Vector2.Zero, 0f);
	}

}
