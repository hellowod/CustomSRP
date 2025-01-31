using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SRP0406 : RenderPipelineAsset
{
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Render Pipeline/SRP0406", priority = 1)]
    static void CreateSRP0406()
    {
        var instance = ScriptableObject.CreateInstance<SRP0406>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP0406.asset");
    }
    #endif

    protected override RenderPipeline CreatePipeline()
    {
        return new SRP0406Instance();
    }
}

public class SRP0406Instance : RenderPipeline
{
    private static readonly ShaderTagId m_PassName = new ShaderTagId("SRP0406_Pass"); //The shader pass tag just for SRP0406

    public SRP0406Instance()
    {
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        //Remember to add ENABLE_SHADER_DEBUG_PRINT in ProjectSettings > Player
        #if ENABLE_SHADER_DEBUG_PRINT
            CommandBuffer cmd_shaderDebug = new CommandBuffer();
            cmd_shaderDebug.name = "Shader Debug Print";
            ShaderDebugPrintManager.instance.SetShaderDebugPrintInputConstants(cmd_shaderDebug, ShaderDebugPrintInputProducer.Get());
            ShaderDebugPrintManager.instance.SetShaderDebugPrintBindings(cmd_shaderDebug);
            context.ExecuteCommandBuffer(cmd_shaderDebug);
            cmd_shaderDebug.Release();
        #endif

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context,camera);

            //Culling
            ScriptableCullingParameters cullingParams;
            if (!camera.TryGetCullingParameters(out cullingParams))
                continue;
            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Get the setting from camera component
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox? true : false;
            bool clearDepth = camera.clearFlags == CameraClearFlags.Nothing? false : true;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color? true : false;

            //Camera clear flag
            CommandBuffer cmd = new CommandBuffer();
            cmd.ClearRenderTarget(clearDepth, clearColor, camera.backgroundColor);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            //Setup DrawSettings and FilterSettings
            var sortingSettings = new SortingSettings(camera);
            DrawingSettings drawSettings = new DrawingSettings(m_PassName, sortingSettings);
            FilteringSettings filterSettings = new FilteringSettings(RenderQueueRange.all);

            //Skybox
            if(drawSkyBox)  {  context.DrawSkybox(camera);  }

            //Opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            //Transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

            context.Submit();

            EndCameraRendering(context,camera);
        }

        EndFrameRendering(context,cameras);

        //ENABLE_SHADER_DEBUG_PRINT EndFrame
        #if ENABLE_SHADER_DEBUG_PRINT
            ShaderDebugPrintManager.instance.EndFrame();
        #endif
    }
}