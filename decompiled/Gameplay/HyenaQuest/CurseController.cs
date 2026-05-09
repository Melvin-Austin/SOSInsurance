using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[DefaultExecutionOrder(-70)]
public class CurseController : NetController<CurseController>
{
	private static readonly Dictionary<CURSE_TYPE, Type> _curseTypeCache;

	private static readonly Dictionary<CURSE_TYPE, int> _maxStacks;

	private readonly Dictionary<byte, List<Curse>> _curses = new Dictionary<byte, List<Curse>>();

	static CurseController()
	{
		_curseTypeCache = new Dictionary<CURSE_TYPE, Type>();
		_maxStacks = new Dictionary<CURSE_TYPE, int>
		{
			{
				CURSE_TYPE.ABYSS,
				1
			},
			{
				CURSE_TYPE.PIRATE,
				1
			},
			{
				CURSE_TYPE.SLOW,
				3
			}
		};
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			CurseTypeAttribute customAttribute = type.GetCustomAttribute<CurseTypeAttribute>();
			if (customAttribute != null && type.IsSubclassOf(typeof(Curse)))
			{
				_curseTypeCache[customAttribute.Type] = type;
			}
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(PlayerController plyCtrl)
			{
				plyCtrl.OnPlayerRemoved += new Action<entity_player, bool>(OnPlayerRemoved);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer && (bool)MonoController<PlayerController>.Instance)
		{
			MonoController<PlayerController>.Instance.OnPlayerRemoved -= new Action<entity_player, bool>(OnPlayerRemoved);
		}
	}

	public static Curse CreateCurseInstance(CURSE_TYPE type, entity_player owner, bool server, params object[] args)
	{
		if (!owner)
		{
			throw new UnityException("Missing owner");
		}
		if (!_curseTypeCache.TryGetValue(type, out var value))
		{
			throw new UnityException($"No curse class found for type: {type}");
		}
		return (Curse)Activator.CreateInstance(value, owner, server, args);
	}

	[Server]
	public void AddCurse(CURSE_TYPE type, entity_player ply, params object[] args)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!ply)
		{
			throw new UnityException("Missing player");
		}
		byte playerID = ply.GetPlayerID();
		int curseStacks = GetCurseStacks(ply, type);
		if (curseStacks <= 0 || AllowsStack(type, curseStacks))
		{
			Curse curse = CreateCurseInstance(type, ply, server: true, args);
			if (curse == null)
			{
				throw new UnityException($"Failed to instance curse {type}");
			}
			if (!_curses.TryGetValue(playerID, out var value))
			{
				value = new List<Curse>();
				_curses[playerID] = value;
			}
			value.Add(curse);
			ply.AddCurse(curse);
		}
	}

	[Server]
	public void ClearCurses(entity_player ply)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!ply)
		{
			throw new UnityException("Missing player");
		}
		byte playerID = ply.GetPlayerID();
		if (!_curses.TryGetValue(playerID, out var value))
		{
			return;
		}
		foreach (Curse item in value)
		{
			item.MarkForDeletion();
		}
	}

	[Server]
	public void RemoveCurse(CURSE_TYPE curse)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		foreach (entity_player allPlayer in MonoController<PlayerController>.Instance.GetAllPlayers())
		{
			if ((bool)allPlayer)
			{
				RemoveCurse(allPlayer, curse);
			}
		}
	}

	[Server]
	public void RemoveCurse(entity_player ply, CURSE_TYPE curse)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!ply)
		{
			throw new UnityException("Missing player");
		}
		byte playerID = ply.GetPlayerID();
		if (!_curses.TryGetValue(playerID, out var value))
		{
			return;
		}
		foreach (Curse item in value)
		{
			if (item.GetCurseType() == curse)
			{
				item.MarkForDeletion();
			}
		}
	}

	[Server]
	public bool HasCurse(entity_player ply, CURSE_TYPE curse)
	{
		if (!base.IsServer)
		{
			throw new UnityException("Server only");
		}
		if (!ply)
		{
			throw new UnityException("Missing player");
		}
		byte playerID = ply.GetPlayerID();
		if (!_curses.TryGetValue(playerID, out var value))
		{
			return false;
		}
		foreach (Curse item in value)
		{
			if (item.GetCurseType() == curse)
			{
				return true;
			}
		}
		return false;
	}

	public int GetCurseStacks(entity_player ply, CURSE_TYPE curse)
	{
		if (!ply)
		{
			return 0;
		}
		byte playerID = ply.GetPlayerID();
		if (!_curses.TryGetValue(playerID, out var value))
		{
			return 0;
		}
		int num = 0;
		foreach (Curse item in value)
		{
			if (item.GetCurseType() == curse)
			{
				num++;
			}
		}
		return num;
	}

	public static bool AllowsStack(CURSE_TYPE type, int currentStacks)
	{
		if (!_maxStacks.TryGetValue(type, out var value))
		{
			return false;
		}
		return currentStacks < value;
	}

	public void Update()
	{
		if (!base.IsServer)
		{
			return;
		}
		foreach (KeyValuePair<byte, List<Curse>> curse2 in _curses)
		{
			List<Curse> value = curse2.Value;
			if (value == null || value.Count == 0)
			{
				continue;
			}
			for (int num = value.Count - 1; num >= 0; num--)
			{
				Curse curse = value[num];
				if (curse.HasEnded())
				{
					curse.OnCurseEnd(server: true);
					entity_player owner = curse.GetOwner();
					if ((bool)owner && owner.IsSpawned)
					{
						owner.RemoveCurse(curse);
					}
					value.RemoveAt(num);
				}
				else
				{
					curse.OnTick(server: true);
				}
			}
		}
	}

	private void OnPlayerRemoved(entity_player ply, bool server)
	{
		if (server && (bool)ply)
		{
			_curses.Remove(ply.GetPlayerID());
		}
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
		return "CurseController";
	}
}
