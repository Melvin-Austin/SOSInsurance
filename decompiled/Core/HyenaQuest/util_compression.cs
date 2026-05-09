using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;

namespace HyenaQuest;

public static class util_compression
{
	public static async UniTask<byte[]> CompressAsync(byte[] data)
	{
		await UniTask.SwitchToThreadPool();
		using MemoryStream memoryStream = new MemoryStream(data.Length / 2);
		await using (BrotliStream brotliStream = new BrotliStream(memoryStream, CompressionLevel.Fastest))
		{
			await brotliStream.WriteAsync(data, 0, data.Length);
		}
		await UniTask.SwitchToMainThread();
		return memoryStream.ToArray();
	}

	public static async UniTask<byte[]> DecompressAsync(byte[] compressedData)
	{
		await UniTask.SwitchToThreadPool();
		using MemoryStream compressedStream = new MemoryStream(compressedData);
		byte[] result;
		await using (BrotliStream brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
		{
			using MemoryStream resultStream = new MemoryStream(compressedData.Length * 2);
			await brotliStream.CopyToAsync(resultStream);
			await UniTask.SwitchToMainThread();
			result = resultStream.ToArray();
		}
		return result;
	}
}
