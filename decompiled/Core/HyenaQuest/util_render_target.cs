using UnityEngine;
using UnityEngine.Rendering;

namespace HyenaQuest;

public static class util_render_target
{
	public static void ClearRenderTarget(RenderTexture target)
	{
		if ((bool)target)
		{
			CommandBuffer commandBuffer = new CommandBuffer();
			commandBuffer.SetRenderTarget(target);
			commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
			Graphics.ExecuteCommandBuffer(commandBuffer);
			commandBuffer.Dispose();
		}
	}
}
