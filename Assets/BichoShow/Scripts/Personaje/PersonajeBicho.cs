using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Qué bicho es este concursante: Bichete (mariquita), Zumbi (abeja), Concha (caracol),
    /// Cornelio (escarabajo) o Chispa (luciérnaga). Todos comparten esqueleto, animaciones y
    /// anclajes, así que cambiar de uno a otro es cambiar el modelo que cuelga de "Aspecto" y
    /// reconectar el animador, el vestuario, el color del jugador y la silueta. Los modelos son
    /// prefabs en Resources/Personajes (los crea "BichoShow > Personaje: preparar los personajes").
    /// El prefab del bicho ya trae a Bichete puesto.
    /// </summary>
    public class PersonajeBicho : MonoBehaviour
    {
        public static readonly string[] Especies = { "Bichete", "Zumbi", "Concha", "Cornelio", "Chispa" };
        public static readonly string[] Nombres = { "Bichete (mariquita)", "Zumbi (abeja)", "Concha (caracol)", "Cornelio (escarabajo)", "Chispa (luciérnaga)" };

        public int Especie { get; private set; }

        /// <summary>Cambia de personaje (no hace nada si ya lo es o si su modelo no está preparado).</summary>
        public void Poner(int especie)
        {
            especie = Mathf.Clamp(especie, 0, Especies.Length - 1);
            if (especie == Especie) return;
            var prefab = Resources.Load<ModeloBicho>($"Personajes/{Especies[especie]}");
            if (prefab == null)
            {
                Debug.LogWarning($"Bicho Show: falta el personaje {Especies[especie]}. Usa BichoShow > Personaje: preparar los personajes.");
                return;
            }
            var animadorBicho = GetComponent<AnimadorBicho>();
            var aspecto = animadorBicho != null && animadorBicho.raizVisual != null ? animadorBicho.raizVisual : transform.Find("Aspecto");
            if (aspecto == null) return;

            // Fuera el modelo de antes (con su ropa y su silueta, que cuelgan de él).
            var viejo = aspecto.Find("Modelo");
            if (viejo != null)
            {
                viejo.name = "Modelo (fuera)";
                viejo.gameObject.SetActive(false); // ya no cuenta (Destroy espera al final del fotograma)
                Destroy(viejo.gameObject);
            }

            var modelo = Instantiate(prefab, aspecto);
            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;
            Especie = especie;

            var bicho = GetComponent<ControladorBicho>();
            bool retirado = bicho != null && bicho.Retirado;
            if (animadorBicho != null) animadorBicho.Reconectar(modelo);
            if (bicho != null)
            {
                bicho.renderersColor = modelo.piel;
                if (bicho.ColorJugador.a > 0f) bicho.AsignarColor(bicho.ColorJugador);
            }
            if (TryGetComponent(out Vestuario vestuario)) vestuario.CambiarAnclas(modelo.anclaGorro, modelo.anclaCara, modelo.anclaPecho, aspecto);
            if (TryGetComponent(out SiluetaBicho silueta)) silueta.Rehacer();
            // Si el bicho está fuera (saboteando, eliminado…), el modelo nuevo tampoco se ve.
            if (retirado) foreach (var r in modelo.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            if (TryGetComponent(out JugoBicho jugo)) jugo.Aplastar(); // ¡boing! al cambiar
        }
    }
}
