using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Nodig voor ScriptableRenderPass en RTHandle

public class MyEmptyRenderPass : ScriptableRenderPass
{
    // Een string die wordt gebruikt voor de profiler. Handig voor debugging!
    private const string m_ProfilerTag = "My Empty Render Pass";

    // Constructor van de pass. Initialiseer hier wat nodig is.
    public MyEmptyRenderPass()
    {
        // Optioneel: Stel de standaard renderPassEvent in
        // renderPassEvent = RenderPassEvent.AfterRenderingOpaques; 
    }

    /// <summary>
    /// Hier kun je variabelen instellen die de pass nodig heeft,
    /// zoals materialen, textures, of welke pass-event het moet zijn.
    /// </summary>
    // public void Setup(Material customMaterial)
    // {
    //     // m_CustomMaterial = customMaterial;
    // }

    /// <summary>
    /// Deze methode wordt aangeroepen voordat de render pass wordt uitgevoerd.
    /// Hier kun je tijdelijke render targets toewijzen of de camera target instellen.
    /// </summary>
    /// <param name="cmd">Het CommandBuffer waarmee commando's naar de GPU worden gestuurd.</param>
    /// <param name="renderingData">Data over de huidige rendering context.</param>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Optioneel: Configureer hier de render targets of clear flags
        // ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
        // ConfigureClear(ClearFlag.Color, Color.black);
    }

    /// <summary>
    /// Deze methode is waar de eigenlijke render-logica plaatsvindt.
    /// Alle teken- en blit-commando's gaan hierin.
    /// </summary>
    /// <param name="context">De ScriptableRenderContext om commando's uit te voeren.</param>
    /// <param name="renderingData">Data over de huidige rendering context.</param>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Haal een CommandBuffer op. Gebruik CommandBufferPool om performance te optimaliseren.
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

        using (new ProfilingScope(cmd, new ProfilingSampler(m_ProfilerTag)))
        {
            // --- HIER PLAATS JE JE RENDERING LOGICA ---
            // Bijvoorbeeld:
            // - Blit operaties om textures te kopiëren of post-processing effecten toe te passen.
            // - Drawing commando's om meshes te renderen met een specifiek materiaal.
            // - Clear commando's om een buffer te legen.

            // Voorbeeld: Een lege blit (doet niets nuttigs zonder materiaal)
            // RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            // Blit(cmd, cameraTarget, cameraTarget); 
            
            // Voorbeeld: Stel een globale shader property in (zichtbaar in elke shader)
            // cmd.SetGlobalVector("_MyCustomVector", new Vector4(1, 0, 0, 1));
        }

        // Voer de commando's in de buffer uit en release de buffer.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    /// <summary>
    /// Deze methode wordt aangeroepen nadat de render pass is uitgevoerd.
    /// Hier kun je tijdelijke render targets vrijgeven.
    /// </summary>
    /// <param name="cmd">Het CommandBuffer.</param>
    /// <param name="renderingData">Rendering data.</param>
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // Ruim hier eventuele tijdelijke render textures of buffers op
        // cmd.ReleaseTemporaryRT(m_TemporaryRTId);
    }
}