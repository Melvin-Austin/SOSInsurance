using System;
using System.Collections.Generic;
using FailCake;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using ZLinq;

namespace HyenaQuest;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-70)]
[RequireComponent(typeof(NetworkObject))]
public class EndController : NetController<EndController>
{
	private static readonly float OUTRO_VOLUME = 0.35f;

	public GameObject endGameUI;

	public TextMeshProUGUI endGamePlayerText;

	public TextMeshProUGUI endGamePlayerSubText;

	public GameObject endGameStatusUI;

	public TextMeshProUGUI endGameDebtText;

	public TextMeshProUGUI endGameStatusText;

	public GameEvent<bool> OnReportComplete = new GameEvent<bool>();

	public GameEvent<bool> OnReportStart = new GameEvent<bool>();

	private Queue<Report> _reportPlayers;

	private util_timer _reportTimer;

	private util_timer _animatorTimer;

	private util_fade_timer _textFadeTimer;

	private bool _textDirection;

	private int _lastMugshotID;

	private readonly NetVar<Report> _mugshotPlayer = new NetVar<Report>();

	public new void Awake()
	{
		base.Awake();
		if (!endGameUI)
		{
			throw new UnityException("EndController requires endGameUI to be set.");
		}
		endGameUI.SetActive(value: false);
		if (!endGamePlayerText)
		{
			throw new UnityException("EndController requires endGamePlayerText to be set.");
		}
		endGamePlayerText.color = Color.black;
		if (!endGamePlayerSubText)
		{
			throw new UnityException("EndController requires endGamePlayerSubText to be set.");
		}
		endGamePlayerSubText.color = Color.white;
		if (!endGameStatusUI)
		{
			throw new UnityException("EndController requires endGameStatusUI to be set.");
		}
		endGameStatusUI.SetActive(value: false);
		if (!endGameStatusText)
		{
			throw new UnityException("EndController requires endGameStatusText to be set.");
		}
		endGameStatusText.color = Color.black;
		endGameStatusText.text = "";
		if (!endGameDebtText)
		{
			throw new UnityException("EndController requires endGameDebtText to be set.");
		}
		endGameDebtText.color = Color.black;
	}

	public new void OnDestroy()
	{
		_reportTimer?.Stop();
		MonoController<LocalizationController>.Instance?.Cleanup("endgame-title");
		base.OnDestroy();
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (base.IsClient)
		{
			NetVar<Report> mugshotPlayer = _mugshotPlayer;
			mugshotPlayer.OnValueChanged = (NetworkVariable<Report>.OnValueChangedDelegate)Delegate.Combine(mugshotPlayer.OnValueChanged, new NetworkVariable<Report>.OnValueChangedDelegate(OnReportUpdated));
		}
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_mugshotPlayer.OnValueChanged = null;
		}
	}

	[Server]
	public void Report(ReportStatus status, Action callback, bool filter = true)
	{
		if (!base.IsServer)
		{
			throw new InvalidOperationException("EndController.Report can only be called on the server.");
		}
		if (callback == null)
		{
			throw new ArgumentNullException("callback", "EndController.Report requires a callback to be provided.");
		}
		OnReportStart?.Invoke(param1: true);
		_reportPlayers = new Queue<Report>();
		if (status != ReportStatus.FAIL_NO_ALIVE)
		{
			IList<entity_player> list = MonoController<PlayerController>.Instance.GetAlivePlayers().ShuffleWithNew();
			if (list.Count > 0)
			{
				if (!NetController<StatsController>.Instance)
				{
					throw new UnityException("StatsController instance is null. Cannot generate reports.");
				}
				List<byte> usedPlayerIDs = new List<byte>();
				(byte, int) mostScrapper = NetController<StatsController>.Instance.GetMostScrapper(null);
				if (mostScrapper.Item1 != byte.MaxValue)
				{
					_reportPlayers.Enqueue(new Report
					{
						playerID = mostScrapper.Item1,
						title = $"ingame.ui.endgame.most-scrap||{mostScrapper.Item2}"
					});
					if (filter)
					{
						usedPlayerIDs.Add(mostScrapper.Item1);
					}
				}
				(byte, int) mostDeliverer = NetController<StatsController>.Instance.GetMostDeliverer(usedPlayerIDs.ToArray());
				if (mostDeliverer.Item1 != byte.MaxValue)
				{
					_reportPlayers.Enqueue(new Report
					{
						playerID = mostDeliverer.Item1,
						title = $"ingame.ui.endgame.most-deliveries||{mostDeliverer.Item2}"
					});
					if (filter)
					{
						usedPlayerIDs.Add(mostDeliverer.Item1);
					}
				}
				if (_reportPlayers.Count == 1)
				{
					List<entity_player> list2 = (from p in list.AsValueEnumerable()
						where (bool)p && !usedPlayerIDs.Contains(p.GetPlayerID())
						select p).ToList();
					if (list2.Count > 0)
					{
						entity_player entity_player2 = list2[UnityEngine.Random.Range(0, list2.Count)];
						_reportPlayers.Enqueue(new Report
						{
							playerID = entity_player2.GetPlayerID(),
							title = "ingame.ui.endgame.mvp"
						});
					}
				}
			}
		}
		if (status != 0)
		{
			_reportPlayers.Enqueue(new Report
			{
				playerID = byte.MaxValue,
				title = "ingame.ui.endgame.result.fail"
			});
		}
		else
		{
			_reportPlayers.Enqueue(new Report
			{
				playerID = 254,
				title = "ingame.ui.endgame.result.success"
			});
		}
		FreezePlayers(freeze: true);
		_mugshotPlayer.Value = _reportPlayers.Dequeue();
		if (_reportPlayers.Count == 0)
		{
			_reportTimer?.Stop();
			_reportTimer = util_timer.Simple(5f, delegate
			{
				_mugshotPlayer.Value = null;
				FreezePlayers(freeze: false);
				callback?.Invoke();
			});
			return;
		}
		_reportTimer?.Stop();
		_reportTimer = util_timer.Create(_reportPlayers.Count + 1, 4f, delegate
		{
			if (_reportPlayers.Count != 0)
			{
				_mugshotPlayer.Value = _reportPlayers.Dequeue();
				if (_reportPlayers.Count == 0)
				{
					_reportTimer.SetDelay(5f);
				}
			}
		}, delegate
		{
			_mugshotPlayer.Value = null;
			FreezePlayers(freeze: false);
			OnReportComplete?.Invoke(param1: true);
			callback?.Invoke();
		});
	}

	[Server]
	private void FreezePlayers(bool freeze)
	{
		if (!base.IsServer)
		{
			throw new UnityException("FreezePlayers can only be called on the server.");
		}
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer)
			{
				allPlayer.SetFreeze(freeze);
			}
		}
	}

	[Client]
	private void OnlyRenderPlayer(byte render)
	{
		if (!base.IsClient)
		{
			throw new UnityException("RenderOnlyPlayer can only be called on the client.");
		}
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer && !allPlayer.IsDead())
			{
				bool flag = allPlayer.GetPlayerID() == render;
				allPlayer.RenderPlayerHead(flag);
				allPlayer.SetRenderersLayer("EndGame", flag);
				allPlayer.SetRenderers(flag);
				allPlayer.CancelGrabbing();
			}
		}
	}

	private void ResetReport()
	{
		if (!MonoController<PlayerController>.Instance)
		{
			return;
		}
		_animatorTimer?.Stop();
		_textFadeTimer?.Stop();
		MonoController<UIController>.Instance?.HideHUD(hidden: false);
		MonoController<LocalizationController>.Instance?.Cleanup("endgame-title");
		endGameUI?.SetActive(value: false);
		endGameStatusUI?.SetActive(value: false);
		if ((bool)PlayerController.LOCAL)
		{
			PlayerController.LOCAL.CancelTaunt();
			PlayerController.LOCAL.RenderPlayerHead(render: false);
			PlayerController.LOCAL.GetCamera()?.ResetCamera();
		}
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer)
			{
				allPlayer.SetRenderersLayer("EndGame", render: false);
				allPlayer.SetRenderers(state: true);
				allPlayer.CancelGrabbing();
				allPlayer.CancelTaunt();
			}
		}
		RenderSettings.ambientLight = Color.black;
		OnReportComplete?.Invoke(param1: false);
	}

	private void OnReportUpdated(Report oldReport, Report report)
	{
		if (!base.IsClient)
		{
			throw new UnityException("OnReportUpdated can only be called on the client.");
		}
		if (!MonoController<LocalizationController>.Instance)
		{
			throw new UnityException("Missing LocalizationController");
		}
		if (oldReport == null && report != null)
		{
			OnReportStart?.Invoke(param1: false);
		}
		_animatorTimer?.Stop();
		_textFadeTimer?.Stop();
		if (report == null)
		{
			ResetReport();
			return;
		}
		byte playerID = report.playerID;
		if (playerID == byte.MaxValue || playerID == 254)
		{
			endGameUI.SetActive(value: false);
			endGameStatusText.text = "";
			endGameStatusText.color = ((report.playerID == byte.MaxValue) ? (Color.red * 3f) : (new Color(0.7f, 0f, 0.35f, 1f) * 3f));
			endGameStatusUI.SetActive(value: true);
			NetController<SoundController>.Instance.PlaySound((report.playerID == byte.MaxValue) ? "Ingame/EndGame/outro_suspense_fail.ogg" : "Ingame/EndGame/outro_suspense_success_1.ogg", new AudioData
			{
				volume = OUTRO_VOLUME,
				mixer = SoundMixer.MUSIC
			});
			_textFadeTimer = util_fade_timer.Fade(0.15f, 0.7f, 1f, delegate(float f)
			{
				endGameDebtText.transform.localScale = Vector3.one * f;
				endGameDebtText.transform.localEulerAngles = new Vector3(0f, 0f, f * 5f);
			});
			_animatorTimer?.Stop();
			_animatorTimer = util_timer.Simple(3.8f, delegate
			{
				string text = report.title.ToString();
				if (text.StartsWith("ingame.ui.endgame"))
				{
					text = MonoController<LocalizationController>.Instance.Get(text);
				}
				endGameStatusText.text = text;
			});
			return;
		}
		bool flag = UnityEngine.Random.Range(0, 2) == 0;
		endGameUI.SetActive(value: true);
		RenderSettings.ambientLight = Color.white;
		entity_player playerEntityByID = MonoController<PlayerController>.Instance.GetPlayerEntityByID(report.playerID);
		if (!playerEntityByID)
		{
			return;
		}
		int num;
		do
		{
			num = UnityEngine.Random.Range(0, 4);
		}
		while (num == _lastMugshotID);
		_lastMugshotID = num;
		playerEntityByID.CancelTaunt();
		OnlyRenderPlayer(playerEntityByID.GetPlayerID());
		playerEntityByID.PlayTaunt((PlayerTauntAnim)(200 + num), -1f, UnityEngine.Random.Range(0.1f, 0.8f));
		endGamePlayerText.text = playerEntityByID.GetPlayerName();
		string text2 = report.title.ToString();
		if (text2.StartsWith("ingame.ui.endgame"))
		{
			MonoController<LocalizationController>.Instance.Get("endgame-title", text2, delegate(string s)
			{
				if ((bool)endGamePlayerSubText)
				{
					if (s.Contains("<##>"))
					{
						string[] array = s.Split(new string[1] { "<##>" }, StringSplitOptions.None);
						endGamePlayerSubText.text = array[UnityEngine.Random.Range(0, array.Length)].Trim();
					}
					else
					{
						endGamePlayerSubText.text = s;
					}
				}
			});
		}
		else
		{
			endGamePlayerSubText.text = text2;
		}
		endGamePlayerText.transform.localPosition = new Vector3(0f, UnityEngine.Random.Range(-20, 0), 0f);
		endGamePlayerText.transform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-10, 10));
		endGamePlayerSubText.transform.localPosition = new Vector3(0f, -52f, 0f);
		endGamePlayerSubText.transform.parent.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-1, 1));
		endGameUI.transform.localScale = (flag ? new Vector3(-1f, 1f, 1f) : Vector3.one);
		endGamePlayerText.transform.localScale = (flag ? new Vector3(-1f, 1f, 1f) : Vector3.one);
		_textDirection = !_textDirection;
		Vector3 textDirection = (_textDirection ? Vector3.right : Vector3.left);
		_textFadeTimer = util_fade_timer.Fade(0.25f, 0f, 2f, delegate
		{
			endGamePlayerText.transform.localPosition += textDirection * (Time.deltaTime * 20f);
			endGamePlayerSubText.transform.localPosition -= textDirection * (Time.deltaTime * 20f);
		});
		entity_player lOCAL = PlayerController.LOCAL;
		if (!lOCAL)
		{
			throw new UnityException("Local player is null.");
		}
		entity_player_camera camera = lOCAL.GetCamera();
		if (!camera)
		{
			throw new UnityException("Local player camera is null.");
		}
		float num2 = ((UnityEngine.Random.Range(0, 2) == 0) ? (-0.7f) : 0.7f);
		float angle = UnityEngine.Random.Range(-20f, 20f);
		Vector3 position = playerEntityByID.transform.position;
		Vector3 forward = playerEntityByID.transform.forward;
		Vector3 up = playerEntityByID.transform.up;
		Vector3 vector = Quaternion.AngleAxis(0f, Vector3.up) * forward;
		Vector3 vector2 = position + Vector3.up * 0.8f + vector * 2f + up * num2;
		Quaternion rotation = Quaternion.LookRotation(position + Vector3.up * 0.8f - vector2) * Quaternion.AngleAxis(angle, Vector3.forward);
		camera.RenderPlayerOnly(render: true);
		camera.ForceLookAt(vector2 + playerEntityByID.transform.right * (flag ? (-0.8f) : 0.8f), rotation);
		MonoController<UIController>.Instance?.HideHUD(hidden: true);
		NetController<SoundController>.Instance.PlaySound($"Ingame/EndGame/outro_{UnityEngine.Random.Range(0, 3)}_2.ogg", new AudioData
		{
			volume = OUTRO_VOLUME,
			mixer = SoundMixer.MUSIC
		});
	}

	protected override void __initializeVariables()
	{
		if (_mugshotPlayer == null)
		{
			throw new Exception("EndController._mugshotPlayer cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_mugshotPlayer.Initialize(this);
		__nameNetworkVariable(_mugshotPlayer, "_mugshotPlayer");
		NetworkVariableFields.Add(_mugshotPlayer);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "EndController";
	}
}
