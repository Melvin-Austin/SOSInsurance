using UnityEngine;
using UnityEngine.Video;

namespace HyenaQuest;

[RequireComponent(typeof(VideoPlayer))]
public class entity_tv : MonoBehaviour
{
	public GameEvent onVideoEnd = new GameEvent();

	private AudioSource _audioSource;

	private VideoPlayer _videoPlayer;

	private bool _autoStop;

	private bool _playing;

	public void Awake()
	{
		_audioSource = GetComponentInChildren<AudioSource>(includeInactive: true);
		if (!_audioSource)
		{
			throw new UnityException("Missing audio source");
		}
		_videoPlayer = GetComponent<VideoPlayer>();
		if (!_videoPlayer)
		{
			throw new UnityException("VideoPlayer component not found");
		}
		_videoPlayer.playOnAwake = false;
		_videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
		_videoPlayer.SetTargetAudioSource(0, _audioSource);
		_videoPlayer.loopPointReached += OnLoopPointReached;
	}

	public void SetPlaying(bool play, bool autoStop = true)
	{
		if (!_videoPlayer)
		{
			throw new UnityException("VideoPlayer component not found");
		}
		_autoStop = autoStop;
		_videoPlayer.isLooping = !autoStop;
		if (play)
		{
			Play();
		}
		else
		{
			Stop();
		}
		_videoPlayer.SetTargetAudioSource(0, _audioSource);
	}

	public void SetVideoClip(VideoClip clip)
	{
		if (!_videoPlayer)
		{
			throw new UnityException("VideoPlayer component not found");
		}
		if (!clip)
		{
			throw new UnityException("Invalid video clip");
		}
		_videoPlayer.clip = clip;
	}

	public void OnDestroy()
	{
		if ((bool)_videoPlayer)
		{
			_videoPlayer.loopPointReached -= OnLoopPointReached;
			_videoPlayer.Stop();
			util_render_target.ClearRenderTarget(_videoPlayer.targetTexture);
		}
	}

	private void OnLoopPointReached(VideoPlayer source)
	{
		if (_autoStop)
		{
			Stop();
			onVideoEnd?.Invoke();
		}
	}

	private void Stop()
	{
		if (!_videoPlayer)
		{
			throw new UnityException("VideoPlayer component not found");
		}
		_videoPlayer.Stop();
		_playing = false;
		NetController<SoundController>.Instance.Play3DSound("Ingame/Entities/Tv/138115__snakebarney__tv-off-short.ogg", GetAudioPos(), new AudioData
		{
			distance = 3f,
			volume = 0.6f
		});
		util_render_target.ClearRenderTarget(_videoPlayer.targetTexture);
	}

	private void Play()
	{
		if (!_videoPlayer)
		{
			throw new UnityException("VideoPlayer component not found");
		}
		_videoPlayer.time = 0.0;
		_videoPlayer.Play();
		NetController<SoundController>.Instance.Play3DSound(_playing ? "Ingame/Entities/Tv/817045__sadiquecat__light-flip-switch-l-edited-r-raw.ogg" : "Ingame/Entities/Tv/693860__fission9__crt-tv-switches-on.ogg", GetAudioPos(), new AudioData
		{
			distance = 3f,
			volume = 0.6f
		});
		_playing = true;
	}

	private Vector3 GetAudioPos()
	{
		if (!_videoPlayer)
		{
			return base.transform.position;
		}
		AudioSource targetAudioSource = _videoPlayer.GetTargetAudioSource(0);
		if ((bool)targetAudioSource)
		{
			return targetAudioSource.transform.position;
		}
		return base.transform.position;
	}
}
