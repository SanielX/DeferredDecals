using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HG.DeferredDecals
{
    [ExecuteAlways]
    public class DeferredDecalRenderer : MonoBehaviour
    {
        private const string BUFFER_NAME = "Deferred decals";

        private static readonly int normalsID = Shader.PropertyToID("_NormalsCopy");
        private static readonly int diffuseID = Shader.PropertyToID("_DiffuseCopy");
        private static readonly int smoothnessID = Shader.PropertyToID("_SpecCopy");

        private readonly Dictionary<Camera, CommandBuffer> cameras = new Dictionary<Camera, CommandBuffer>();
        private readonly Plane[] planes = new Plane[6];
        private readonly RenderTargetIdentifier[] renderTargets = new RenderTargetIdentifier[3];
        
        private DeferredDecalSystem system;

        /// <summary> For debug purposes </summary>
        public int renderedDecals { get; private set; }

        [SerializeField] Mesh m_CubeMesh = null;
        
        private void OnEnable()  => Camera.onPreRender += RenderDecals;
        private void OnDisable() => Camera.onPreRender -= RenderDecals;
        
        private void Awake()
        {
            system = new DeferredDecalSystem();
        }

        public void RenderDecals(Camera cam)
        {
            if (!cam || system.availableLayers.Count == 0)
                return;

            CommandBuffer buffer = GetBuffer(cam);
            renderedDecals = 0;

            if (!gameObject.activeInHierarchy && enabled)
                return;

            GeometryUtility.CalculateFrustumPlanes(cam, planes);

            // copy g-buffer normals into a temporary RT
            buffer.GetTemporaryRT(diffuseID, -1, -1);
            buffer.GetTemporaryRT(smoothnessID, -1, -1);
            buffer.GetTemporaryRT(normalsID, -1, -1);

            renderTargets[0] = BuiltinRenderTextureType.GBuffer0;
            renderTargets[1] = BuiltinRenderTextureType.GBuffer1;
            renderTargets[2] = BuiltinRenderTextureType.GBuffer2;

            foreach (var layer in system.availableLayers)
            {
                bool copiedTextures = false;

                if (!system.layerToDecals.TryGetValue(layer, out List<Decal> decals))
                    return;

                foreach (var decal in decals)
                {
                    if (!GeometryUtility.TestPlanesAABB(planes, decal.DecalBounds))
                    {
                        continue;
                    }

                    if (!copiedTextures)
                    {
                        // In shader you can't really read and write to the texture at the same time
                        // That's why we need to copy existing textures for each layer
                        CopyRenderes();
                        copiedTextures = true;
                    }

                    renderedDecals++;

                    //  matricies.Add(Matrix4x4.TRS(decalTransform.position, decalTransform.rotation, decalTransform.lossyScale));
                    buffer.DrawMesh(m_CubeMesh, decal.transform.localToWorldMatrix, decal.DecalMaterial);  // TODO: Should support instancing
                }
            }

            // release temporary normals RT
            buffer.ReleaseTemporaryRT(normalsID);
            buffer.ReleaseTemporaryRT(diffuseID);
            buffer.ReleaseTemporaryRT(smoothnessID);

            void CopyRenderes()
            {
                buffer.Blit(BuiltinRenderTextureType.GBuffer0, diffuseID);
                buffer.Blit(BuiltinRenderTextureType.GBuffer1, smoothnessID);
                buffer.Blit(BuiltinRenderTextureType.GBuffer2, normalsID);
                buffer.SetRenderTarget(renderTargets, BuiltinRenderTextureType.CameraTarget);
            }
        }

        private CommandBuffer GetBuffer(Camera cam)
        {
            CommandBuffer buffer;
            if (cameras.ContainsKey(cam))
            {
                buffer = cameras[cam];
                buffer.Clear();
            }
            else
            {
                buffer = cam.GetCommandBuffers(CameraEvent.BeforeLighting).FirstOrDefault(b => b.name == BUFFER_NAME);
                if (buffer != null)
                {
                    cameras[cam] = buffer;
                    return buffer;
                }

                buffer = new CommandBuffer();
                buffer.name = BUFFER_NAME;
                cameras[cam] = buffer;

                // set this command buffer to be executed just before deferred lighting pass
                // in the camera
                cam.AddCommandBuffer(CameraEvent.BeforeReflections, buffer);
            }

            buffer.Clear();
            return buffer;
        }
    }
}
