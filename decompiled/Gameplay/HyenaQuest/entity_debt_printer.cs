using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FailCake;
using Unity.Netcode;
using UnityEngine;

namespace HyenaQuest;

[RequireComponent(typeof(NetworkObject))]
public class entity_debt_printer : NetworkBehaviour
{
	private static readonly float PRINT_SPEED = 0.2f;

	public GameObject debtReceiptPrefab;

	public entity_led printerLED;

	private AudioSource _audioSource;

	private util_timer _printTimer;

	private readonly List<entity_prop_debt_receipt> _printedReceipts = new List<entity_prop_debt_receipt>();

	private readonly NetVar<bool> _printing = new NetVar<bool>(value: false);

	public void Awake()
	{
		if (!debtReceiptPrefab)
		{
			throw new UnityException("Missing debtReceiptPrefab");
		}
		if (!printerLED)
		{
			throw new UnityException("Missing printerLED");
		}
		_audioSource = GetComponent<AudioSource>();
		if (!_audioSource)
		{
			throw new UnityException("Missing printerBG AudioSource");
		}
		_audioSource.Stop();
		_audioSource.loop = true;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (base.IsServer)
		{
			CoreController.WaitFor(delegate(CurrencyController currCtrl)
			{
				currCtrl.OnDebtChanged += new Action<int, bool, bool>(OnDebtChanged);
			});
		}
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (base.IsServer)
		{
			_printTimer?.Stop();
			CleanupPrintedReceipts();
			if ((bool)NetController<CurrencyController>.Instance)
			{
				NetController<CurrencyController>.Instance.OnDebtChanged -= new Action<int, bool, bool>(OnDebtChanged);
			}
		}
	}

	private void OnDebtChanged(int debt, bool server, bool set)
	{
		if (server && set && debt > 0)
		{
			CreateDebtReceipt(debt);
		}
	}

	protected override void OnNetworkPostSpawn()
	{
		base.OnNetworkPostSpawn();
		if (!base.IsClient)
		{
			return;
		}
		_printing.RegisterOnValueChanged(delegate(bool _, bool newValue)
		{
			if ((bool)_audioSource)
			{
				printerLED.SetActive(newValue);
				if (newValue)
				{
					_audioSource.Play();
				}
				else
				{
					_audioSource.Stop();
				}
			}
		});
	}

	public override void OnNetworkPreDespawn()
	{
		base.OnNetworkPreDespawn();
		if (base.IsClient)
		{
			_printing.OnValueChanged = null;
		}
	}

	[Server]
	private void CleanupPrintedReceipts()
	{
		if (!base.IsServer)
		{
			throw new UnityException("CleanupPrintedReceipts can only be called on the server.");
		}
		if (_printedReceipts.Count == 0)
		{
			return;
		}
		foreach (entity_prop_debt_receipt printedReceipt in _printedReceipts)
		{
			if ((bool)printedReceipt && (bool)printedReceipt.NetworkObject && printedReceipt.NetworkObject.IsSpawned)
			{
				printedReceipt.NetworkObject.Despawn();
			}
		}
		_printedReceipts.Clear();
	}

	[Server]
	private void CreateDebtReceipt(int debt)
	{
		if (!base.IsServer)
		{
			throw new UnityException("CreateDebtReceipt can only be called on the server.");
		}
		if (!debtReceiptPrefab)
		{
			throw new UnityException("Missing debtReceiptPrefab");
		}
		if (_printTimer != null)
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(debtReceiptPrefab, base.transform.position + base.transform.up * 0.02f, base.transform.rotation);
		if (!gameObject)
		{
			return;
		}
		entity_prop_debt_receipt receiptEntity = gameObject.GetComponent<entity_prop_debt_receipt>();
		if (!receiptEntity)
		{
			throw new UnityException("Missing entity_prop_debt_receipt component on debtReceiptPrefab");
		}
		if (!receiptEntity.NetworkObject)
		{
			throw new UnityException("Missing NetworkObject component on entity_prop_debt_receipt");
		}
		if (!receiptEntity.receiptText)
		{
			throw new UnityException("Missing receiptText component on entity_prop_debt_receipt");
		}
		receiptEntity.NetworkObject.Spawn();
		_printedReceipts.Add(receiptEntity);
		var (text, receiptSteps) = GenerateReceiptText(debt);
		if (receiptSteps == null || receiptSteps.Count <= 0)
		{
			return;
		}
		receiptEntity.SetText(text);
		receiptEntity.SetLocked(LOCK_TYPE.LOCKED);
		_printing.Value = true;
		int totalSteps = receiptSteps.Count;
		_printTimer = util_timer.Create(totalSteps, PRINT_SPEED, delegate(int tick)
		{
			if (tick == 0)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/DotMatrix/printer_done.ogg", base.transform.position, new AudioData
				{
					volume = 1f,
					distance = 3f
				}, broadcast: true);
			}
			else if (tick == totalSteps - 1)
			{
				NetController<SoundController>.Instance.Play3DSound("Ingame/DotMatrix/printer_start.ogg", base.transform.position, new AudioData
				{
					volume = 1f,
					distance = 3f
				}, broadcast: true);
			}
			else
			{
				int num = totalSteps - tick;
				if (num < 0 || num >= receiptSteps.Count)
				{
					return;
				}
				switch (receiptSteps[num])
				{
				case PrinterSTEP.INK:
					util_timer.Create(UnityEngine.Random.Range(2, 5), 0.15f, delegate
					{
						NetController<SoundController>.Instance.Play3DSound($"Ingame/DotMatrix/print_{UnityEngine.Random.Range(0, 6)}.ogg", base.transform.position, new AudioData
						{
							volume = 0.15f,
							distance = 3f
						}, broadcast: true);
					});
					break;
				}
				NetController<SoundController>.Instance.Play3DSound($"Ingame/DotMatrix/skip_{tick % 2}.ogg", base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(0.8f, 1.2f),
					volume = 0.15f,
					distance = 3f
				}, broadcast: true);
			}
			if ((bool)receiptEntity)
			{
				receiptEntity.transform.position = base.transform.position - base.transform.up * 0.205f + base.transform.up * (0.01f * (float)tick);
			}
		}, delegate
		{
			if ((bool)receiptEntity)
			{
				receiptEntity.SetLocked(LOCK_TYPE.NONE);
			}
			_printing.Value = false;
			_printTimer = null;
		});
	}

	private (string, List<PrinterSTEP>) GenerateReceiptText(int debt)
	{
		StringBuilder stringBuilder = new StringBuilder(500);
		List<PrinterSTEP> list = new List<PrinterSTEP>(30);
		string[] array = new string[6];
		int[] array2 = new int[6];
		int num = 0;
		int num2 = debt;
		if (debt <= 0)
		{
			return ("", null);
		}
		int[] array3 = Enumerable.Range(0, 23).ToArray();
		array3.Shuffle();
		int num3 = 0;
		for (int i = 0; i < 6; i++)
		{
			if (num2 <= 0)
			{
				break;
			}
			array[i] = $"%%{array3[num3++]}%%";
			int num4;
			if (i == 5 || num2 <= 50)
			{
				num4 = num2;
			}
			else
			{
				int num5 = 6 - i;
				int num6 = num2 / num5;
				int num7 = Math.Max(2, num6 * 2);
				num4 = Math.Min(UnityEngine.Random.Range(1, num7 + 1), num2 - (num5 - 1));
			}
			array2[i] = num4;
			num2 -= num4;
			num++;
		}
		if (num2 > 0 && num > 0)
		{
			array2[num - 1] += num2;
		}
		list.Add(PrinterSTEP.INK);
		list.Add(PrinterSTEP.SKIP);
		for (int j = 0; j < 6; j++)
		{
			bool num8 = j < num;
			bool flag = false;
			if (num8)
			{
				stringBuilder.AppendLine(array[j] ?? "");
				flag = true;
			}
			else
			{
				stringBuilder.AppendLine("");
			}
			list.Add(PrinterSTEP.SKIP);
			if (num8)
			{
				stringBuilder.AppendLine($"<indent=83%><rotate=-90>€</rotate> {array2[j]}</indent>");
			}
			list.Add((!flag) ? PrinterSTEP.SKIP : PrinterSTEP.INK);
		}
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		list.Add(PrinterSTEP.SKIP);
		return (stringBuilder.ToString(), list);
	}

	protected override void __initializeVariables()
	{
		if (_printing == null)
		{
			throw new Exception("entity_debt_printer._printing cannot be null. All NetworkVariableBase instances must be initialized.");
		}
		_printing.Initialize(this);
		__nameNetworkVariable(_printing, "_printing");
		NetworkVariableFields.Add(_printing);
		base.__initializeVariables();
	}

	protected override void __initializeRpcs()
	{
		base.__initializeRpcs();
	}

	protected internal override string __getTypeName()
	{
		return "entity_debt_printer";
	}
}
