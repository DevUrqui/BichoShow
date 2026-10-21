using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BichoShow
{
    /// <summary>
    /// La escena de una prueba, copiada del anfitrión a los invitados de una partida online.
    ///
    /// Los dos ordenadores cargan la misma escena. En el invitado se apaga toda la lógica
    /// (pruebas, bots, trampas, cámaras: los scripts del juego) y los cuerpos físicos se vuelven
    /// cinemáticos; a partir de ahí, lo que se ve es lo que cuenta el anfitrión:
    ///  - Movimientos: cualquier objeto de la escena que se mueva (muebles, pelota, plataformas,
    ///    la cámara…), unas 30 veces por segundo, sin garantía de entrega (llega el siguiente).
    ///  - Cambios (con garantía): objetos que aparecen (muebles que caen, pedazos, charcos…) con su
    ///    malla y materiales, objetos que desaparecen o se encienden/apagan y colores que cambian
    ///    (baldosas pintadas, trampas avisando, la bomba parpadeando).
    /// No hace falta tocar cada prueba: el anfitrión vigila los Transform que cambian
    /// (transform.hasChanged) y los Renderer, y cada objeto se reconoce por su ruta en la jerarquía.
    /// Los bichos van aparte (RedShow + MarionetaBicho).
    /// </summary>
    public class EspejoEscena
    {
        public const ushort SinPadre = ushort.MaxValue;
        const byte MovMundo = 1, MovEscala = 2;
        const byte CambioRegistro = 1, CambioAlta = 2, CambioBaja = 3, CambioActivo = 4, CambioRender = 5, CambioPose = 6;
        /// <summary>Tamaño máximo de cada trozo de movimientos (para no pasar del tamaño de un paquete).</summary>
        public const int TamanoTrozo = 1100;
        /// <summary>Tras pararse, un objeto se sigue mandando este rato (por si se perdió el último movimiento).</summary>
        const float RepetirTrasParar = 0.5f;
        static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        /// <summary>Número de la escena (cada carga suma uno): los mensajes de otra escena se tiran.</summary>
        public int Numero { get; private set; }
        /// <summary>Anfitrión: se está copiando esta escena. Invitado: la escena está lista y se copia.</summary>
        public bool Activo { get; private set; }

        // =====================================================================
        // ANFITRIÓN
        // =====================================================================

        class Seguido
        {
            public Transform t;
            public ushort id;
            public bool registrado, generado;
            public string ruta;
            public int hijos;
            public bool activo;
            public MeshRenderer render;
            public bool renderActivo;
            public Material[] materiales = Array.Empty<Material>();
            public Color[] colores = Array.Empty<Color>();
            // Último movimiento mandado
            public bool hayPose, mundo;
            public Seguido padre;
            public Vector3 p, s;
            public Quaternion r;
            public float ultimoMovimiento = -10f;
        }

        readonly List<Seguido> seguidos = new List<Seguido>();
        readonly Dictionary<Transform, Seguido> porTransform = new Dictionary<Transform, Seguido>();
        readonly HashSet<Transform> ignorados = new HashSet<Transform>();
        readonly HashSet<GameObject> raices = new HashSet<GameObject>();
        readonly List<GameObject> raicesTmp = new List<GameObject>();
        readonly List<Material> matsTmp = new List<Material>();
        readonly List<Seguido> movidos = new List<Seguido>();
        readonly List<ulong> listos = new List<ulong>();
        readonly Dictionary<string, int> nombresTmp = new Dictionary<string, int>();
        readonly MemoryStream memCambios = new MemoryStream(4096);
        readonly BinaryWriter cambios;
        readonly MemoryStream memMov = new MemoryStream(TamanoTrozo + 64);
        readonly BinaryWriter mov;
        Scene escena;
        ushort siguienteId;

        public IReadOnlyList<ulong> Listos => listos;
        public bool HayListos => listos.Count > 0;

        public EspejoEscena()
        {
            cambios = new BinaryWriter(memCambios);
            mov = new BinaryWriter(memMov);
        }

        /// <summary>El anfitrión ha cargado una prueba: empezar a vigilarla.</summary>
        public void EmpezarAnfitrion(Scene s)
        {
            Limpiar();
            Numero++;
            escena = s;
            Activo = true;
            siguienteId = 1;

            s.GetRootGameObjects(raicesTmp);
            nombresTmp.Clear();
            foreach (var go in raicesTmp)
            {
                raices.Add(go);
                string ruta = Segmento(go.name, nombresTmp);
                if (ExcluirDeEscena(go.transform)) { ignorados.Add(go.transform); continue; }
                SeguirEscena(go.transform, ruta);
            }
        }

        /// <summary>Otra escena (o vuelta al menú): dejar de vigilar. Un número nuevo invalida lo que quede por llegar.</summary>
        public void Parar()
        {
            if (Activo) Numero++;
            Limpiar();
        }

        void Limpiar()
        {
            Activo = false;
            seguidos.Clear();
            porTransform.Clear();
            ignorados.Clear();
            raices.Clear();
            listos.Clear();
            memCambios.SetLength(0);
            LimpiarInvitado();
        }

        static string Segmento(string nombre, Dictionary<string, int> vistos)
        {
            vistos.TryGetValue(nombre, out int k);
            vistos[nombre] = k + 1;
            return k == 0 ? nombre : $"{nombre}#{k}";
        }

        void SeguirEscena(Transform t, string ruta)
        {
            var s = Nuevo(t);
            s.ruta = ruta;
            var hermanos = new Dictionary<string, int>();
            for (int i = 0; i < t.childCount; i++)
            {
                var hijo = t.GetChild(i);
                string rutaHijo = ruta + "/" + Segmento(hijo.name, hermanos);
                if (ExcluirDeEscena(hijo)) { ignorados.Add(hijo); continue; }
                SeguirEscena(hijo, rutaHijo);
            }
        }

        Seguido Nuevo(Transform t)
        {
            var s = new Seguido { t = t, hijos = t.childCount, activo = t.gameObject.activeSelf };
            s.render = t.GetComponent<MeshRenderer>();
            if (s.render != null) Foto(s);
            t.hasChanged = false;
            seguidos.Add(s);
            porTransform[t] = s;
            return s;
        }

        /// <summary>Lo que anima cada ordenador por su cuenta (público) o va aparte (bichos).</summary>
        static bool ExcluirDeEscena(Transform t) =>
            t.GetComponent<ControladorBicho>() != null || t.GetComponent<PublicoAnimado>() != null;

        /// <summary>De lo que aparece durante la prueba, lo que no se copia: bichos, partículas, cámaras, gestores.</summary>
        static bool ExcluirGenerado(Transform t) =>
            ExcluirDeEscena(t) || t.GetComponent<ParticleSystem>() != null || t.GetComponent<Camera>() != null ||
            t.name.StartsWith("Bicho Show");

        /// <summary>Un invitado ha cargado la escena: se le manda todo lo que ya se conoce de ella.</summary>
        public byte[] Listo(ulong cliente, int numero, out int longitud)
        {
            longitud = 0;
            if (!Activo || numero != Numero) return null;
            if (!listos.Contains(cliente)) listos.Add(cliente);

            using var memoria = new MemoryStream(8192);
            using var w = new BinaryWriter(memoria);
            foreach (var s in seguidos)
            {
                if (!s.registrado || s.t == null) continue;
                if (s.generado) EscribirAlta(w, s);
                else
                {
                    w.Write(CambioRegistro); w.Write(s.id); w.Write(s.ruta);
                    w.Write(CambioActivo); w.Write(s.id); w.Write(s.t.gameObject.activeSelf);
                    if (s.render != null) EscribirRender(w, s);
                }
                if (s.hayPose)
                {
                    w.Write(CambioPose);
                    EscribirPose(w, s);
                }
            }
            w.Flush();
            longitud = (int)memoria.Length;
            return memoria.ToArray();
        }

        public void Quitar(ulong cliente) => listos.Remove(cliente);

        /// <summary>
        /// Una vuelta de vigilancia (unas 30 por segundo). Devuelve los cambios (con garantía) y
        /// llama a "enviarTrozo" con cada trozo de movimientos.
        /// </summary>
        public void Vigilar(float tiempo, out byte[] datosCambios, out int largoCambios, Action<byte[], int> enviarTrozo)
        {
            datosCambios = null;
            largoCambios = 0;
            if (!Activo || !escena.IsValid()) return;
            memCambios.SetLength(0);

            // Objetos nuevos en la raíz de la escena (muebles que caen, pedazos, bots… estos no).
            escena.GetRootGameObjects(raicesTmp);
            foreach (var go in raicesTmp)
            {
                if (!raices.Add(go)) continue;
                if (ExcluirGenerado(go.transform)) ignorados.Add(go.transform);
                else Generar(go.transform, null);
            }

            movidos.Clear();
            for (int i = 0; i < seguidos.Count; i++)
            {
                var s = seguidos[i];
                var t = s.t;
                if (t == null)
                {
                    // Destruido (entregado, roto…). Si nunca se movió, el invitado aún no lo conoce: se registra y se da de baja.
                    if (!s.registrado && s.ruta != null) Asegurar(s);
                    if (s.registrado) { cambios.Write(CambioBaja); cambios.Write(s.id); }
                    seguidos.RemoveAt(i--);
                    continue;
                }

                // Hijos nuevos (grietas, cordones…)
                if (t.childCount != s.hijos)
                {
                    s.hijos = t.childCount;
                    for (int c = 0; c < t.childCount; c++)
                    {
                        var hijo = t.GetChild(c);
                        if (porTransform.ContainsKey(hijo) || ignorados.Contains(hijo)) continue;
                        if (ExcluirGenerado(hijo)) ignorados.Add(hijo);
                        else Generar(hijo, s);
                    }
                }

                bool activo = t.gameObject.activeSelf;
                if (activo != s.activo)
                {
                    s.activo = activo;
                    Asegurar(s);
                    cambios.Write(CambioActivo); cambios.Write(s.id); cambios.Write(activo);
                }

                if (s.render != null && RenderCambiado(s))
                {
                    Asegurar(s);
                    Foto(s);
                    EscribirRender(cambios, s);
                }

                if (t.hasChanged)
                {
                    t.hasChanged = false;
                    if (PoseCambiada(s))
                    {
                        Asegurar(s);
                        if (s.padre != null) Asegurar(s.padre);
                        s.ultimoMovimiento = tiempo;
                    }
                }
                if (s.registrado && s.hayPose && tiempo - s.ultimoMovimiento < RepetirTrasParar) movidos.Add(s);
            }

            cambios.Flush();
            if (memCambios.Length > 0)
            {
                datosCambios = memCambios.GetBuffer();
                largoCambios = (int)memCambios.Length;
            }

            // Movimientos, en trozos que quepan en un paquete.
            int k = 0;
            while (k < movidos.Count)
            {
                memMov.SetLength(0);
                mov.Write(Numero);
                mov.Write(tiempo);
                long posCuenta = memMov.Position;
                mov.Write((ushort)0);
                ushort n = 0;
                while (k < movidos.Count && memMov.Length < TamanoTrozo)
                {
                    EscribirPose(mov, movidos[k++]);
                    n++;
                }
                mov.Flush();
                long fin = memMov.Position;
                memMov.Position = posCuenta;
                mov.Write(n);
                mov.Flush();
                memMov.Position = fin;
                enviarTrozo(memMov.GetBuffer(), (int)memMov.Length);
            }
        }

        void Asegurar(Seguido s)
        {
            if (s.registrado) return;
            s.id = siguienteId++;
            if (siguienteId == SinPadre) siguienteId = 1;
            s.registrado = true;
            cambios.Write(CambioRegistro);
            cambios.Write(s.id);
            cambios.Write(s.ruta ?? "");
        }

        /// <summary>Un objeto nuevo (y lo que lleve dentro): se manda entero, con su malla y materiales.</summary>
        void Generar(Transform t, Seguido padre)
        {
            if (padre != null) Asegurar(padre);
            var s = Nuevo(t);
            s.generado = true;
            s.registrado = true;
            s.id = siguienteId++;
            if (siguienteId == SinPadre) siguienteId = 1;
            s.padre = padre;
            EscribirAlta(cambios, s);
            for (int i = 0; i < t.childCount; i++)
            {
                var hijo = t.GetChild(i);
                if (porTransform.ContainsKey(hijo)) continue;
                if (ExcluirGenerado(hijo)) { ignorados.Add(hijo); continue; }
                Generar(hijo, s);
            }
        }

        void EscribirAlta(BinaryWriter w, Seguido s)
        {
            var t = s.t;
            porTransform.TryGetValue(t.parent != null ? t.parent : t, out var padre);
            if (padre == s) padre = null;
            bool mundo = t.parent != null && (padre == null || !padre.registrado);
            w.Write(CambioAlta);
            w.Write(s.id);
            w.Write(mundo || padre == null ? SinPadre : padre.id);
            w.Write(t.name);
            w.Write(t.gameObject.activeSelf);
            w.Write(mundo);
            EscribirV3(w, mundo ? t.position : t.localPosition);
            EscribirQ(w, mundo ? t.rotation : t.localRotation);
            EscribirV3(w, mundo ? t.lossyScale : t.localScale);
            var filtro = t.GetComponent<MeshFilter>();
            var malla = filtro != null ? filtro.sharedMesh : null;
            w.Write(malla != null);
            if (malla != null)
            {
                w.Write(malla.name);
                w.Write(malla.vertexCount);
            }
            w.Write(s.render != null);
            if (s.render != null) EscribirDatosRender(w, s);
        }

        bool PoseCambiada(Seguido s)
        {
            var t = s.t;
            Seguido padre = null;
            bool mundo = t.parent != null && !porTransform.TryGetValue(t.parent, out padre);
            Vector3 p = mundo ? t.position : t.localPosition;
            Quaternion r = mundo ? t.rotation : t.localRotation;
            Vector3 sc = mundo ? t.lossyScale : t.localScale;
            if (s.hayPose && s.mundo == mundo && s.padre == padre && (p - s.p).sqrMagnitude < 1e-8f &&
                Quaternion.Angle(r, s.r) < 0.05f && (sc - s.s).sqrMagnitude < 1e-8f)
                return false;
            s.hayPose = true;
            s.mundo = mundo;
            s.padre = padre;
            s.p = p;
            s.r = r;
            s.s = sc;
            return true;
        }

        static void EscribirPose(BinaryWriter w, Seguido s)
        {
            bool escala = (s.s - Vector3.one).sqrMagnitude > 1e-8f;
            w.Write(s.id);
            w.Write((byte)((s.mundo ? MovMundo : 0) | (escala ? MovEscala : 0)));
            if (!s.mundo) w.Write(s.padre != null && s.padre.registrado ? s.padre.id : SinPadre);
            EscribirV3(w, s.p);
            EscribirQ(w, s.r);
            if (escala) EscribirV3(w, s.s);
        }

        bool RenderCambiado(Seguido s)
        {
            var r = s.render;
            if (r.enabled != s.renderActivo) return true;
            r.GetSharedMaterials(matsTmp);
            if (matsTmp.Count != s.materiales.Length) return true;
            for (int i = 0; i < matsTmp.Count; i++)
            {
                var m = matsTmp[i];
                if (m != s.materiales[i]) return true;
                if (m != null && m.HasProperty(IdColorBase) && !Parecido(m.GetColor(IdColorBase), s.colores[i])) return true;
            }
            return false;
        }

        void Foto(Seguido s)
        {
            s.renderActivo = s.render.enabled;
            s.render.GetSharedMaterials(matsTmp);
            s.materiales = matsTmp.ToArray();
            s.colores = new Color[s.materiales.Length];
            for (int i = 0; i < s.materiales.Length; i++)
            {
                var m = s.materiales[i];
                s.colores[i] = m != null && m.HasProperty(IdColorBase) ? m.GetColor(IdColorBase) : Color.white;
            }
        }

        static bool Parecido(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f && Mathf.Abs(a.a - b.a) < 0.004f;

        static void EscribirRender(BinaryWriter w, Seguido s)
        {
            w.Write(CambioRender);
            w.Write(s.id);
            EscribirDatosRender(w, s);
        }

        static void EscribirDatosRender(BinaryWriter w, Seguido s)
        {
            w.Write(s.render.enabled);
            w.Write((byte)s.materiales.Length);
            for (int i = 0; i < s.materiales.Length; i++)
            {
                var m = s.materiales[i];
                w.Write(m != null ? NombreBase(m.name) : "");
                w.Write(m != null && m.shader != null ? m.shader.name : "");
                Color32 c = s.colores[i];
                w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a);
            }
        }

        static string NombreBase(string nombre)
        {
            while (nombre.EndsWith(" (Instance)")) nombre = nombre.Substring(0, nombre.Length - " (Instance)".Length);
            return nombre;
        }

        static void EscribirV3(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }

        static void EscribirQ(BinaryWriter w, Quaternion q)
        {
            w.Write(Mathf.FloatToHalf(q.x)); w.Write(Mathf.FloatToHalf(q.y));
            w.Write(Mathf.FloatToHalf(q.z)); w.Write(Mathf.FloatToHalf(q.w));
        }

        static Vector3 LeerV3(BinaryReader l) => new Vector3(l.ReadSingle(), l.ReadSingle(), l.ReadSingle());

        static Quaternion LeerQ(BinaryReader l)
        {
            var q = new Quaternion(Mathf.HalfToFloat(l.ReadUInt16()), Mathf.HalfToFloat(l.ReadUInt16()),
                                   Mathf.HalfToFloat(l.ReadUInt16()), Mathf.HalfToFloat(l.ReadUInt16()));
            float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return m > 1e-5f ? new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m) : Quaternion.identity;
        }

        // =====================================================================
        // INVITADO
        // =====================================================================

        struct FotoMov
        {
            public float tiempo;
            public bool mundo;
            public ushort padre;
            public Vector3 p, s;
            public Quaternion r;
        }

        class Copia
        {
            public Transform t;
            public bool generado;
            public MeshRenderer render;
            public Material[] instancias = Array.Empty<Material>();
            public readonly List<FotoMov> fotos = new List<FotoMov>();
            public bool moviendose;
        }

        readonly Dictionary<ushort, Copia> copias = new Dictionary<ushort, Copia>();
        readonly Dictionary<string, Transform> rutas = new Dictionary<string, Transform>();
        readonly List<Copia> enMovimiento = new List<Copia>();
        static readonly Dictionary<string, Mesh> mallas = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();
        static readonly List<(Mesh malla, Material material)> primitivas = new List<(Mesh, Material)>();
        int numeroPendiente = -1;
        bool espejoPendiente;

        void LimpiarInvitado()
        {
            copias.Clear();
            rutas.Clear();
            enMovimiento.Clear();
        }

        /// <summary>
        /// El anfitrión ha cambiado de escena. Si es una prueba (espejo), al cargarla se le apaga
        /// la lógica; el menú se carga normal (ahí cada uno tiene el suyo).
        /// Devuelve true si ya estaba en esa escena y no hay que esperar a cargarla.
        /// </summary>
        public bool CambiarEscenaInvitado(int numero, string nombre, bool espejo)
        {
            Activo = false;
            LimpiarInvitado();
            if (!espejo && SceneManager.GetActiveScene().name == nombre)
            {
                Numero = numero;
                return true;
            }
            numeroPendiente = numero;
            espejoPendiente = espejo;
            SceneManager.LoadScene(nombre);
            return false;
        }

        /// <summary>Se ha cargado la escena que pidió el anfitrión. Devuelve true si hay que avisarle de que estamos listos.</summary>
        public bool AlCargarEscenaInvitado(Scene s)
        {
            if (numeroPendiente < 0) return false;
            Numero = numeroPendiente;
            numeroPendiente = -1;
            if (!espejoPendiente) return false;

            Neutralizar(s);
            ConstruirRutas(s);
            ConstruirRegistros();
            Activo = true;
            return true;
        }

        /// <summary>Apaga la lógica de la prueba en el invitado: aquí solo se mira.</summary>
        static void Neutralizar(Scene s)
        {
            foreach (var raiz in s.GetRootGameObjects())
            {
                foreach (var mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb is PublicoAnimado || mb is CamaraSinEstorbos) continue; // lo visual de cada pantalla sigue vivo
                    if (mb.GetType().Namespace == "BichoShow") mb.enabled = false;
                }
                foreach (var rb in raiz.GetComponentsInChildren<Rigidbody>(true))
                {
                    rb.interpolation = RigidbodyInterpolation.None;
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                    rb.isKinematic = true;
                }
            }
        }

        void ConstruirRutas(Scene s)
        {
            rutas.Clear();
            var nombres = new Dictionary<string, int>();
            foreach (var go in s.GetRootGameObjects())
                AnotarRuta(go.transform, Segmento(go.name, nombres));
        }

        void AnotarRuta(Transform t, string ruta)
        {
            rutas[ruta] = t;
            var hermanos = new Dictionary<string, int>();
            for (int i = 0; i < t.childCount; i++)
            {
                var hijo = t.GetChild(i);
                AnotarRuta(hijo, ruta + "/" + Segmento(hijo.name, hermanos));
            }
        }

        /// <summary>Mallas y materiales cargados (los de la escena y los de los prefabs que usa), por nombre.</summary>
        static void ConstruirRegistros()
        {
            if (primitivas.Count == 0)
            {
                foreach (PrimitiveType tipo in Enum.GetValues(typeof(PrimitiveType)))
                {
                    var go = GameObject.CreatePrimitive(tipo);
                    primitivas.Add((go.GetComponent<MeshFilter>().sharedMesh, go.GetComponent<Renderer>().sharedMaterial));
                    Object.DestroyImmediate(go);
                }
            }
            mallas.Clear();
            foreach (var (malla, material) in primitivas)
            {
                if (malla != null) mallas[ClaveMalla(malla.name, malla.vertexCount)] = malla;
                if (material != null) materiales[material.name] = material;
            }
            foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>())
            {
                string clave = ClaveMalla(m.name, m.vertexCount);
                if (!mallas.ContainsKey(clave)) mallas[clave] = m;
            }
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
                if (m != null && !m.name.EndsWith(" (Instance)") && !materiales.ContainsKey(m.name)) materiales[m.name] = m;
        }

        static string ClaveMalla(string nombre, int vertices) => nombre + "|" + vertices;

        /// <summary>Cambios con garantía (nuevos, bajas, encendidos, colores) o el estado entero al entrar.</summary>
        public void RecibirCambios(byte[] datos, int longitud)
        {
            if (!Activo) return;
            using var l = new BinaryReader(new MemoryStream(datos, 0, longitud, false));
            try
            {
                while (l.BaseStream.Position < longitud)
                {
                    byte tipo = l.ReadByte();
                    ushort id = l.ReadUInt16();
                    switch (tipo)
                    {
                        case CambioRegistro:
                        {
                            string ruta = l.ReadString();
                            if (rutas.TryGetValue(ruta, out var t) && t != null && !copias.ContainsKey(id))
                                copias[id] = new Copia { t = t, render = t.GetComponent<MeshRenderer>() };
                            break;
                        }
                        case CambioAlta:
                            LeerAlta(l, id);
                            break;
                        case CambioBaja:
                            if (copias.TryGetValue(id, out var baja))
                            {
                                if (baja.t != null) Object.Destroy(baja.t.gameObject);
                                copias.Remove(id);
                            }
                            break;
                        case CambioActivo:
                        {
                            bool activo = l.ReadBoolean();
                            if (copias.TryGetValue(id, out var c) && c.t != null) c.t.gameObject.SetActive(activo);
                            break;
                        }
                        case CambioRender:
                        {
                            copias.TryGetValue(id, out var c);
                            LeerRender(l, c);
                            break;
                        }
                        case CambioPose:
                        {
                            var foto = LeerPose(l, 0f);
                            if (copias.TryGetValue(id, out var c) && c.t != null)
                            {
                                c.fotos.Clear();
                                Aplicar(c, foto);
                            }
                            break;
                        }
                        default:
                            return; // datos de otra versión
                    }
                }
            }
            catch (EndOfStreamException) { }
        }

        void LeerAlta(BinaryReader l, ushort id)
        {
            ushort padre = l.ReadUInt16();
            string nombre = l.ReadString();
            bool activo = l.ReadBoolean();
            bool mundo = l.ReadBoolean();
            Vector3 p = LeerV3(l);
            Quaternion r = LeerQ(l);
            Vector3 s = LeerV3(l);
            bool hayMalla = l.ReadBoolean();
            string nombreMalla = null;
            int vertices = 0;
            if (hayMalla)
            {
                nombreMalla = l.ReadString();
                vertices = l.ReadInt32();
            }
            bool hayRender = l.ReadBoolean();

            if (!copias.TryGetValue(id, out var c) || c.t == null)
            {
                var go = new GameObject(nombre);
                c = new Copia { t = go.transform, generado = true };
                copias[id] = c;
                if (hayMalla)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = mallas.TryGetValue(ClaveMalla(nombreMalla, vertices), out var malla) ? malla : null;
                }
                if (hayRender) c.render = go.AddComponent<MeshRenderer>();
            }

            Transform padreT = padre != SinPadre && copias.TryGetValue(padre, out var cp) ? cp.t : null;
            if (!mundo && c.t.parent != padreT) c.t.SetParent(padreT, false);
            if (mundo) { c.t.SetPositionAndRotation(p, r); c.t.localScale = s; }
            else { c.t.localPosition = p; c.t.localRotation = r; c.t.localScale = s; }
            c.t.gameObject.SetActive(activo);
            if (hayRender) LeerRender(l, c);
        }

        void LeerRender(BinaryReader l, Copia c)
        {
            bool activo = l.ReadBoolean();
            int n = l.ReadByte();
            var mats = new Material[n];
            if (c != null && c.instancias.Length != n) c.instancias = new Material[n];
            for (int i = 0; i < n; i++)
            {
                string nombre = l.ReadString();
                string shader = l.ReadString();
                Color color = new Color32(l.ReadByte(), l.ReadByte(), l.ReadByte(), l.ReadByte());
                if (c == null) continue;
                var baseMat = MaterialPorNombre(nombre, shader);
                if (baseMat == null) continue;
                if (baseMat.HasProperty(IdColorBase) && !Parecido(baseMat.GetColor(IdColorBase), color))
                {
                    var inst = c.instancias[i];
                    if (inst == null || inst.shader != baseMat.shader || !inst.name.StartsWith(baseMat.name))
                    {
                        inst = new Material(baseMat) { name = baseMat.name + " (red)" };
                        c.instancias[i] = inst;
                    }
                    inst.SetColor(IdColorBase, color);
                    mats[i] = inst;
                }
                else mats[i] = baseMat;
            }
            if (c == null || c.render == null) return;
            c.render.enabled = activo;
            c.render.sharedMaterials = mats;
        }

        static Material MaterialPorNombre(string nombre, string shader)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            if (materiales.TryGetValue(nombre, out var m) && m != null) return m;
            var s = Shader.Find(string.IsNullOrEmpty(shader) ? "Universal Render Pipeline/Lit" : shader)
                    ?? Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) return null;
            m = new Material(s) { name = nombre };
            materiales[nombre] = m;
            return m;
        }

        FotoMov LeerPose(BinaryReader l, float tiempo)
        {
            byte flags = l.ReadByte();
            var f = new FotoMov { tiempo = tiempo, mundo = (flags & MovMundo) != 0 };
            f.padre = f.mundo ? SinPadre : l.ReadUInt16();
            f.p = LeerV3(l);
            f.r = LeerQ(l);
            f.s = (flags & MovEscala) != 0 ? LeerV3(l) : Vector3.one;
            return f;
        }

        /// <summary>Un trozo de movimientos (sin garantía: si se pierde, llega el siguiente).</summary>
        public void RecibirMovimientos(byte[] datos, int longitud)
        {
            if (!Activo) return;
            using var l = new BinaryReader(new MemoryStream(datos, 0, longitud, false));
            try
            {
                int numero = l.ReadInt32();
                if (numero != Numero) return;
                float tiempo = l.ReadSingle();
                int n = l.ReadUInt16();
                for (int i = 0; i < n; i++)
                {
                    ushort id = l.ReadUInt16();
                    var foto = LeerPose(l, tiempo);
                    if (!copias.TryGetValue(id, out var c) || c.t == null) continue;
                    var fotos = c.fotos;
                    if (fotos.Count > 0 && tiempo <= fotos[fotos.Count - 1].tiempo) continue;
                    if (fotos.Count == 0 || tiempo - fotos[fotos.Count - 1].tiempo > 0.2f)
                    {
                        // Empieza a moverse tras estar quieto: arrancar desde donde está ahora.
                        fotos.Clear();
                        fotos.Add(EstadoActual(c, foto, tiempo - 1f / 30f));
                    }
                    fotos.Add(foto);
                    if (fotos.Count > 20) fotos.RemoveAt(0);
                    if (!c.moviendose) { c.moviendose = true; enMovimiento.Add(c); }
                }
            }
            catch (EndOfStreamException) { }
        }

        static FotoMov EstadoActual(Copia c, FotoMov como, float tiempo)
        {
            var t = c.t;
            return new FotoMov
            {
                tiempo = tiempo, mundo = como.mundo, padre = como.padre,
                p = como.mundo ? t.position : t.localPosition,
                r = como.mundo ? t.rotation : t.localRotation,
                s = como.mundo ? t.lossyScale : t.localScale,
            };
        }

        /// <summary>Cada fotograma, en el invitado: mueve lo que se está moviendo (un poco en el pasado, interpolando).</summary>
        public void Actualizar(double tiempoDibujo)
        {
            if (!Activo) return;
            float t = (float)tiempoDibujo;
            for (int i = enMovimiento.Count - 1; i >= 0; i--)
            {
                var c = enMovimiento[i];
                var fotos = c.fotos;
                if (c.t == null || fotos.Count == 0) { c.moviendose = false; enMovimiento.RemoveAt(i); continue; }
                while (fotos.Count >= 2 && fotos[1].tiempo <= t) fotos.RemoveAt(0);

                var a = fotos[0];
                if (fotos.Count == 1)
                {
                    if (t >= a.tiempo)
                    {
                        Aplicar(c, a);
                        c.moviendose = false;
                        enMovimiento.RemoveAt(i);
                    }
                    continue;
                }
                var b = fotos[1];
                if (t < a.tiempo) continue; // aún no toca
                float k = Mathf.Clamp01((t - a.tiempo) / Mathf.Max(0.0001f, b.tiempo - a.tiempo));
                if (a.mundo != b.mundo || a.padre != b.padre)
                {
                    Aplicar(c, k < 0.5f ? a : b);
                    continue;
                }
                Aplicar(c, new FotoMov
                {
                    mundo = b.mundo, padre = b.padre,
                    p = Vector3.Lerp(a.p, b.p, k),
                    r = Quaternion.Slerp(a.r, b.r, k),
                    s = Vector3.Lerp(a.s, b.s, k),
                });
            }
        }

        void Aplicar(Copia c, FotoMov f)
        {
            var t = c.t;
            if (t == null) return;
            if (f.mundo)
            {
                t.SetPositionAndRotation(f.p, f.r);
                var padre = t.parent;
                t.localScale = padre == null ? f.s : Dividir(f.s, padre.lossyScale);
                return;
            }
            if (f.padre == SinPadre)
            {
                if (t.parent != null && c.generado) t.SetParent(null, false);
            }
            else if (copias.TryGetValue(f.padre, out var cp) && cp.t != null && t.parent != cp.t)
            {
                t.SetParent(cp.t, false);
            }
            t.localPosition = f.p;
            t.localRotation = f.r;
            t.localScale = f.s;
        }

        static Vector3 Dividir(Vector3 a, Vector3 b) =>
            new Vector3(Mathf.Abs(b.x) > 1e-5f ? a.x / b.x : a.x, Mathf.Abs(b.y) > 1e-5f ? a.y / b.y : a.y, Mathf.Abs(b.z) > 1e-5f ? a.z / b.z : a.z);

        /// <summary>El invitado sale de la partida online.</summary>
        public void PararInvitado()
        {
            Activo = false;
            numeroPendiente = -1;
            LimpiarInvitado();
        }
    }
}
