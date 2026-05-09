using System;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct BoundsData
{
	private readonly Renderer _renderer;

	private readonly Bounds _bounds;

	public BoundsData(Renderer renderer)
	{
		_renderer = renderer;
		_bounds = renderer.bounds;
	}

	public BoundsData(Bounds bounds)
	{
		_renderer = null;
		_bounds = bounds;
	}

	public Bounds GetBounds()
	{
		if (!_renderer)
		{
			return _bounds;
		}
		return _renderer.bounds;
	}
}
