using UnityEngine;

// =============================================================================
// BoundaryTreeScatter.cs
// -----------------------------------------------------------------------------
// 역할 : 경계선(LineRenderer)을 따라 나무 프리팹을 불규칙한 크기/간격/회전으로
//        빙 둘러 배치한다. 실제 생성은 Editor/BoundaryTreeScatterEditor.cs 의
//        [나무 생성] 버튼이 수행한다 (에디터 전용 — 씬에 오브젝트로 구워짐).
// 붙는 곳 : PlayScene의 "BoundaryLine" 오브젝트 (LineRenderer가 있는 곳)
// 생성물 : 자식 "__BoundaryTrees" 아래로 들어감. [지우기] 버튼으로 통째 삭제.
// 런타임 동작 없음 (순수 씬 데코레이션).
// =============================================================================
public class BoundaryTreeScatter : MonoBehaviour
{
    [Header("소스")]
    [Tooltip("비워두면 같은 오브젝트의 LineRenderer를 사용")]
    public LineRenderer boundary;
    [Tooltip("배치할 나무 프리팹/모델 (Assets/Materials/TreeObj)")]
    public GameObject treePrefab;

    [Header("배치")]
    [Tooltip("나무 사이 평균 간격(m). 작을수록 촘촘")]
    public float spacing = 3.4f;
    [Range(0f, 0.9f)] [Tooltip("간격이 얼마나 들쭉날쭉한지")]
    public float spacingJitter = 0.3f;
    [Tooltip("경계선에서 바깥쪽으로 밀어내는 기본 거리")]
    public float outwardOffset = 4f;
    [Tooltip("경계선 수직 방향으로 흩뿌리는 정도 (띠 두께)")]
    public float lateralJitter = 3.2f;
    [Tooltip("경계선 진행 방향으로 흩뿌리는 정도")]
    public float alongJitter = 1.4f;
    [Range(1, 4)] [Tooltip("나무 띠를 몇 겹으로 둘지")]
    public int rows = 3;
    [Tooltip("겹 사이 간격")]
    public float rowGap = 4.5f;
    [Tooltip("모서리 근처를 살짝 비울지")]
    public bool avoidCorners = false;

    [Header("크기 / 회전")]
    [Tooltip("프리팹 기본 크기에 곱해지는 랜덤 배율 (min~max). 불규칙한 크기의 핵심")]
    public Vector2 scaleMultiplier = new Vector2(0.6f, 1.5f);
    [Tooltip("나무를 무작위로 기울이는 최대 각도")]
    public float tiltMax = 6f;

    [Header("바닥")]
    [Tooltip("나무가 서는 바닥 높이(Y)")]
    public float groundY = 0f;
    [Tooltip("뿌리 쪽을 살짝 파묻는 깊이")]
    public float sinkDepth = 0.4f;

    [Header("재현성")]
    [Tooltip("같은 seed면 항상 같은 배치. 숫자 바꿔가며 마음에 드는 배치 찾기")]
    public int seed = 12345;

    public const string ContainerName = "__BoundaryTrees";
}
