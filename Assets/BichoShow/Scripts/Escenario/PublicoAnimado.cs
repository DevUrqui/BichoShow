using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// El público de las gradas: se balancea, da algún botecito suelto y se vuelve loco
    /// (bota y agita los brazos) cada vez que un bicho se cae al suelo.
    /// Anima a todos los hijos cuyo nombre empiece por "Publico".
    /// </summary>
    public class PublicoAnimado : MonoBehaviour
    {
        [Tooltip("Altura de los botes al celebrar.")]
        public float alturaSalto = 0.35f;
        [Tooltip("Segundos que dura la celebración tras un derribo.")]
        public float duracionCelebracion = 1.6f;

        Transform[] miembros;
        Vector3[] posiciones;
        Quaternion[] giros;
        float[] fases;
        float finCelebracion = -100f;

        void Awake()
        {
            var lista = new List<Transform>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t != transform && t.name.StartsWith("Publico")) lista.Add(t);

            miembros = lista.ToArray();
            posiciones = new Vector3[miembros.Length];
            giros = new Quaternion[miembros.Length];
            fases = new float[miembros.Length];
            for (int i = 0; i < miembros.Length; i++)
            {
                posiciones[i] = miembros[i].localPosition;
                giros[i] = miembros[i].localRotation;
                fases[i] = i * 2.399f; // ángulo áureo: nadie se mueve a la vez que su vecino
            }
        }

        void OnEnable() => ControladorBicho.AlgunoDerribado += Celebrar;
        void OnDisable() => ControladorBicho.AlgunoDerribado -= Celebrar;

        void Celebrar(ControladorBicho _) => finCelebracion = Time.time + duracionCelebracion;

        void Update()
        {
            if (miembros == null) return;
            float t = Time.time;
            bool fiesta = t < finCelebracion;

            for (int i = 0; i < miembros.Length; i++)
            {
                var m = miembros[i];
                if (m == null) continue;
                float f = fases[i];
                float bote = fiesta
                    ? Mathf.Abs(Mathf.Sin(t * 9f + f)) * alturaSalto
                    : Mathf.Max(0f, Mathf.Sin(t * 1.3f + f) - 0.9f) * 1.5f; // de vez en cuando, un botecito
                float balanceo = fiesta ? Mathf.Sin(t * 14f + f) * 12f : Mathf.Sin(t * 1.7f + f) * 4f;
                m.localPosition = posiciones[i] + Vector3.up * bote;
                m.localRotation = giros[i] * Quaternion.Euler(0f, 0f, balanceo);
            }
        }
    }
}
