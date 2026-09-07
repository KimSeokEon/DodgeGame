using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

// =============================================================================
// PauseBlurFeature.cs
// -----------------------------------------------------------------------------
// ESC 일시정지 메뉴가 열려 있을 때 게임 화면 전체를 블러 + 어둡게 만드는
// URP 커스텀 렌더 피처. 시간(timeScale)은 건드리지 않는다 — 오버레이 연출용.
//
// 등록 : Assets/Settings/PC_Renderer.asset (필요하면 Mobile_Renderer 도)
//        Inspector 의 Renderer Features 에 "Pause Blur Feature" 추가하고
//        Shader 칸에 Hidden/PauseBlur (Assets/Script/Dodge2/PauseBlur.shader) 연결.
//
// 켜고 끄기 : PauseMenuManager 가 static 필드로 제어한다.
//   PauseBlurFeature.Active  — 이 피처가 돌지 말지 (false 면 렌더 비용 0)
//   PauseBlurFeature.Weight  — 0~1, 블러 세기 (메뉴 열릴 때 2초에 걸쳐 0→1)
//
// 주의 : RenderGraph 경로(Unity 6 / URP 17 기본)로 작성됨. Compatibility Mode
//        (RenderGraph 비활성) 에서는 RecordRenderGraph 가 안 불리므로 동작 안 함.
// =============================================================================
public class PauseBlurFeature : ScriptableRendererFeature
{
    // ── PauseMenuManager 가 제어하는 런타임 스위치 ──────────────────
    public static bool Active;
    public static float Weight; // 0~1

    [Header("셰이더 (Hidden/PauseBlur)")]
    [SerializeField] private Shader shader;

    [Header("룩")]
    [Range(1f, 24f)] public float blurRadius = 8f;   // 블러 반경(픽셀)
    [Range(0f, 0.8f)] public float darken = 0.15f;    // 배경 어둡게 (0 = 그대로)

    [Header("주입 시점")]
    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private Material _mat;
    private BlurPass _pass;

    public override void Create()
    {
        if (shader == null)
            shader = Shader.Find("Hidden/PauseBlur");
        if (shader == null)
            return;

        _mat = CoreUtils.CreateEngineMaterial(shader);
        _pass = new BlurPass(_mat) { renderPassEvent = injectionPoint };
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_mat);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 메뉴 안 열렸으면 아무 것도 안 함 (비용 0)
        if (!Active || Weight <= 0f || _mat == null)
            return;

        // 게임 카메라에서만 (씬뷰 / 프리뷰 / 리플렉션 프로브 제외)
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        _mat.SetFloat("_BlurRadius", blurRadius);
        _mat.SetFloat("_Darken", darken);
        _mat.SetFloat("_Weight", Mathf.Clamp01(Weight));

        _pass.renderPassEvent = injectionPoint;
        renderer.EnqueuePass(_pass);
    }

    // ── 실제 패스 (RenderGraph) ────────────────────────────────────
    private class BlurPass : ScriptableRenderPass
    {
        private readonly Material _mat;

        public BlurPass(Material mat)
        {
            _mat = mat;
            // 백버퍼로 바로 그리면 activeColorTexture 를 샘플할 수 없으므로 중간 텍스처 강제
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return; // 중간 텍스처가 없으면(백버퍼 직행) 안전하게 패스

            TextureHandle source = resourceData.activeColorTexture;

            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "PauseBlur_CameraColor";
            desc.clearBuffer = false;
            desc.depthBufferBits = 0;
            desc.msaaSamples = MSAASamples.None;
            TextureHandle dest = renderGraph.CreateTexture(desc);

            // 블러 + 틴트 + weight lerp :  activeColor -> dest
            var para = new RenderGraphUtils.BlitMaterialParameters(source, dest, _mat, 0);
            renderGraph.AddBlitPass(para, "PauseBlur");

            // 이후 파이프라인이 dest 를 카메라 컬러로 쓰게 교체 (copy-back 불필요)
            resourceData.cameraColor = dest;
        }
    }
}
