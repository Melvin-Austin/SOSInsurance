using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace HyenaQuest;

public class entity_phys_prop_scrap_tv : entity_phys_prop_scrap
{
	public VideoPlayer videoPlayer;

	public List<VideoClip> videoClips = new List<VideoClip>();

	public RawImage videoDisplay;

	private RenderTexture _videoRenderTexture;

	private readonly NetVar<byte> _videoIndex = new NetVar<byte>(byte.MaxValue);

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer && UnityEngine.Random.Range(0, 100) < 25)
		{
			_videoIndex.Value = (byte)UnityEngine.Random.Range(0, videoClips.Count);
		}
	}

	public override void OnDestroy()
	{
		if ((bool)_videoRenderTexture)
		{
			_videoRenderTexture.Release();
			UnityEngine.Object.Destroy(_videoRenderTexture);
			_videoRenderTexture = null;
		}
		base.OnDestroy();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_videoIndex.RegisterOnValueChanged(delegate(byte oldValue, byte newValue)
		{
			if (oldValue != newValue && newValue != byte.MaxValue && (bool)videoPlayer)
			{
				videoPlayer.clip = videoClips[newValue];
				videoPlayer.isLooping = true;
				videoPlayer.renderMode = VideoRenderMode.RenderTexture;
				RenderTexture renderTexture = CreateVideoRenderTexture();
				videoPlayer.targetTexture = renderTexture;
				videoPlayer.Play();
				videoDisplay.gameObject.SetActive(value: true);
				videoDisplay.texture = renderTexture;
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_videoIndex.OnValueChanged = null;
		}
	}

	protected override void Init()
	{
		base.Init();
		if (!videoPlayer)
		{
			throw new UnityException("VideoPlayer is not assigned");
		}
		if (videoClips.Count == 0)
		{
			throw new UnityException("No video clips assigned to the TV");
		}
		if (!videoDisplay)
		{
			throw new UnityException("Video Display (RawImage) is not assigned");
		}
	}

	private RenderTexture CreateVideoRenderTexture()
	{
		if ((bool)_videoRenderTexture && _videoRenderTexture.IsCreated())
		{
			return _videoRenderTexture;
		}
		_videoRenderTexture = new RenderTexture(512, 512, 16);
		_videoRenderTexture.Create();
		return _videoRenderTexture;
	}

	protected override void __initializeVariables()
	{
		if (_videoIndex == null)
		{
			throw new Exception("entity_phys_prop_scrap_tv._videoIndex cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_videoIndex.Initialize(this);
		__nameNetworkVariable(_videoIndex, "_videoIndex");
		NetworkVariableFields.Add(_videoIndex);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_phys_prop_scrap_tv";
	}
}
