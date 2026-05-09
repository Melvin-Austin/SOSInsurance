using UnityEngine;
using UnityEngine.Scripting;

namespace HyenaQuest;

[Preserve]
[CurseType(CURSE_TYPE.PIRATE)]
public class PirateCurse : Curse
{
	private static readonly float DRUNK_SWAY_AMOUNT = 0.1f;

	private static readonly float DRUNK_STUMBLE_AMOUNT = 0.1f;

	private static readonly float DRUNK_CAMERA_TILT = 12f;

	private static readonly float DRUNK_CAMERA_TILT_SPEED = 0.2f;

	private static readonly float DRUNK_TRANSITION_SPEED = 0.8f;

	private float _drunkPhase;

	private Vector3 _drunkDirection;

	private Vector3 _targetDrunkDirection;

	private float _lastJump;

	public PirateCurse(entity_player owner, bool server, params object[] args)
		: base(owner, server)
	{
	}

	public override void OnCurseStart(bool server)
	{
		if (!server && IsOwner())
		{
			_drunkDirection = Random.insideUnitSphere;
			_targetDrunkDirection = Random.insideUnitSphere;
			NetController<ShakeController>.Instance?.LocalShake(ShakeMode.SHAKE_ALL);
			NetController<SoundController>.Instance?.PlaySound("Ingame/Player/Damage/Curse/curse.ogg", new AudioData
			{
				pitch = Random.Range(0.8f, 1.2f),
				volume = 0.5f
			});
			MonoController<UIController>.Instance?.SetFade(fadeIn: false, 1.8f);
			MonoController<PostProcessController>.Instance?.SetDepthOfField(3f, 5f);
		}
	}

	public override void OnTick(bool server)
	{
		if (server || !IsOwner() || !_owner || _owner.IsDead())
		{
			return;
		}
		entity_player_movement movement = _owner.GetMovement();
		if ((bool)movement && (bool)_owner.GetCamera() && (bool)SDK.MainCamera)
		{
			if (movement.IsJumping() && Time.time > _lastJump)
			{
				_lastJump = Time.time + 1.4f;
				NetController<SoundController>.Instance?.PlaySound($"Ingame/Props/Special/Pirate/woah_{Random.Range(0, 4)}.ogg", new AudioData
				{
					volume = 0.8f,
					mixer = SoundMixer.CURSES
				});
			}
			_drunkPhase += Time.deltaTime * 2f;
			_drunkDirection = Vector3.Lerp(_drunkDirection, _targetDrunkDirection, Time.deltaTime * DRUNK_TRANSITION_SPEED);
			if (Vector3.Distance(_drunkDirection, _targetDrunkDirection) < 0.3f)
			{
				_targetDrunkDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)).normalized;
			}
			Vector3 movementDirection = movement.GetMovementDirection();
			float num = Mathf.Sin(_drunkPhase * 1.3f) * DRUNK_SWAY_AMOUNT;
			float num2 = Mathf.Cos(_drunkPhase * 0.8f) * DRUNK_SWAY_AMOUNT;
			Vector3 vector = movement.transform.right * num + movement.transform.forward * num2;
			Vector3 vector2 = _drunkDirection * DRUNK_STUMBLE_AMOUNT;
			movement.SetMovementDirection(movementDirection + vector + vector2);
			Transform transform = SDK.MainCamera.transform;
			float z = Mathf.Sin(_drunkPhase * 0.9f) * DRUNK_CAMERA_TILT;
			Vector3 eulerAngles = transform.localRotation.eulerAngles;
			transform.localRotation = Quaternion.Slerp(b: Quaternion.Euler(eulerAngles.x, eulerAngles.y, z), a: transform.localRotation, t: Time.deltaTime * DRUNK_CAMERA_TILT_SPEED);
		}
	}

	public override void OnCurseEnd(bool server)
	{
		if (!server && IsOwner())
		{
			MonoController<PostProcessController>.Instance?.ResetDepthOfField();
			MonoController<UIController>.Instance?.SetFade(fadeIn: false, 1.8f);
			if ((bool)SDK.MainCamera)
			{
				SDK.MainCamera.transform.localRotation = Quaternion.identity;
			}
		}
	}
}
