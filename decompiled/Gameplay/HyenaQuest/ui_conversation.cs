using System.Collections.Generic;
using FailCake;
using Febucci.TextAnimatorCore.Text;
using Febucci.TextAnimatorForUnity;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace HyenaQuest;

public class ui_conversation : MonoBehaviour
{
	public float talkSpeed = 0.3f;

	public float cooldown = 1.25f;

	public GameEvent OnComplete = new GameEvent();

	private readonly Queue<Conversation> _talkQueue = new Queue<Conversation>();

	private util_timer _cooldownTimer;

	private util_timer _fallbackTimer;

	private TextMeshPro _textMeshPro;

	private TypewriterComponent _text;

	[CanBeNull]
	private Conversation _currentChat;

	private Vector3 _currentChatPosition;

	public void Awake()
	{
		_text = GetComponentInChildren<TypewriterComponent>(includeInactive: true);
		if (!_text)
		{
			throw new UnityException("TypewriterByCharacter component not found");
		}
		_textMeshPro = _text.GetComponent<TextMeshPro>();
		if (!_textMeshPro)
		{
			throw new UnityException("TextMeshPro component not found");
		}
		_text.onTextShowed.AddListener(OnTextShowed);
		_text.onCharacterVisible.AddListener(OnCharacterVisible);
		Clear();
	}

	public void OnDestroy()
	{
		_cooldownTimer?.Stop();
		_fallbackTimer?.Stop();
		if ((bool)_text)
		{
			_text.onTextShowed.RemoveAllListeners();
			_text.onCharacterVisible.RemoveAllListeners();
		}
	}

	public void Clear()
	{
		_cooldownTimer?.Stop();
		_fallbackTimer?.Stop();
		_talkQueue.Clear();
		_currentChat = null;
		_currentChatPosition = Vector3.zero;
		if ((bool)_text)
		{
			_text.ShowText("");
			_text.StopShowingText();
		}
	}

	public void QueueConversation(Conversation conv)
	{
		if (conv.text.Length == 0)
		{
			throw new UnityException("Conversation text is empty");
		}
		_talkQueue.Enqueue(conv);
		NextConversation();
	}

	private void Update()
	{
		if ((bool)SDK.MainCamera && !(_currentChatPosition == Vector3.zero))
		{
			float num = Vector3.Distance(SDK.MainCamera.transform.position, _currentChatPosition);
			float alpha = Mathf.Clamp01(1f - num / 5f);
			_textMeshPro.alpha = alpha;
		}
	}

	private void NextConversation()
	{
		if (_currentChat != null)
		{
			return;
		}
		if (_talkQueue.Count == 0)
		{
			OnComplete?.Invoke();
			Clear();
			return;
		}
		_currentChat = _talkQueue.Dequeue();
		if (_currentChat == null)
		{
			NextConversation();
			return;
		}
		_currentChatPosition = _currentChat.position;
		_text.SetTypewriterSpeed(talkSpeed);
		_text.ShowText(_currentChat.text);
		_fallbackTimer?.Stop();
		_fallbackTimer = util_timer.Simple(10f, OnTextShowed);
	}

	private void OnTextShowed()
	{
		_currentChat = null;
		_fallbackTimer?.Stop();
		_cooldownTimer?.Stop();
		_cooldownTimer = util_timer.Simple(cooldown, NextConversation);
	}

	private void OnCharacterVisible(CharacterData ca)
	{
		if (_currentChat != null && ca.info.character != ' ')
		{
			AudioData data = new AudioData
			{
				pitch = Random.Range(_currentChat.minPitch, _currentChat.maxPitch),
				volume = 0.1f
			};
			if (_currentChat.position == Vector3.zero)
			{
				NetController<SoundController>.Instance.PlaySound($"NPCS/Chat/untitled-{Random.Range(1, 11)}.ogg", data);
			}
			else
			{
				NetController<SoundController>.Instance.Play3DSound($"NPCS/Chat/untitled-{Random.Range(1, 11)}.ogg", _currentChat.position, data);
			}
		}
	}
}
