using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BichoShow
{
    /// <summary>
    /// Cuando algo se pone entre la cámara y el bicho (el abejorro, una grada, la pista de arriba…),
    /// se ve su silueta en blanco a través de ello, para no perderlo nunca de vista. Duplica el
    /// cuerpo con dos materiales: uno marca dónde el bicho se ve y el otro pinta la silueta donde
    /// no (así no sale cuando el bicho se tapa a sí mismo). Lo añade ControladorBicho solo.
    /// Solo se ve en los bichos que lleva una persona (los bots no tienen silueta).
    /// </summary>
    public class SiluetaBicho : MonoBehaviour
    {
        public Color color = new Color(1f, 1f, 1f, 0.55f);

        static Material marca, silueta;

        readonly List<Renderer> copias = new List<Renderer>();
        ControladorBicho bicho;

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            if (!PrepararMateriales()) return;
            foreach (var cuerpo in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (cuerpo.sharedMesh == null || cuerpo.name.StartsWith("Silueta")) continue;
                copias.Add(Duplicar(cuerpo, "Silueta (marca)", marca));
                copias.Add(Duplicar(cuerpo, "Silueta", silueta));
            }
            Actualizar();
        }

        // Después de Retirar (que enciende todos los renderers) y de que un bot tome o deje el bicho.
        void LateUpdate() => Actualizar();

        void Actualizar()
        {
            bool ver = bicho != null && !bicho.Retirado && !bicho.EsBot;
            if (copias.Count == 0 || copias[0].enabled == ver) return;
            foreach (var c in copias) if (c != null) c.enabled = ver;
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
        static Renderer Duplicar(SkinnedMeshRenderer origen, string nombre, Material material)
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
            return copia;
        }

        // Enter Play Mode sin recargar dominio: los materiales estáticos se vuelven a crear.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() { marca = null; silueta = null; }
    }
}
