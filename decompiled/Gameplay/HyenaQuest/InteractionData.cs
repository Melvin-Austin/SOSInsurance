using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public class InteractionData
{
	public Interaction interaction;

	public BoundsData[] renderers;

	public string hint;

	public InteractionData(Interaction interaction, Renderer[] targets, string hint = null)
	{
		this.interaction = interaction;
		if (targets != null)
		{
			List<BoundsData> list = new List<BoundsData>();
			foreach (Renderer renderer in targets)
			{
				if ((bool)renderer && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
				{
					list.Add(new BoundsData(renderer.bounds));
				}
			}
			renderers = list.ToArray();
		}
		if (!string.IsNullOrEmpty(hint))
		{
			this.hint = ((!hint.StartsWith("ingame.") && !hint.StartsWith("general.")) ? hint.ToUpper() : MonoController<LocalizationController>.Instance?.Get(hint).ToUpper());
		}
	}

	public InteractionData(Interaction interaction, Bounds[] targets, string hint = null)
	{
		this.interaction = interaction;
		if (targets != null)
		{
			renderers = new BoundsData[targets.Length];
			for (int i = 0; i < targets.Length; i++)
			{
				renderers[i] = new BoundsData(targets[i]);
			}
		}
		if (!string.IsNullOrEmpty(hint))
		{
			this.hint = ((!hint.StartsWith("ingame.") && !hint.StartsWith("general.")) ? hint.ToUpper() : MonoController<LocalizationController>.Instance?.Get(hint).ToUpper());
		}
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		InteractionData interactionData = (InteractionData)obj;
		if (interaction == interactionData.interaction && renderers == interactionData.renderers)
		{
			return hint == interactionData.hint;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (interaction, renderers, hint).GetHashCode();
	}

	public static bool operator ==(InteractionData a, InteractionData b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return a.Equals(b);
	}

	public static bool operator !=(InteractionData a, InteractionData b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return $"{interaction} - {hint} ({renderers.Length})";
	}
}
