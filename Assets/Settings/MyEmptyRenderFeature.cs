using UnityEngine;
using UnityEngine.Rendering.Universal; // Nodig voor UniversalRendererFeature

public class MyEmptyRendererFeature : ScriptableRendererFeature
{
    // Hier kun je publieke variabelen toevoegen die je in de Inspector wilt instellen
    // bijv. een Materiaal, een Render Texture, of andere instellingen.
    // [SerializeField] private Material myCustomMaterial;
    // [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

    // Een referentie naar de custom render pass die deze feature zal uitvoeren
    private MyEmptyRenderPass m_ScriptablePass;

    /// <summary>
    /// De 'Create' methode wordt aangeroepen wanneer de feature wordt ingeschakeld
    /// of wanneer het URP Asset wordt geladen. Hier initialiseren we onze render pass.
    /// </summary>
    public override void Create()
    {
        m_ScriptablePass = new MyEmptyRenderPass();

        // Configureer wanneer deze pass moet worden uitgevoerd in de render pijplijn.
        // Je kunt dit ook via een serializable field instellen.
        // m_ScriptablePass.renderPassEvent = renderPassEvent;

        // Je kunt hier ook properties van de pass instellen, bijv. materialen of textures
        // m_ScriptablePass.Setup(myCustomMaterial);
    }

    /// <summary>
    /// De 'AddRenderPasses' methode wordt per camera aangeroepen. Hier voeg je de pass toe aan de renderer.
    /// </summary>
    /// <param name="renderer">De Universal Renderer waaraan de pass wordt toegevoegd.</param>
    /// <param name="renderingData">Data over de huidige rendering context.</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Hier kun je optioneel controleren of de pass moet worden toegevoegd,
        // bijvoorbeeld op basis van cameratype of rendering path.
        // if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        renderer.EnqueuePass(m_ScriptablePass); // Voeg de pass toe aan de render queue
    }

    /// <summary>
    /// Optioneel: Deze methode wordt aangeroepen wanneer de feature wordt uitgeschakeld of vernietigd.
    /// Hier kun je resources opschonen.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        // Zorg ervoor dat eventuele tijdelijke render textures of andere resources worden vrijgegeven
        // m_ScriptablePass.Dispose();
    }
}