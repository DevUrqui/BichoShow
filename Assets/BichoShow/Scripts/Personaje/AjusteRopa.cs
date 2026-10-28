using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Cómo le queda la ropa a cada bicho, además de su talla: cuánto se mueve y cuánto crece en
    /// cada zona. La ropa está hecha para Bichete; los demás tienen la cabeza más alta o más baja,
    /// los ojos más juntos o más separados, cuernos, conchas o abdómenes enormes detrás.
    /// Desplazamientos en metros, en el espacio del bicho (x a su derecha, y arriba, z adelante).
    /// </summary>
    public static class AjusteRopa
    {
        public enum Zona { Gorro, Cara, Frente, Espalda, Anillo }

        public class Ajuste
        {
            public Vector3 gorro, cara, frente, espalda, anillo;
            public float tallaGorro = 1f, tallaCara = 1f, tallaFrente = 1f, tallaEspalda = 1f, tallaAnillo = 1f;
            /// <summary>Grados que se echa el gorro hacia atrás (negativo) o hacia delante.</summary>
            public float inclinarGorro;
            /// <summary>Para alguna prenda concreta que no encaja con el resto de su zona (desplazamiento y talla).</summary>
            public Dictionary<string, (Vector3 desplazamiento, float talla)> prendas = new Dictionary<string, (Vector3, float)>();
        }

        /// <summary>Lo del cuerpo que va a la espalda o rodea el cuerpo (lo demás, delante).</summary>
        static readonly HashSet<string> DeEspalda = new HashSet<string> { "Capa", "Mochila", "Guitarra", "Alas" };
        static readonly HashSet<string> DeAnillo = new HashSet<string> { "CollarPerlas", "Bufanda", "Flotador" };

        public static Zona ZonaCuerpo(string prenda) =>
            DeEspalda.Contains(prenda) ? Zona.Espalda : DeAnillo.Contains(prenda) ? Zona.Anillo : Zona.Frente;

        /// <summary>Separación de los ojos de Bichete: la ropa de la cara se agranda con la de cada uno.</summary>
        public const float OjosBichete = 0.34f;

        /// <summary>Por especie (los nombres de PersonajeBicho.Especies). La que no esté, como Bichete.</summary>
        public static readonly Dictionary<string, Ajuste> PorEspecie = new Dictionary<string, Ajuste>
        {
            // Abeja: el abdomen tapa la espalda; lo de atrás va encima del abdomen (lo que ve la cámara).
            ["Zumbi"] = new Ajuste { espalda = new Vector3(0f, 0.62f, -0.5f) },
            // Caracol: la concha es su espalda; lo de atrás va encima de la concha.
            ["Concha"] = new Ajuste { espalda = new Vector3(0f, 0.22f, -0.55f), tallaEspalda = 1.15f },
            // Escarabajo: el cuerno sale por delante de lo alto de la cabeza; el gorro, más atrás.
            ["Cornelio"] = new Ajuste { gorro = new Vector3(0f, 0.1f, -0.3f), inclinarGorro = -25f },
            // Araña: el abdomen enorme detrás; lo de atrás, encima de él.
            ["Ocho"] = new Ajuste
            {
                espalda = new Vector3(0f, 0.62f, -0.5f),
                prendas = { ["Capa"] = (new Vector3(0f, 0.2f, -0.15f), 1.25f) }, // la capa, envolviendo el abdomen
            },
            // Hormiga: el abdomen tapa; lo de atrás, encima de él.
            ["Migas"] = new Ajuste { espalda = new Vector3(0f, 0.55f, -0.45f) },
            // Serpiente: el cuerpo es estrecho; lo del cuerpo, mucho más pequeño.
            ["Serafin"] = new Ajuste { tallaFrente = 0.75f, tallaEspalda = 0.65f, tallaAnillo = 0.62f },
            // Rana: los ojos están arriba y salen mucho; lo de la cara va más adelante.
            ["Croqueta"] = new Ajuste
            {
                cara = new Vector3(0f, 0.02f, 0.2f),
                prendas = { ["NarizPayaso"] = (new Vector3(0f, -0.02f, 0.22f), 0.6f) }, // con sus ojos tan separados, la nariz salía enorme
            },
        };

        public static Ajuste De(int especie)
        {
            var especies = PersonajeBicho.Especies;
            string nombre = especies[Mathf.Clamp(especie, 0, especies.Length - 1)];
            return PorEspecie.TryGetValue(nombre, out var a) ? a : Normal;
        }

        static readonly Ajuste Normal = new Ajuste();

        public static (Vector3 desplazamiento, float talla) De(Ajuste a, Zona zona, string prenda) =>
            a.prendas.TryGetValue(prenda, out var propio) ? propio : zona switch
        {
            Zona.Gorro => (a.gorro, a.tallaGorro),
            Zona.Cara => (a.cara, a.tallaCara),
            Zona.Frente => (a.frente, a.tallaFrente),
            Zona.Espalda => (a.espalda, a.tallaEspalda),
            _ => (a.anillo, a.tallaAnillo),
        };
    }
}
