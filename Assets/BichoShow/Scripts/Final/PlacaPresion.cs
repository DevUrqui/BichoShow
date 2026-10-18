using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Placa en el suelo que cuenta cuántos bichos tiene encima (de un equipo o de cualquiera).
    /// Se hunde un poco y cambia de color cuando está pisada. También sirve, sin aspecto, como
    /// "zona" para contar bichos (la meta, por ejemplo).
    /// </summary>
    public class PlacaPresion : MonoBehaviour
    {
        [Tooltip("Zona de detección, apoyada en el suelo y centrada en este objeto.")]
        public Vector3 tamano = new Vector3(1.8f, 1.6f, 1.8f);
        [Tooltip("-1 = cuenta cualquier bicho; 0 o 1 = solo los de ese equipo.")]
        public int equipo = -1;
        [Tooltip("Pieza visible que se hunde y se ilumina (opcional).")]
        public Renderer visual;
        public Color colorLibre = new Color(0.55f, 0.55f, 0.6f);
        public Color colorPulsada = new Color(0.4f, 1f, 0.5f);

        public int Contados { get; private set; }
        public bool Pulsada => Contados > 0;

        Vector3 posicionVisual;
        Material material;

        static readonly Collider[] Buffer = new Collider[32];
        static readonly HashSet<ControladorBicho> Vistos = new HashSet<ControladorBicho>();
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            if (visual != null)
            {
                posicionVisual = visual.transform.localPosition;
                material = visual.material;
            }
        }

        void FixedUpdate()
        {
            Contados = Contar(transform.position + Vector3.up * tamano.y * 0.5f, tamano * 0.5f, equipo);
        }

        bool pulsadaAntes;

        void Update()
        {
            if (Pulsada != pulsadaAntes)
            {
                pulsadaAntes = Pulsada;
                Sonidos.Reproducir(Sonido.Click, transform.position, Pulsada ? 1f : 0.5f, Pulsada ? 1f : 0.8f);
            }
            if (visual == null) return;
            visual.transform.localPosition = posicionVisual + (Pulsada ? Vector3.down * 0.06f : Vector3.zero);
            Color c = Pulsada ? colorPulsada : colorLibre;
            if (material.HasProperty(IdBaseColor)) material.SetColor(IdBaseColor, c);
            else material.color = c;
        }

        /// <summary>Cuenta los bichos (no retirados) dentro de una caja; -1 = de cualquier equipo.</summary>
        public static int Contar(Vector3 centro, Vector3 mitad, int equipo)
        {
            int n = Physics.OverlapBoxNonAlloc(centro, mitad, Buffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Vistos.Clear();
            for (int i = 0; i < n; i++)
            {
                var rb = Buffer[i].attachedRigidbody;
                var bicho = rb != null ? rb.GetComponent<ControladorBicho>() : null;
                if (bicho == null || bicho.Retirado) continue;
                if (equipo >= 0 && RondaBase.EquipoDe(bicho) != equipo) continue;
                Vistos.Add(bicho);
            }
            return Vistos.Count;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * tamano.y * 0.5f, tamano);
        }
    }
}
