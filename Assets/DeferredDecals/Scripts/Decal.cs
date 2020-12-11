using UnityEngine;

namespace HG.DeferredDecals
{
    [System.Flags]
    public enum DecalFeature
    {
        Diffuse = 1,
        Normal = 2,
        Smoothness = 4,
        Emission = 8
    }

    [ExecuteAlways]
    [AddComponentMenu("Effects/Decal")]
    public class Decal : MonoBehaviour
    {
        private Bounds bounds;
        private Vector3[] boundPositions = new Vector3[8];

        public Bounds DecalBounds { get { return bounds; } }
        public Material DecalMaterial { get => m_Material; }
        public int Layer { get => m_Layer; }

        [Range(0, 49)]
        [SerializeField] int m_Layer = 20;
        [SerializeField] DecalFeature m_FeatureSet = (DecalFeature)~0;        //TODO: Make this matter
        [SerializeField] Material m_Material = null;

        public void OnEnable()
        {
            if (!m_Material)
                return;

            DeferredDecalSystem.Instance?.AddDecal(this);
            RecalculateBounds();
        }

        private void Start()
        {
            if (!m_Material)
                return;

            DeferredDecalSystem.Instance?.AddDecal(this);
        }

        public void RecalculateBounds()
        {
            Vector3 scale = transform.lossyScale;
            Vector3 position = transform.position;

            Quaternion quat = Quaternion.identity;
            quat.SetLookRotation(transform.forward, Vector3.Cross(transform.forward, transform.right));

            boundPositions[0] = position + quat * new Vector3( scale.x * 0.5f,  scale.y * 0.5f,  scale.z * 0.5f);
            boundPositions[1] = position + quat * new Vector3(-scale.x * 0.5f,  scale.y * 0.5f,  scale.z * 0.5f);
            boundPositions[2] = position + quat * new Vector3( scale.x * 0.5f, -scale.y * 0.5f,  scale.z * 0.5f);
            boundPositions[3] = position + quat * new Vector3( scale.x * 0.5f,  scale.y * 0.5f, -scale.z * 0.5f);
            boundPositions[4] = position + quat * new Vector3(-scale.x * 0.5f, -scale.y * 0.5f,  scale.z * 0.5f);
            boundPositions[5] = position + quat * new Vector3( scale.x * 0.5f, -scale.y * 0.5f, -scale.z * 0.5f);
            boundPositions[6] = position + quat * new Vector3(-scale.x * 0.5f,  scale.y * 0.5f, -scale.z * 0.5f);
            boundPositions[7] = position + quat * new Vector3(-scale.x * 0.5f, -scale.y * 0.5f, -scale.z * 0.5f);

            bounds = GeometryUtility.CalculateBounds(boundPositions, Matrix4x4.identity);
        }

        public void OnDisable()
        {
            DeferredDecalSystem.Instance?.RemoveDecal(this);
        }

#if UNITY_EDITOR
        private void DrawGizmo(bool selected)
        {
            var col = new Color(0.0f, 0.7f, 1f, 1.0f);
            col.a = selected ? 0.1f : 0.05f;
            Gizmos.color = col;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            col.a = selected ? 0.5f : 0.2f;
            Gizmos.color = col;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }


        public void OnDrawGizmos()
        {
            DrawGizmo(false);
        }
        public void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
        }

        private void OnValidate()
        {
            DeferredDecalSystem.Instance?.RemoveDecal(this, false);
            OnEnable();
        }

        [UnityEditor.MenuItem("GameObject/Effects/Deferred Decal")]
        static void CreateDecal()
        {
            var gameObj = new GameObject();
            gameObj.name = "Deferred Decal";

            gameObj.AddComponent<Decal>();
            Camera cam = UnityEditor.SceneView.lastActiveSceneView.camera;
            Transform sceneCam = cam.transform;
            gameObj.transform.position = sceneCam.position + sceneCam.forward * /*(cam.nearClipPlane */ 6/*)*/;

            UnityEditor.Selection.activeObject = gameObj;
        }
#endif
    }
}