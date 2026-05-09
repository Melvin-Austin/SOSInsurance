using UnityEngine;

namespace HyenaQuest;

public class Conversation
{
	public string text;

	public float maxPitch;

	public float minPitch;

	public Vector3 position;

	public Conversation(string text, float minPitch = 1f, float maxPitch = 1f, Vector3 position = default(Vector3))
	{
		this.text = text;
		this.minPitch = minPitch;
		this.maxPitch = maxPitch;
		this.position = position;
	}
}
