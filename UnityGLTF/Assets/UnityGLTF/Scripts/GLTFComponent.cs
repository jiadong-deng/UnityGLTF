using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityGLTF {

	/// <summary>
	/// Component to load a GLTF scene with
	/// </summary>
	class GLTFComponent : MonoBehaviour
	{
		public string Url;
		public bool Multithreaded = true;
		public bool UseStream = false;

		public int MaximumLod = 300;

		public Shader GLTFStandard;
        public Shader GLTFStandardSpecular;
        public Shader GLTFConstant;

        public bool addColliders = false;

        public float boundsSize = 5f;

        public bool normalizeSize = false;
        public bool flip = false;
        public bool center = false;

        List<Bounds> rendererBounds = new List<Bounds>();

		IEnumerator Start()
		{
			GLTFSceneImporter loader = null;
			FileStream gltfStream = null;
			if (UseStream)
			{
				var fullPath = Application.streamingAssetsPath + Url;
				gltfStream = File.OpenRead(fullPath);
				loader = new GLTFSceneImporter(
					fullPath,
					gltfStream,
					gameObject.transform,
                    addColliders
					);
			}
			else
			{
				loader = new GLTFSceneImporter(
					Url,
					gameObject.transform,
                    addColliders
					);
			}

            loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.PbrMetallicRoughness, GLTFStandard);
            loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.KHR_materials_pbrSpecularGlossiness, GLTFStandardSpecular);
            loader.SetShaderForMaterialType(GLTFSceneImporter.MaterialType.CommonConstant, GLTFConstant);
			loader.MaximumLod = MaximumLod;
			yield return loader.Load(-1, Multithreaded);
			if(gltfStream != null)
			{
#if WINDOWS_UWP
				gltfStream.Dispose();
#else
				gltfStream.Close();
#endif
			}

            // Flip Remix3D models to face Unity forward vector
            if (flip && gameObject.transform.childCount > 0)
            {
                gameObject.transform.GetChild(0).transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Normalize model sizes
            if (normalizeSize && gameObject.transform.childCount > 0)
            {
                Bounds bounds = BoundsHelper.GetGameObjectHierarchyBounds(this.gameObject, this.transform.position);
                float maxBound = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                gameObject.transform.GetChild(0).localScale *= (boundsSize / maxBound);
            }

            // Center object
            MeshRenderer[] renderers = this.GetComponentsInChildren<MeshRenderer>();
            if (center && renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;

                foreach (MeshRenderer curRenderer in renderers)
                {
                    rendererBounds.Add(curRenderer.bounds);
                    combinedBounds.Encapsulate(curRenderer.bounds);
                }

                Debug.DrawRay(this.transform.position, this.transform.position - combinedBounds.center, Color.magenta, 10f);

                gameObject.transform.GetChild(0).localPosition -= combinedBounds.center;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;

            Bounds bounds = BoundsHelper.GetGameObjectHierarchyBounds(this.gameObject, this.transform.position);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = Color.red;
            foreach (Bounds curBounds in rendererBounds)
            {
                Gizmos.DrawWireCube(curBounds.center, curBounds.size);
            }
        }
    }
}
