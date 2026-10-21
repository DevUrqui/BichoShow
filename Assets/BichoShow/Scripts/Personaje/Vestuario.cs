using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Gorros, cosas de la cara y cosas del cuerpo de Bichete. Cuelgan de los anclajes de su
    /// esqueleto (AnclaGorro, AnclaCara, AnclaPecho), así siguen al cuerpo al andar, saltar o caerse.
    /// Las prendas son prefabs en Resources/Vestuario (los crea el menú del personaje, y se rehacen
    /// solos cuando cambia el kit de vestuario). Lo que elige cada jugador se guarda; los bots se
    /// visten al azar.
    /// </summary>
    public class Vestuario : MonoBehaviour
    {
        public static readonly string[] Gorros =
        {
            "", "Corona", "Gorra", "Chistera", "GorroFiesta", "Vikingo", "GorroHelice", "Flor",
            "GorroChef", "Vaquero", "Bruja", "GorroLana", "OrejasConejo", "Seta", "HuevoFrito", "Aureola", "Pirata",
        };
        public static readonly string[] NombresGorros =
        {
            "Sin gorro", "Corona", "Gorra", "Chistera", "Gorro de fiesta", "Casco vikingo", "Gorro con hélice", "Flor",
            "Gorro de chef", "Sombrero vaquero", "Sombrero de bruja", "Gorro de lana", "Orejas de conejo", "Seta", "Huevo frito", "Aureola", "Sombrero pirata",
        };
        /// <summary>Lo de la cara (delante de los ojos).</summary>
        public static readonly string[] Complementos =
        {
            "", "GafasSol", "GafasRedondas", "Bigote", "NarizPayaso",
            "GafasCorazon", "Monoculo", "ParcheOjo", "GafasBucear", "Barba", "Chupete",
        };
        public static readonly string[] NombresComplementos =
        {
            "Nada", "Gafas de sol", "Gafas redondas", "Bigote", "Nariz de payaso",
            "Gafas de corazón", "Monóculo", "Parche pirata", "Gafas de bucear", "Barba de mago", "Chupete",
        };
        /// <summary>Lo del cuerpo (pecho y espalda).</summary>
        public static readonly string[] Cuerpos =
        {
            "", "Pajarita", "Medalla", "CollarPerlas", "Babero", "Corbata", "Bufanda", "Capa", "Mochila",
        };
        public static readonly string[] NombresCuerpos =
        {
            "Nada", "Pajarita", "Medalla de oro", "Collar de perlas", "Babero", "Corbata", "Bufanda", "Capa", "Mochila",
        };

        [Header("Anclajes (los pone la herramienta del personaje)")]
        public Transform anclaGorro;
        public Transform anclaCara;
        public Transform anclaPecho;
        [Tooltip("Lo que marca 'de pie y mirando al frente' (el objeto Aspecto).")]
        public Transform referencia;

        public int Gorro { get; private set; }
        public int Complemento { get; private set; }
        public int Cuerpo { get; private set; }

        Quaternion giroGorro = Quaternion.identity, giroCara = Quaternion.identity, giroPecho = Quaternion.identity;
        GameObject puestoGorro, puestoComplemento, puestoCuerpo;
        Transform helice;
        bool listo;

        void Awake() => Preparar();

        /// <summary>
        /// En la pose de reposo (antes de que se anime nada), se apunta qué giro hay que dar a una
        /// prenda colgada de cada anclaje para que quede derecha y mirando al frente.
        /// </summary>
        void Preparar()
        {
            if (listo) return;
            listo = true;
            Quaternion recto = (referencia != null ? referencia : transform).rotation;
            if (anclaGorro != null) giroGorro = Quaternion.Inverse(anclaGorro.rotation) * recto;
            if (anclaCara != null) giroCara = Quaternion.Inverse(anclaCara.rotation) * recto;
            if (anclaPecho != null) giroPecho = Quaternion.Inverse(anclaPecho.rotation) * recto;
        }

        /// <summary>Cambia el gorro y lo de la cara (lo del cuerpo se queda como está).</summary>
        public void Poner(int gorro, int complemento) => Poner(gorro, complemento, Cuerpo);

        public void Poner(int gorro, int complemento, int cuerpo)
        {
            Preparar();
            Gorro = Mathf.Clamp(gorro, 0, Gorros.Length - 1);
            Complemento = Mathf.Clamp(complemento, 0, Complementos.Length - 1);
            Cuerpo = Mathf.Clamp(cuerpo, 0, Cuerpos.Length - 1);

            if (puestoGorro != null) Destroy(puestoGorro);
            if (puestoComplemento != null) Destroy(puestoComplemento);
            if (puestoCuerpo != null) Destroy(puestoCuerpo);
            helice = null;

            puestoGorro = Colgar(Gorros[Gorro], anclaGorro, giroGorro);
            if (puestoGorro != null) helice = puestoGorro.transform.Find("HeliceAspas");
            puestoComplemento = Colgar(Complementos[Complemento], anclaCara, giroCara);
            puestoCuerpo = Colgar(Cuerpos[Cuerpo], anclaPecho, giroPecho);
        }

        GameObject Colgar(string prenda, Transform ancla, Quaternion giro)
        {
            if (string.IsNullOrEmpty(prenda) || ancla == null) return null;
            var prefab = Resources.Load<GameObject>($"Vestuario/{prenda}");
            if (prefab == null) return null;
            var go = Instantiate(prefab, ancla);
            go.name = prenda;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = giro;
            Vector3 escala = ancla.lossyScale;
            go.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, escala.x), 1f / Mathf.Max(0.001f, escala.y), 1f / Mathf.Max(0.001f, escala.z));

            // Si el bicho está retirado (saboteando), la prenda tampoco se ve.
            if (TryGetComponent(out ControladorBicho bicho) && bicho.Retirado)
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = false;
            return go;
        }

        void Update()
        {
            if (helice != null) helice.Rotate(0f, 900f * Time.deltaTime, 0f, Space.Self);
        }

        // ------------------------------------------------------------------
        // Lo que eligió cada jugador
        // ------------------------------------------------------------------

        static string ClaveGorro(int jugador) => $"BichoShow.Vestuario.Gorro{jugador}";
        static string ClaveComplemento(int jugador) => $"BichoShow.Vestuario.Complemento{jugador}";
        static string ClaveCuerpo(int jugador) => $"BichoShow.Vestuario.Cuerpo{jugador}";

        public static void Guardar(int jugador, int gorro, int complemento, int cuerpo)
        {
            PlayerPrefs.SetInt(ClaveGorro(jugador), gorro);
            PlayerPrefs.SetInt(ClaveComplemento(jugador), complemento);
            PlayerPrefs.SetInt(ClaveCuerpo(jugador), cuerpo);
            PlayerPrefs.Save();
        }

        /// <summary>Le pone a un jugador lo que eligió la última vez.</summary>
        public static void PonerGuardado(ControladorBicho bicho)
        {
            if (bicho == null || !bicho.TryGetComponent(out Vestuario v)) return;
            int i = bicho.IndiceJugador;
            v.Poner(PlayerPrefs.GetInt(ClaveGorro(i), 0), PlayerPrefs.GetInt(ClaveComplemento(i), 0), PlayerPrefs.GetInt(ClaveCuerpo(i), 0));
        }

        /// <summary>Viste a un bot al azar (casi siempre con algo puesto).</summary>
        public static void PonerAlAzar(ControladorBicho bicho)
        {
            if (bicho == null || !bicho.TryGetComponent(out Vestuario v)) return;
            v.Poner(Random.value < 0.85f ? Random.Range(1, Gorros.Length) : 0,
                    Random.value < 0.55f ? Random.Range(1, Complementos.Length) : 0,
                    Random.value < 0.5f ? Random.Range(1, Cuerpos.Length) : 0);
        }
    }
}
