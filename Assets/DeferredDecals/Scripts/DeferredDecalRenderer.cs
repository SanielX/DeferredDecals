using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HG.DeferredDecals
{
    [ExecuteInEditMode]
    public class DeferredDecalRenderer : MonoBehaviour
    {
        private const string BUFFER_NAME = "Deferred decals";
        Dictionary<Camera, CommandBuffer> cameras = new Dictionary<Camera, CommandBuffer>();
        DeferredDecalSystem system;

        /// <summary>
        /// For debug purposes
        /// </summary>
        public int renderedDecals { get; private set; }

        static readonly int normalsID = Shader.PropertyToID("_NormalsCopy");
        static readonly int diffuseID = Shader.PropertyToID("_DiffuseCopy");
        static readonly int smoothnessID = Shader.PropertyToID("_SpecCopy");

        [SerializeField]
        private Mesh m_CubeMesh = null;

        Plane[] planes = new Plane[6];
        RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[3];

        private void Awake()
        {
            system = new DeferredDecalSystem();
        }

        public void LateUpdate()
        {
            CommandBuffer buffer;
            var cam = Camera.current;
            if (!cam)
                return;

            buffer = GetBuffer(cam);

            // recreate the command buffer when something has changed.
            var system = DeferredDecalSystem.Instance;
            buffer.Clear();

            if (system.availableLayers.Count == 0)
                return;

            renderedDecals = 0;

            if (!gameObject.activeInHierarchy && enabled)
                return;

            GeometryUtility.CalculateFrustumPlanes(cam, planes);

            // copy g-buffer normals into a temporary RT
            buffer.GetTemporaryRT(diffuseID, -1, -1);
            buffer.GetTemporaryRT(smoothnessID, -1, -1);
            buffer.GetTemporaryRT(normalsID, -1, -1);

            mrt[0] = BuiltinRenderTextureType.GBuffer0;
            mrt[1] = BuiltinRenderTextureType.GBuffer1;
            mrt[2] = BuiltinRenderTextureType.GBuffer2;

            foreach (var layer in system.availableLayers)
            {
                // In shader you can't really read and write to the texture at the same time
                // That's why we need to copy existing textures for each layer
                buffer.Blit(BuiltinRenderTextureType.GBuffer0, diffuseID);
                buffer.Blit(BuiltinRenderTextureType.GBuffer1, smoothnessID);
                buffer.Blit(BuiltinRenderTextureType.GBuffer2, normalsID);
                buffer.SetRenderTarget(mrt, BuiltinRenderTextureType.CameraTarget);

                if (!system.layerToDecals.TryGetValue(layer, out List<Decal> decals))
                    return;

                foreach (var decal in decals)
                {
                    if (!GeometryUtility.TestPlanesAABB(planes, decal.DecalBounds))
                    {
                        continue;
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
                cam.AddCommandBuffer(CameraEvent.BeforeLighting, buffer);
            }

            return buffer;
        }
    }
}