using UnityEngine;
using UnityEngine.Rendering;

namespace BichoShow
{
    /// <summary>
    /// Cuando algo se pone entre la cámara y el bicho (el abejorro, una grada, la pista de arriba…),
    /// se ve su silueta en blanco a través de ello, para no perderlo nunca de vista. Duplica el
    /// cuerpo con dos materiales: uno marca dónde el bicho se ve y el otro pinta la silueta donde
    /// no (así no sale cuando el bicho se tapa a sí mismo). Lo añade ControladorBicho solo.
    /// </summary>
    public class SiluetaBicho : MonoBehaviour
    {
        public Color color = new Color(1f, 1f, 1f, 0.55f);

        static Material marca, silueta;

        void Start()
        {
            if (!PrepararMateriales()) return;
            foreach (var cuerpo in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (cuerpo.sharedMesh == null || cuerpo.name.StartsWith("Silueta")) continue;
                Duplicar(cuerpo, "Silueta (marca)", marca);
                Duplicar(cuerpo, "Silueta", silueta);
            }
        }

        bool PrepararMateriales()
        {
            if (marca != null && silueta != null) return true;
            var shaderMarca = Resources.Load<Shader>("SiluetaMarca");
            var shaderSilueta = Resources.Load<Shader>("SiluetaBicho");
            if (shaderMarca == null || shaderSilueta == null || !shaderMarca.isSupported || !shaderSilueta.isSupported) return false;
            marca = new Material(shaderMarca) { name = "SiluetaMarca" };
            silueta = new Material(shaderSilueta) { name = "SiluetaBicho" };
            silueta.SetColor("_Color", color);
            return true;
        }

        /// <summary>Otro SkinnedMeshRenderer con la misma malla y los mismos huesos: se deforma igual.</summary>
        static void Duplicar(SkinnedMeshRenderer origen, string nombre, Material material)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(origen.transform, false);
            var copia = go.AddComponent<SkinnedMeshRenderer>();
            copia.sharedMesh = origen.sharedMesh;
            copia.bones = origen.bones;
            copia.rootBone = origen.rootBone;
            copia.localBounds = origen.localBounds;
            copia.updateWhenOffscreen = origen.updateWhenOffscreen;
            copia.quality = origen.quality;
            copia.shadowCastingMode = ShadowCastingMode.Off;
            copia.receiveShadows = false;
            copia.lightProbeUsage = LightProbeUsage.Off;
            copia.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var materiales = new Material[Mathf.Max(1, origen.sharedMesh.subMeshCount)];
            for (int i = 0; i < materiales.Length; i++) materiales[i] = material;
            copia.sharedMaterials = materiales;
        }

        // Enter Play Mode sin recargar dominio: los materiales estáticos se vuelven a crear.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() { marca = null; silueta = null; }
    }
}
