using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Pintabichos" (2 contra 2):
    ///  - El suelo es una cuadrícula de baldosas. Al pisar una, se pinta del color de tu equipo
    ///    (también las del rival: ¡se las robas!).
    ///  - De vez en cuando aparece un bote de pintura: quien lo coge salpica un buen círculo.
    ///  - En los últimos segundos, pintura doble: cada paso pinta también las de alrededor.
    ///  - Gana el equipo con más suelo pintado al sonar la bocina.
    /// </summary>
    public class RondaPintura : RondaBase
    {
        [Header("Pintabichos")]
        [Tooltip("Las baldosas, fila a fila (columnas × filas). Solo se ven; se pisa el suelo de debajo.")]
        public Renderer[] baldosas;
        public int columnas = 14, filas = 14;
        [Tooltip("Centro de la baldosa [0, 0] y distancia entre baldosas.")]
        public Vector3 origen;
        public float paso = 1.4f;
        public Material neutra, roja, azul;

        [Header("Botes de pintura")]
        public GameObject plantillaBote;
        public float cadaBote = 9f;
        public int maxBotes = 2;
        public float radioSalpicon = 2.8f;
        public float segundosPinturaDoble = 15f;

        public string[] nombres = { "Rojo", "Azul" };
        public Color[] colores = { new Color(1f, 0.38f, 0.35f), new Color(0.3f, 0.6f, 1f) };

        int[] duena;                       // -1 nadie, 0 rojo, 1 azul
        Vector3[] escalas;
        readonly int[] cuenta = new int[2];
        readonly List<(int indice, float desde)> saltando = new List<(int, float)>();
        readonly List<GameObject> botes = new List<GameObject>();
        float siguienteBote;
        bool avisoDoble;

        public static RondaPintura Instancia { get; private set; }

        public override string Titulo => "PINTABICHOS";
        protected override string Descripcion => "Pisa el suelo para pintarlo del color de tu equipo. ¡Gana quien tenga más suelo pintado!";
        protected override string ConsejoInicio => "¡Corre por todas partes pintando! Los botes salpican a lo grande";
        public override string NombreEquipo(int equipo) => nombres[equipo];
        public override Color ColorEquipo(int equipo) => colores[equipo];
        public override int PuntosEquipo(int equipo) => cuenta[equipo];
        int Total => columnas * filas;
        bool Doble => TiempoRestante <= segundosPinturaDoble;

        /// <summary>De quién es la baldosa más cercana a un punto (-1 nadie), y su centro.</summary>
        public int DuenaEn(Vector3 punto) => Indice(punto, out int i) ? duena[i] : -2;
        public Vector3 CentroBaldosa(int x, int z) => origen + new Vector3(x * paso, 0f, z * paso);
        public int Duena(int x, int z) => duena != null ? duena[z * columnas + x] : -1;
        public IEnumerable<Vector3> Botes()
        {
            foreach (var b in botes) if (b != null) yield return b.transform.position;
        }
        public Vector3 Centro => origen + new Vector3((columnas - 1) * paso * 0.5f, 0f, (filas - 1) * paso * 0.5f);

        void Awake()
        {
            Instancia = this;
            duena = new int[baldosas != null ? baldosas.Length : 0];
            escalas = new Vector3[duena.Length];
            for (int i = 0; i < duena.Length; i++)
            {
                duena[i] = -1;
                if (baldosas[i] != null) escalas[i] = baldosas[i].transform.localScale;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotPintura>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override void AlPrepararRonda()
        {
            for (int i = 0; i < duena.Length; i++) Pintar(i, -1, false);
            cuenta[0] = cuenta[1] = 0;
            foreach (var b in botes) if (b != null) Destroy(b);
            botes.Clear();
            avisoDoble = false;
        }

        protected override void AlEmpezarRonda()
        {
            siguienteBote = Time.time + cadaBote * 0.6f;
            Mensaje("¡A PINTAR!", Color.white, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            // Cada bicho en el suelo pinta lo que pisa.
            foreach (var b in ControladorBicho.Todos)
            {
                if (b.Retirado || !b.EnSuelo) continue;
                int equipo = EquipoDe(b);
                if (!Indice(b.transform.position, out int i)) continue;
                PintarPor(i, equipo, b);
                if (Doble)
                {
                    int x = i % columnas, z = i / columnas;
                    foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        if (Dentro(x + dx, z + dz)) PintarPor((z + dz) * columnas + x + dx, equipo, b);
                }
            }

            // Botes de pintura
            botes.RemoveAll(b => b == null);
            if (Time.time >= siguienteBote)
            {
                siguienteBote = Time.time + cadaBote;
                if (botes.Count < maxBotes) PonerBote();
            }
            for (int k = botes.Count - 1; k >= 0; k--)
            {
                var bote = botes[k];
                bote.transform.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
                Vector3 p = bote.transform.position;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b.Retirado) continue;
                    if ((b.Cuerpo.worldCenterOfMass - (p + Vector3.up * 0.4f)).sqrMagnitude > 1.3f * 1.3f) continue;
                    Salpicar(p, EquipoDe(b), b);
                    Destroy(bote);
                    botes.RemoveAt(k);
                    break;
                }
            }

            if (!avisoDoble && Doble)
            {
                avisoDoble = true;
                Mensaje("¡ÚLTIMOS SEGUNDOS: PINTURA DOBLE!", EstiloShow.Oro, 3f);
                Sonidos.Reproducir(Sonido.Alarma);
            }
        }

        protected override void AlTerminarRonda()
        {
            foreach (var b in botes) if (b != null) Destroy(b);
            botes.Clear();
        }

        protected override void DibujarInfoEquipo(int equipo, float x, float ancho, float h)
        {
            float parte = Total > 0 ? cuenta[equipo] / (float)Total : 0f;
            Barra(new Rect(x, h * 0.095f, ancho, h * 0.025f), parte, colores[equipo]);
            Texto(new Rect(x, h * 0.125f, ancho, h * 0.04f), $"{Mathf.RoundToInt(parte * 100f)}% del suelo", estiloPequeno, Color.white);
        }

        void AnimarSaltos()
        {
            for (int k = saltando.Count - 1; k >= 0; k--)
            {
                var (i, desde) = saltando[k];
                float t = Time.time - desde;
                if (t > 0.35f || baldosas[i] == null)
                {
                    if (baldosas[i] != null) baldosas[i].transform.localScale = escalas[i];
                    saltando.RemoveAt(k);
                    continue;
                }
                float s = 1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.25f;
                baldosas[i].transform.localScale = new Vector3(escalas[i].x * s, escalas[i].y * (1f + (s - 1f) * 6f), escalas[i].z * s);
            }
        }

        protected override void Update()
        {
            base.Update();
            AnimarSaltos();
        }

        // ---------------------------------------------------------------------
        // Pintar
        // ---------------------------------------------------------------------

        bool Dentro(int x, int z) => x >= 0 && z >= 0 && x < columnas && z < filas;

        bool Indice(Vector3 punto, out int indice)
        {
            int x = Mathf.RoundToInt((punto.x - origen.x) / paso);
            int z = Mathf.RoundToInt((punto.z - origen.z) / paso);
            indice = z * columnas + x;
            return Dentro(x, z) && duena != null && indice < duena.Length;
        }

        void PintarPor(int i, int equipo, ControladorBicho quien)
        {
            int antes = duena[i];
            if (antes == equipo) return;
            Pintar(i, equipo, true);
            if (antes >= 0) Sonidos.Reproducir(Sonido.Paso, baldosas[i].transform.position, 0.6f, 1.6f, 0.2f); // ¡robada!
        }

        void Pintar(int i, int equipo, bool saltar)
        {
            int antes = duena[i];
            if (antes == equipo) return;
            if (antes >= 0) cuenta[antes]--;
            duena[i] = equipo;
            if (equipo >= 0) cuenta[equipo]++;
            if (baldosas[i] != null)
            {
                baldosas[i].sharedMaterial = equipo == 0 ? roja : equipo == 1 ? azul : neutra;
                if (saltar) saltando.Add((i, Time.time));
                else baldosas[i].transform.localScale = escalas[i];
            }
        }

        void PonerBote()
        {
            if (plantillaBote == null) return;
            // En una baldosa al azar, lejos del borde.
            int x = Random.Range(2, columnas - 2), z = Random.Range(2, filas - 2);
            var bote = Instantiate(plantillaBote, CentroBaldosa(x, z), Quaternion.identity);
            bote.SetActive(true);
            botes.Add(bote);
            Efectos.Impacto(bote.transform.position + Vector3.up * 0.5f, Color.white, 0.5f);
            Sonidos.Reproducir(Sonido.Aparecer, bote.transform.position);
        }

        void Salpicar(Vector3 punto, int equipo, ControladorBicho quien)
        {
            int pintadas = 0;
            for (int i = 0; i < duena.Length; i++)
            {
                if (baldosas[i] == null) continue;
                Vector3 c = baldosas[i].transform.position;
                if (new Vector2(c.x - punto.x, c.z - punto.z).sqrMagnitude > radioSalpicon * radioSalpicon) continue;
                if (duena[i] != equipo) pintadas++;
                Pintar(i, equipo, true);
            }
            Sonidos.Reproducir(Sonido.Rotura, punto, 0.6f, 1.4f);
            Sonidos.Reproducir(Sonido.Golpe, punto, 0.6f, 0.8f);
            Efectos.Impacto(punto + Vector3.up * 0.4f, colores[equipo], 1f);
            Efectos.Sacudir(0.2f);
            Mensaje($"¡SALPICÓN de {NombreJugador(quien)}!  +{pintadas} baldosas", colores[equipo], 2f);
            Estadisticas.Evento("salpicon", quien, $"{pintadas} baldosas");
        }
    }
}
