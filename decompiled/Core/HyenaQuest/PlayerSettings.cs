using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyenaQuest;

[Serializable]
public struct PlayerSettings
{
	public PlayerTargetFramerate targetFrameRate;

	public FullScreenMode screenMode;

	public SerializableResolution screenResolution;

	public bool vsyncEnabled;

	public int monitor;

	public float brightness;

	public float fov;

	public ShadowQuality shadow;

	public bool configured;

	public bool crosshair;

	public bool disableVibration;

	public ulong accessories;

	public string keybinds;

	public float masterVolume;

	public float musicVolume;

	public float sfxVolume;

	public float microphoneVolume;

	public bool muteOnUnfocus;

	public LOCALE localization;

	public float mouseSensitivity;

	public float physgunRotateSensitivity;

	public bool invertPhysRotationX;

	public bool invertPhysRotationY;

	public bool invertMouseY;

	public uint[] bestTimes;

	public int microphoneDevice;

	public VoiceChatMode microphoneMode;

	public Dictionary<string, float> playerMicrophoneSettings;
}
