using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Goma elástica entre dos compañeros: con holgura no hace nada; estirada más allá de
    /// su largo, tira de los dos (un SpringJoint). Se dibuja como un cordón que adelgaza y se
    /// pone blanco cuanto más tenso está.
    /// </summary>
    public class GomaElastica : MonoBehaviour
    {
        ControladorBicho a, b;
        SpringJoint muelle;
        Transform cordon;
        Material material;
        Color color;
        float largo;

        static readonly Vector3 Pecho = new Vector3(0f, 0.6f, 0f);
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        public static GomaElastica Crear(ControladorBicho primero, ControladorBicho segundo, Color colorGoma,
                                         float largoLibre, float fuerza, float amortiguacion)
        {
            var go = new GameObject($"Goma {primero.name} - {segundo.name}");
            var goma = go.AddComponent<GomaElastica>();
            goma.Configurar(primero, segundo, colorGoma, largoLibre, fuerza, amortiguacion);
            return goma;
        }

        void Configurar(ControladorBicho primero, ControladorBicho segundo, Color colorGoma,
                        float largoLibre, float fuerza, float amortiguacion)
        {
            a = primero;
            b = segundo;
            color = colorGoma;
            largo = largoLibre;

            muelle = a.gameObject.AddComponent<SpringJoint>();
            muelle.connectedBody = b.Cuerpo;
            muelle.autoConfigureConnectedAnchor = false;
            muelle.anchor = Pecho;
            muelle.connectedAnchor = Pecho;
            muelle.minDistance = 0f;
            muelle.maxDistance = largoLibre;
            muelle.spring = fuerza;
            muelle.damper = amortiguacion;
            muelle.tolerance = 0.05f;
            muelle.enableCollision = true;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Cordon";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(transform, false);
            visual.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            visual.GetComponent<Renderer>().sharedMaterial = material;
            cordon = visual.transform;
        }

        void LateUpdate()
        {
            if (a == null || b == null || a.Retirado || b.Retirado)
            {
                if (cordon != null) cordon.gameObject.SetActive(false);
                return;
            }

            Vector3 p1 = a.transform.TransformPoint(Pecho);
            Vector3 p2 = b.transform.TransformPoint(Pecho);
            Vector3 tramo = p2 - p1;
            float distancia = tramo.magnitude;
            float tension = Mathf.Clamp01((distancia - largo) / Mathf.Max(0.1f, largo));

            cordon.gameObject.SetActive(distancia > 0.05f);
            cordon.position = (p1 + p2) * 0.5f;
            cordon.rotation = distancia > 0.001f ? Quaternion.FromToRotation(Vector3.up, tramo / distancia) : Quaternion.identity;
            float grosor = Mathf.Lerp(0.12f, 0.05f, tension);
            cordon.localScale = new Vector3(grosor, distancia * 0.5f, grosor);

            Color c = Color.Lerp(color, Color.white, tension * 0.8f);
            if (material.HasProperty(IdBaseColor)) material.SetColor(IdBaseColor, c);
            else material.color = c;
        }

        void OnDestroy()
        {
            if (muelle != null) Destroy(muelle);
            if (material != null) Destroy(material);
        }
    }
}
