using UnityEngine;

namespace HyenaQuest;

public class entity_item_cookie : entity_item_pickable
{
	private bool _used;

	private float _useCooldown;

	[Client]
	public override void OnUse(entity_player ply, Collider obj, bool pressing)
	{
		if ((bool)ply && !_used && pressing && !(Time.time < _useCooldown))
		{
			_useCooldown = Time.time + 0.5f;
			if (ply.IsDead() || ply.GetHealth() >= 100)
			{
				NetController<NotificationController>.Instance?.CreateNotification(new NotificationData
				{
					id = "health-full-error",
					text = "ingame.ui.notification.full-health",
					duration = 2f,
					soundEffect = "Ingame/Entities/Terminal/142608__autistic-lucario__error.ogg",
					soundVolume = 0.05f
				});
			}
			else
			{
				NetController<ShakeController>.Instance?.LocalShake(ShakeMode.SHAKE_ALL, 0.1f, 0.05f);
				MonoController<UIController>.Instance?.SetFade(fadeIn: false, new Color(0f, 0.2f, 0f, 0.4f), 3f);
				NetController<SoundController>.Instance?.PlaySound("Ingame/Items/Medkit/heal.ogg", new AudioData
				{
					volume = 0.5f,
					pitch = Random.Range(0.8f, 1.2f)
				});
				_used = true;
				ply.PlayTaunt(PlayerTauntAnim.EAT, 0.7f);
				ply.AddHealth(25);
				DestroyRPC();
			}
		}
	}

	[Server]
	public override void Destroy()
	{
		if ((bool)_ownerPlayer)
		{
			NetController<SoundController>.Instance?.Play3DSound($"Ingame/Items/Cookie/cookie_{Random.Range(0, 3)}.ogg", _ownerPlayer.transform.position, new AudioData
			{
				distance = 3f,
				parent = _ownerPlayer
			}, broadcast: true);
		}
		base.Destroy();
	}

	public override string GetID()
	{
		return "item_cookie";
	}

	protected override void __initializeVariables()
	{
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_item_cookie";
	}
}
