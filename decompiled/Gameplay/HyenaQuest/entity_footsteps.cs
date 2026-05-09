using System;
using System.Collections.Generic;
using FailCake;
using FailCake.VMF;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace HyenaQuest;

public class entity_footsteps : MonoBehaviour
{
	private struct ZTriangleData
	{
		public byte materialType;

		public float3 p0;

		public float3 p1;

		public float3 p2;

		public float minY;

		public float maxY;

		public float2 e0;

		public float2 e1;

		public float2 e2;
	}

	[BurstCompile]
	private struct TriangleCacheJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<Vector3> Vertices;

		[ReadOnly]
		public NativeArray<Vector4> UVs;

		[ReadOnly]
		public NativeArray<int> Triangles;

		[ReadOnly]
		public float4x4 LocalToWorldMatrix;

		[ReadOnly]
		public NativeArray<byte> MaterialIndices;

		[ReadOnly]
		public NativeArray<byte> TextureIndices;

		[ReadOnly]
		public NativeArray<byte> MaterialTypes;

		[WriteOnly]
		public NativeArray<ZTriangleData> OutputTriangles;

		public void Execute(int i)
		{
			int num = i * 3;
			if (num + 2 >= Triangles.Length)
			{
				return;
			}
			int num2 = Triangles[num];
			int num3 = Triangles[num + 1];
			int num4 = Triangles[num + 2];
			if (num2 >= Vertices.Length || num3 >= Vertices.Length || num4 >= Vertices.Length || num2 >= UVs.Length || num3 >= UVs.Length || num4 >= UVs.Length)
			{
				return;
			}
			float3 p = math.transform(LocalToWorldMatrix, Vertices[num2]);
			float3 p2 = math.transform(LocalToWorldMatrix, Vertices[num3]);
			float3 p3 = math.transform(LocalToWorldMatrix, Vertices[num4]);
			Vector4 vector = UVs[num2];
			byte b = (byte)vector.z;
			byte b2 = (byte)vector.w;
			byte materialType = 0;
			for (int j = 0; j < MaterialIndices.Length; j++)
			{
				if (MaterialIndices[j] == b && TextureIndices[j] == b2)
				{
					materialType = MaterialTypes[j];
					break;
				}
			}
			ZTriangleData zTriangleData = default(ZTriangleData);
			zTriangleData.materialType = materialType;
			zTriangleData.p0 = p;
			zTriangleData.p1 = p2;
			zTriangleData.p2 = p3;
			zTriangleData.minY = math.min(math.min(p.y, p2.y), p3.y);
			zTriangleData.maxY = math.max(math.max(p.y, p2.y), p3.y);
			zTriangleData.e0 = new float2(p2.x - p.x, p2.z - p.z);
			zTriangleData.e1 = new float2(p3.x - p2.x, p3.z - p2.z);
			zTriangleData.e2 = new float2(p.x - p3.x, p.z - p3.z);
			ZTriangleData value = zTriangleData;
			OutputTriangles[i] = value;
		}
	}

	[BurstCompile]
	private struct TriangleHitJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<ZTriangleData> Triangles;

		[ReadOnly]
		public float3 HitPoint;

		[ReadOnly]
		public float YOffset;

		[WriteOnly]
		public NativeArray<bool> Results;

		[WriteOnly]
		public NativeArray<byte> Materials;

		public void Execute(int i)
		{
			ZTriangleData zTriangleData = Triangles[i];
			if (HitPoint.y < zTriangleData.minY - YOffset || HitPoint.y > zTriangleData.maxY + YOffset)
			{
				Results[i] = false;
				return;
			}
			float2 @float = new float2(HitPoint.x, HitPoint.z);
			float num = math.dot(y: @float - new float2(zTriangleData.p0.x, zTriangleData.p0.z), x: new float2(0f - zTriangleData.e0.y, zTriangleData.e0.x));
			float num2 = math.dot(new float2(0f - zTriangleData.e1.y, zTriangleData.e1.x), @float - new float2(zTriangleData.p1.x, zTriangleData.p1.z));
			float num3 = math.dot(new float2(0f - zTriangleData.e2.y, zTriangleData.e2.x), @float - new float2(zTriangleData.p2.x, zTriangleData.p2.z));
			bool flag = (num <= 0f && num2 <= 0f && num3 <= 0f) || (num >= 0f && num2 >= 0f && num3 >= 0f);
			Results[i] = flag;
			Materials[i] = (byte)(flag ? zTriangleData.materialType : 0);
		}
	}

	[Serializable]
	private class AudioClipGroup
	{
		public AudioClip[] stepClips;

		public AudioClip[] landClips;
	}

	private static readonly int CLEAR_CACHE_TIMER = 30;

	[Header("Settings")]
	public float baseStepSpeed = 0.89f;

	public float footstepRaycastDistance = 0.25f;

	public float minPitch = 0.9f;

	public float maxPitch = 1.2f;

	public float minVolume = 0.25f;

	public float maxVolume = 0.35f;

	public float maxHearingDistance = 3f;

	[Header("Sound Effects")]
	[SerializeField]
	private AudioClipGroup tileGroup;

	[SerializeField]
	private AudioClipGroup metalGroup;

	[SerializeField]
	private AudioClipGroup concreteGroup;

	[SerializeField]
	private AudioClipGroup woodGroup;

	[SerializeField]
	private AudioClipGroup stoneGroup;

	[SerializeField]
	private AudioClipGroup grassGroup;

	[SerializeField]
	private AudioClipGroup sandGroup;

	[SerializeField]
	private AudioClipGroup carpetGroup;

	private float _footstepTimer;

	private GameObject _cacheObj;

	private MeshFilter _cacheMesh;

	private SerializedDictionary<byte, SerializedDictionary<byte, VMFMaterial>> _cacheData;

	private entity_player_movement _characterMovement;

	private LayerMask _mask;

	private int _lastFootstep;

	private util_timer _clearCache;

	private readonly Dictionary<GameObject, NativeArray<ZTriangleData>> _jobTriangleCache = new Dictionary<GameObject, NativeArray<ZTriangleData>>(8);

	private readonly RaycastHit[] _hitResults = new RaycastHit[1];

	public void Awake()
	{
		_mask = LayerMask.GetMask("entity_ground");
		_characterMovement = GetComponentInParent<entity_player_movement>(includeInactive: true);
		if (!_characterMovement)
		{
			throw new UnityException("Invalid entity_player, missing CharacterMovement object");
		}
		_clearCache = util_timer.Create(-1, CLEAR_CACHE_TIMER, delegate
		{
			Clear();
		});
		_characterMovement.Landed += OnLand;
	}

	public void OnDestroy()
	{
		_clearCache?.Stop();
		Clear();
		if ((bool)_characterMovement)
		{
			_characterMovement.Landed -= OnLand;
		}
	}

	public void Update()
	{
		if (!_characterMovement)
		{
			return;
		}
		float magnitude = _characterMovement.velocity.magnitude;
		if (!(magnitude <= 0.5f) && _characterMovement.IsGrounded() && !_characterMovement.IsInWaterPhysicsVolume())
		{
			_footstepTimer -= Mathf.Max(magnitude, 1.2f) * Time.deltaTime;
			if (_footstepTimer <= 0f)
			{
				PlayFootstepSound(GetFootstepMaterial(), isLanding: false);
				_footstepTimer = baseStepSpeed;
			}
		}
	}

	private void Clear()
	{
		foreach (KeyValuePair<GameObject, NativeArray<ZTriangleData>> item in _jobTriangleCache)
		{
			if (item.Value.IsCreated)
			{
				item.Value.Dispose();
			}
		}
		_jobTriangleCache.Clear();
		_cacheObj = null;
		_cacheMesh = null;
		_cacheData = null;
		_lastFootstep = 0;
		_footstepTimer = 0f;
	}

	private void OnLand(Vector3 velocity)
	{
		PlayFootstepSound(GetFootstepMaterial(footstepRaycastDistance), isLanding: true);
	}

	private void PlayFootstepSound(VMFMaterial material, bool isLanding)
	{
		AudioClipGroup audioGroup = GetAudioGroup(material);
		AudioClip[] array = (isLanding ? audioGroup.landClips : audioGroup.stepClips);
		if (array != null && array.Length != 0)
		{
			int num;
			do
			{
				num = UnityEngine.Random.Range(0, array.Length);
			}
			while (num == _lastFootstep && array.Length > 1);
			_lastFootstep = num;
			AudioClip audioClip = array[num];
			if ((bool)audioClip)
			{
				NetController<SoundController>.Instance.Play3DSound(audioClip, base.transform.position, new AudioData
				{
					pitch = UnityEngine.Random.Range(minPitch, maxPitch),
					distance = maxHearingDistance,
					volume = (_characterMovement.IsCrouching() ? UnityEngine.Random.Range(0.05f, 0.15f) : (isLanding ? UnityEngine.Random.Range(0.5f, 0.7f) : UnityEngine.Random.Range(minVolume, maxVolume))),
					parent = PlayerController.LOCAL
				}, broadcast: true);
			}
		}
	}

	private AudioClipGroup GetAudioGroup(VMFMaterial material)
	{
		return material switch
		{
			VMFMaterial.METAL => metalGroup, 
			VMFMaterial.CONCRETE => concreteGroup, 
			VMFMaterial.WOOD => woodGroup, 
			VMFMaterial.STONE => stoneGroup, 
			VMFMaterial.GRASS => grassGroup, 
			VMFMaterial.SAND => sandGroup, 
			VMFMaterial.CARPET => carpetGroup, 
			_ => tileGroup, 
		};
	}

	private VMFMaterial GetFootstepMaterial(float distance = 0.25f)
	{
		if (Physics.RaycastNonAlloc(base.transform.position, Vector3.down, _hitResults, distance, _mask) > 0)
		{
			RaycastHit raycastHit = _hitResults[0];
			GameObject gameObject = raycastHit.collider.gameObject;
			if (_cacheObj != gameObject)
			{
				VMFBoxMaterial component = raycastHit.collider.GetComponent<VMFBoxMaterial>();
				if ((bool)component)
				{
					return component.materialType;
				}
				VMFMaterials vMFMaterials = raycastHit.collider.GetComponent<VMFMaterials>();
				if (!vMFMaterials)
				{
					vMFMaterials = raycastHit.collider.GetComponentInParent<VMFMaterials>(includeInactive: true);
				}
				if (!vMFMaterials)
				{
					return VMFMaterial.TILE;
				}
				_cacheObj = gameObject;
				_cacheMesh = vMFMaterials?.meshFilter;
				_cacheData = vMFMaterials?.materialDictionary;
				if ((bool)_cacheMesh && !_jobTriangleCache.ContainsKey(_cacheObj))
				{
					CacheTriangleDataWithJobs(_cacheObj, _cacheMesh);
				}
			}
			if (!_cacheMesh || _cacheData == null)
			{
				return VMFMaterial.TILE;
			}
			return FindHitMaterial(raycastHit.point, 0.015f);
		}
		return VMFMaterial.TILE;
	}

	private void CacheTriangleDataWithJobs(GameObject obj, MeshFilter meshFilter)
	{
		if (!meshFilter || !meshFilter.sharedMesh || !meshFilter.sharedMesh.isReadable || _cacheData == null)
		{
			return;
		}
		using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshFilter.sharedMesh);
		Mesh.MeshData meshData = meshDataArray[0];
		List<byte> list = new List<byte>();
		List<byte> list2 = new List<byte>();
		List<byte> list3 = new List<byte>();
		foreach (KeyValuePair<byte, SerializedDictionary<byte, VMFMaterial>> cacheDatum in _cacheData)
		{
			foreach (KeyValuePair<byte, VMFMaterial> item in cacheDatum.Value)
			{
				list.Add(cacheDatum.Key);
				list2.Add(item.Key);
				list3.Add((byte)item.Value);
			}
		}
		NativeArray<byte> materialIndices = new NativeArray<byte>(list.ToArray(), Allocator.TempJob);
		NativeArray<byte> textureIndices = new NativeArray<byte>(list2.ToArray(), Allocator.TempJob);
		NativeArray<byte> materialTypes = new NativeArray<byte>(list3.ToArray(), Allocator.TempJob);
		int num = 0;
		for (int i = 0; i < meshData.subMeshCount; i++)
		{
			num += meshData.GetSubMesh(i).indexCount / 3;
		}
		NativeArray<ZTriangleData> value = new NativeArray<ZTriangleData>(num, Allocator.Persistent);
		int num2 = 0;
		for (int j = 0; j < meshData.subMeshCount; j++)
		{
			int indexCount = meshData.GetSubMesh(j).indexCount;
			int num3 = indexCount / 3;
			NativeArray<Vector3> nativeArray = new NativeArray<Vector3>(meshData.vertexCount, Allocator.TempJob);
			NativeArray<Vector4> nativeArray2 = new NativeArray<Vector4>(meshData.vertexCount, Allocator.TempJob);
			NativeArray<int> nativeArray3 = new NativeArray<int>(indexCount, Allocator.TempJob);
			meshData.GetVertices(nativeArray);
			meshData.GetUVs(0, nativeArray2);
			meshData.GetIndices(nativeArray3, j);
			float4x4 localToWorldMatrix = meshFilter.transform.localToWorldMatrix;
			TriangleCacheJob jobData = default(TriangleCacheJob);
			jobData.Vertices = nativeArray;
			jobData.UVs = nativeArray2;
			jobData.Triangles = nativeArray3;
			jobData.LocalToWorldMatrix = localToWorldMatrix;
			jobData.MaterialIndices = materialIndices;
			jobData.TextureIndices = textureIndices;
			jobData.MaterialTypes = materialTypes;
			jobData.OutputTriangles = value.GetSubArray(num2, num3);
			IJobParallelForExtensions.Schedule(jobData, num3, 64).Complete();
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeArray3.Dispose();
			num2 += num3;
		}
		materialIndices.Dispose();
		textureIndices.Dispose();
		materialTypes.Dispose();
		_jobTriangleCache[obj] = value;
	}

	private VMFMaterial FindHitMaterial(Vector3 hitPoint, float offset)
	{
		if (!_jobTriangleCache.TryGetValue(_cacheObj, out var value))
		{
			return VMFMaterial.TILE;
		}
		int length = value.Length;
		NativeArray<bool> results = new NativeArray<bool>(length, Allocator.TempJob);
		NativeArray<byte> materials = new NativeArray<byte>(length, Allocator.TempJob);
		TriangleHitJob jobData = default(TriangleHitJob);
		jobData.Triangles = value;
		jobData.HitPoint = new float3(hitPoint.x, hitPoint.y, hitPoint.z);
		jobData.YOffset = offset;
		jobData.Results = results;
		jobData.Materials = materials;
		IJobParallelForExtensions.Schedule(jobData, length, 64).Complete();
		VMFMaterial result = VMFMaterial.TILE;
		for (int i = 0; i < length; i++)
		{
			if (results[i])
			{
				result = (VMFMaterial)materials[i];
				break;
			}
		}
		results.Dispose();
		materials.Dispose();
		return result;
	}
}
