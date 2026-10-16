using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Malla de tubo (brazo o pierna "de fideo") que sigue una curva y se reconstruye cada
    /// frame. Los extremos quedan escondidos dentro del cuerpo, las manos o las botas, así
    /// que no lleva tapas.
    /// </summary>
    public class TuboProcedural
    {
        public Mesh Malla { get; }

        readonly int tramos, lados;
        readonly Vector3[] vertices, normales;
        readonly Vector3[] puntos;

        public TuboProcedural(string nombre, int tramos = 10, int lados = 8)
        {
            this.tramos = Mathf.Max(2, tramos);
            this.lados = Mathf.Max(3, lados);
            puntos = new Vector3[this.tramos];
            vertices = new Vector3[this.tramos * this.lados];
            normales = new Vector3[vertices.Length];

            var triangulos = new int[(this.tramos - 1) * this.lados * 6];
            int t = 0;
            for (int i = 0; i < this.tramos - 1; i++)
            for (int j = 0; j < this.lados; j++)
            {
                int a = i * this.lados + j;
                int b = i * this.lados + (j + 1) % this.lados;
                int c = a + this.lados;
                int d = b + this.lados;
                triangulos[t++] = a; triangulos[t++] = c; triangulos[t++] = b;
                triangulos[t++] = b; triangulos[t++] = c; triangulos[t++] = d;
            }

            Malla = new Mesh { name = nombre };
            Malla.MarkDynamic();
            Malla.vertices = vertices;
            Malla.normals = normales;
            Malla.triangles = triangulos;
        }

        /// <summary>Rehace el tubo siguiendo una curva de Bézier cúbica (en el espacio local del objeto).</summary>
        public void Actualizar(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float radioInicio, float radioFin)
        {
            for (int i = 0; i < tramos; i++)
            {
                float t = i / (float)(tramos - 1);
                float u = 1f - t;
                puntos[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            }

            Vector3 referencia = Vector3.forward;
            for (int i = 0; i < tramos; i++)
            {
                Vector3 tangente = (i < tramos - 1 ? puntos[i + 1] - puntos[i] : puntos[i] - puntos[i - 1]).normalized;
                if (Mathf.Abs(Vector3.Dot(tangente, referencia)) > 0.95f) referencia = Vector3.right;
                Vector3 normal = Vector3.Cross(tangente, referencia).normalized;
                Vector3 binormal = Vector3.Cross(tangente, normal);
                float radio = Mathf.Lerp(radioInicio, radioFin, i / (float)(tramos - 1));

                for (int j = 0; j < lados; j++)
                {
                    float a = j / (float)lados * Mathf.PI * 2f;
                    Vector3 dir = normal * Mathf.Cos(a) + binormal * Mathf.Sin(a);
                    vertices[i * lados + j] = puntos[i] + dir * radio;
                    normales[i * lados + j] = dir;
                }
            }

            Malla.vertices = vertices;
            Malla.normals = normales;
            Malla.RecalculateBounds();
        }
    }
}
