using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Objeto que hay que llevar de la mudanza sin romperlo (tele, jarrón, sofá...).
    /// Cada golpe fuerte le quita integridad; se ve porque se oscurece, tiembla y le salen
    /// grietas. Al llegar a 0 revienta en pedazos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ObjetoFragil : MonoBehaviour
    {
        [Header("Identidad")]
        [Tooltip("Cómo se nombra en los mensajes: \"la tele\", \"el jarrón\"...")]
        public string nombreObjeto = "la tele";
        [Tooltip("Puntos si llega intacto. Se multiplican por la integridad que le quede.")]
        public int puntosBase = 100;

        [Header("Fragilidad")]
        [Tooltip("Multiplica todo el daño. 1 = normal, 2 = el doble de frágil.")]
        public float fragilidad = 1f;
        [Tooltip("Cambio de velocidad por un golpe (m/s) por debajo del cual no se hace daño.")]
        public float umbralGolpe = 3f;
        [Tooltip("Daño por cada m/s por encima del umbral.")]
        public float danoPorVelocidad = 12f;
        [Tooltip("Daño máximo de un solo golpe, para que nada lo rompa de un toque.")]
        public float danoMaximoPorGolpe = 45f;
        [Tooltip("Los empujones de los bichos hacen menos daño que caídas y trampas.")]
        [Range(0f, 1f)] public float factorGolpeBicho = 0.6f;
        [Tooltip("Tiempo sin poder recibir daño justo al aparecer (s).")]
        public float invulnerableAlAparecer = 2f;

        [Header("Física")]
        public float rozamiento = 0.35f;

        [Header("Peso")]
        [Tooltip("Bichos que hacen falta para llevarlo a buen paso. Con menos, se arrastra despacio.")]
        [Range(1, 2)] public int portadoresNecesarios = 1;
        [Tooltip("Velocidad máxima (m/s) mientras lo llevan menos bichos de los necesarios.")]
        public float velocidadPocosPortadores = 1.2f;

        [Header("Aspecto")]
        [Tooltip("Parte visual que tiembla con los golpes. Si está vacío, se usa este objeto.")]
        public Transform modelo;
        [Tooltip("Renderers que se oscurecen con el daño.")]
        public Renderer[] renderersDano;
        [Tooltip("Rombo flotante que muestra de qué equipo es.")]
        public Renderer marcador;
        public float alturaMarcador = 1.8f;
        public Color colorDanado = new Color(0.35f, 0.08f, 0.05f);

        public float Integridad { get; private set; } = 100f;
        public int Equipo { get; private set; } = -1;
        public bool Roto { get; private set; }
        public bool Entregado { get; private set; }
        public Rigidbody Cuerpo => rb;
        /// <summary>Los que hacen falta en esta partida (la ronda lo baja si el equipo tiene menos jugadores).</summary>
        public int NecesariosAhora { get; set; }
        /// <summary>Bichos que lo tienen agarrado ahora mismo.</summary>
        public int Portadores { get; private set; }
        /// <summary>Lo llevan, pero menos bichos de los que hacen falta: va despacio.</summary>
        public bool Pesado => Portadores > 0 && Portadores < NecesariosAhora;

        public event System.Action<ObjetoFragil, float> AlRecibirDano;
        public event System.Action<ObjetoFragil, string> AlRomperse;

        Rigidbody rb;
        Collider colisionador;
        float invulnerableHasta;
        float ultimoGolpe = -10f;
        float inicioTemblor = -10f;
        float fuerzaTemblor;
        int grietasPuestas;
        Vector3 escalaModelo = Vector3.one;
        Vector3 posicionModelo;
        Material materialGrieta;
        TextMesh cartelPeso;
        readonly List<Material> materiales = new List<Material>();
        readonly List<Color> coloresBase = new List<Color>();

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colisionador = GetComponent<Collider>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (colisionador != null && colisionador.sharedMaterial == null)
            {
                colisionador.sharedMaterial = new PhysicsMaterial("ObjetoFragil")
                {
                    dynamicFriction = rozamiento,
                    staticFriction = rozamiento,
                    bounciness = 0.05f,
                    frictionCombine = PhysicsMaterialCombine.Average,
                };
            }

            if (modelo == null) modelo = transform;
            escalaModelo = modelo.localScale;
            posicionModelo = modelo.localPosition;

            if (renderersDano != null)
            {
                foreach (var r in renderersDano)
                {
                    if (r == null) continue;
                    var m = r.material; // instancia propia
                    materiales.Add(m);
                    coloresBase.Add(LeerColor(m));
                }
            }

            invulnerableHasta = Time.time + invulnerableAlAparecer;
            NecesariosAhora = portadoresNecesarios;
            CrearCartelPeso();
        }

        /// <summary>"¡ENTRE DOS!" flotando encima de los objetos que pesan.</summary>
        void CrearCartelPeso()
        {
            if (portadoresNecesarios < 2) return;
            var go = new GameObject("CartelPeso");
            go.transform.SetParent(transform, false);
            cartelPeso = go.AddComponent<TextMesh>();
            var fuente = Resources.Load<Font>("Fuentes/LilitaOne");
            if (fuente != null)
            {
                cartelPeso.font = fuente;
                go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
            cartelPeso.text = "¡ENTRE DOS!";
            cartelPeso.fontSize = 64;
            cartelPeso.characterSize = 0.05f;
            cartelPeso.anchor = TextAnchor.MiddleCenter;
            cartelPeso.alignment = TextAlignment.Center;
            cartelPeso.fontStyle = FontStyle.Bold;
        }

        void FixedUpdate()
        {
            int n = 0;
            foreach (var b in ControladorBicho.Todos)
                if (b != null && b.CuerpoAgarrado == rb) n++;
            Portadores = n;
            if (!Pesado || Roto || Entregado) return;

            // Pesa demasiado para uno solo: se arrastra despacio (y quien tira de él, también).
            Vector3 v = rb.linearVelocity;
            var plano = new Vector3(v.x, 0f, v.z);
            if (plano.magnitude > velocidadPocosPortadores)
            {
                plano = plano.normalized * velocidadPocosPortadores;
                rb.linearVelocity = new Vector3(plano.x, v.y, plano.z);
            }
        }

        /// <summary>Lo asigna la ronda al crearlo.</summary>
        public void AsignarEquipo(int equipo, Color color)
        {
            Equipo = equipo;
            if (marcador != null)
            {
                var m = marcador.material;
                EscribirColor(m, color);
            }
        }

        void Update()
        {
            // Temblor al recibir un golpe: un "boing" de escala que se apaga rápido.
            float t = Time.time - inicioTemblor;
            if (t < 0.35f)
            {
                float onda = Mathf.Sin(t * 45f) * Mathf.Exp(-t * 10f) * fuerzaTemblor;
                modelo.localScale = new Vector3(escalaModelo.x * (1f + onda), escalaModelo.y * (1f - onda), escalaModelo.z * (1f + onda));
            }
            else
            {
                modelo.localScale = escalaModelo;
            }

            // Cuando está casi roto, tiembla solo, avisando.
            if (!Roto && Integridad < 25f && modelo != transform)
                modelo.localPosition = posicionModelo + Random.insideUnitSphere * 0.015f;

            if (marcador != null)
            {
                // Rombo que flota siempre encima, girando, aunque el objeto vuelque.
                marcador.transform.position = transform.position + Vector3.up * (alturaMarcador + Mathf.Sin(Time.time * 3f) * 0.08f);
                marcador.transform.rotation = Quaternion.Euler(45f, Time.time * 90f, 45f);
            }

            if (cartelPeso != null) ActualizarCartelPeso();

            if (transform.position.y < -30f) Romper("se ha perdido en el vacío");
        }

        void ActualizarCartelPeso()
        {
            bool ver = !Roto && !Entregado && NecesariosAhora >= 2 && Portadores < NecesariosAhora;
            if (cartelPeso.gameObject.activeSelf != ver) cartelPeso.gameObject.SetActive(ver);
            if (!ver) return;
            var t = cartelPeso.transform;
            t.position = transform.position + Vector3.up * (alturaMarcador + 0.75f);
            var camara = Camera.main;
            if (camara != null) t.rotation = camara.transform.rotation;
            // Si uno lo arrastra solo, late en naranja; si nadie lo lleva, quieto en blanco.
            float latido = Pesado ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.25f : 1f;
            t.localScale = Vector3.one * latido;
            cartelPeso.color = Pesado ? new Color(1f, 0.6f, 0.15f) : Color.white;
        }

        void OnCollisionEnter(Collision choque)
        {
            if (Roto || Entregado) return;
            if (Time.time < invulnerableHasta) return;
            if (Time.time - ultimoGolpe < 0.15f) return;

            float cambioVelocidad = choque.impulse.magnitude / rb.mass;
            if (choque.rigidbody != null && choque.rigidbody.GetComponent<ControladorBicho>() != null)
                cambioVelocidad *= factorGolpeBicho;

            if (cambioVelocidad < umbralGolpe) return;

            ultimoGolpe = Time.time;
            float dano = Mathf.Min((cambioVelocidad - umbralGolpe) * danoPorVelocidad, danoMaximoPorGolpe) * fragilidad;
            var acolchado = choque.collider != null ? choque.collider.GetComponentInParent<Acolchado>() : null;
            if (acolchado != null) dano *= acolchado.factorDano;
            RecibirDano(dano);
        }

        public void RecibirDano(float dano)
        {
            if (Roto || Entregado || dano <= 0f) return;

            Integridad = Mathf.Max(0f, Integridad - dano);
            inicioTemblor = Time.time;
            fuerzaTemblor = Mathf.Clamp(dano / 40f, 0.08f, 0.3f);
            ActualizarAspecto();
            AlRecibirDano?.Invoke(this, dano);

            if (Integridad <= 0f) Romper("se ha hecho añicos");
            else Sonidos.Reproducir(Sonido.Grieta, transform.position, Mathf.Clamp(dano / 25f, 0.35f, 1f));
        }

        void ActualizarAspecto()
        {
            float dano01 = 1f - Integridad / 100f;
            for (int i = 0; i < materiales.Count; i++)
                EscribirColor(materiales[i], Color.Lerp(coloresBase[i], colorDanado, dano01 * 0.75f));

            // Grietas a partir del 75%, 50% y 25%.
            int grietasQueTocan = Integridad < 25f ? 9 : Integridad < 50f ? 6 : Integridad < 75f ? 3 : 0;
            while (grietasPuestas < grietasQueTocan)
            {
                PonerGrieta();
                grietasPuestas++;
            }
        }

        void PonerGrieta()
        {
            if (colisionador == null) return;

            if (materialGrieta == null)
            {
                materialGrieta = materiales.Count > 0 ? new Material(materiales[0]) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                EscribirColor(materialGrieta, new Color(0.05f, 0.04f, 0.04f));
            }

            Bounds limites = colisionador.bounds;
            Vector3 desde = limites.center + Random.onUnitSphere * limites.extents.magnitude * 2f;
            Vector3 punto = colisionador.ClosestPoint(desde);
            Vector3 normal = (desde - punto).normalized;

            var grieta = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grieta.name = "Grieta";
            Destroy(grieta.GetComponent<Collider>());
            grieta.GetComponent<Renderer>().sharedMaterial = materialGrieta;
            grieta.transform.SetParent(modelo, true);
            grieta.transform.position = punto + normal * 0.005f;
            grieta.transform.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
            grieta.transform.localScale = Vector3.one; // se reescala abajo en espacio mundo
            Vector3 escalaMundo = new Vector3(0.035f, Random.Range(0.18f, 0.4f), 0.012f);
            Vector3 escalaPadre = modelo.lossyScale;
            grieta.transform.localScale = new Vector3(escalaMundo.x / escalaPadre.x, escalaMundo.y / escalaPadre.y, escalaMundo.z / escalaPadre.z);
        }

        /// <summary>Revienta en pedazos y desaparece.</summary>
        public void Romper(string motivo)
        {
            if (Roto || Entregado) return;
            Roto = true;
            Integridad = 0f;

            LiberarDeLosBichos();
            SoltarPedazos();

            // El momentazo: estallido, temblor, cámara lenta y el público que se lamenta.
            Vector3 centro = colisionador != null ? colisionador.bounds.center : transform.position;
            Sonidos.Reproducir(Sonido.Rotura, centro);
            Sonidos.Reproducir(Sonido.Ooh, centro);
            Efectos.Impacto(centro, coloresBase.Count > 0 ? coloresBase[0] : Color.white, 1f);
            Efectos.Polvo(centro, 0.8f);
            Efectos.Sacudir(0.4f);
            if (transform.position.y > -5f) Efectos.CamaraLenta(0.3f, 0.5f);
            Estadisticas.Evento("objeto roto", null, $"{nombreObjeto} {motivo}");

            AlRomperse?.Invoke(this, motivo);
            Destroy(gameObject);
        }

        /// <summary>Lo llama la zona de entrega: se encoge y desaparece.</summary>
        public void Entregar()
        {
            if (Roto || Entregado) return;
            Entregado = true;
            LiberarDeLosBichos();
            Sonidos.Reproducir(Sonido.Entrega, transform.position);
            Estadisticas.Evento("objeto entregado", null, $"{nombreObjeto} con {Integridad:0}% de vida");
            Sonidos.Reproducir(Sonido.Aplausos, transform.position, 0.7f);
            Efectos.Impacto(transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.3f), 0.6f);
            rb.isKinematic = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            StartCoroutine(Encoger());
        }

        System.Collections.IEnumerator Encoger()
        {
            Vector3 inicial = transform.localScale;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float k = 1f - t / 0.4f;
                transform.localScale = inicial * (1f + Mathf.Sin(t * 20f) * 0.15f * k) * Mathf.Max(0.01f, k);
                transform.position += Vector3.up * Time.deltaTime * 2f;
                yield return null;
            }
            Destroy(gameObject);
        }

        void LiberarDeLosBichos()
        {
            var bichos = ControladorBicho.Todos;
            for (int i = bichos.Count - 1; i >= 0; i--)
                bichos[i].SoltarSi(rb);
        }

        void SoltarPedazos()
        {
            if (colisionador == null) return;
            Bounds limites = colisionador.bounds;
            float tamano = Mathf.Max(0.12f, limites.extents.magnitude * 0.35f);
            Material material = materiales.Count > 0 ? materiales[0] : null;

            for (int i = 0; i < 12; i++)
            {
                var pedazo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedazo.name = "Pedazo";
                pedazo.transform.position = limites.center + Vector3.Scale(Random.insideUnitSphere, limites.extents);
                pedazo.transform.rotation = Random.rotation;
                pedazo.transform.localScale = Vector3.one * tamano * Random.Range(0.4f, 1f);
                if (material != null)
                    pedazo.GetComponent<Renderer>().sharedMaterial = i % 3 == 0 && materialGrieta != null ? materialGrieta : material;

                var rbPedazo = pedazo.AddComponent<Rigidbody>();
                rbPedazo.mass = 0.2f;
                rbPedazo.linearVelocity = rb.linearVelocity * 0.5f + Random.insideUnitSphere * 4f + Vector3.up * 3f;
                rbPedazo.angularVelocity = Random.insideUnitSphere * 10f;
                Destroy(pedazo, Random.Range(2.5f, 4f));
            }
        }

        static Color LeerColor(Material m) => m.HasProperty(IdBaseColor) ? m.GetColor(IdBaseColor) : m.color;

        static void EscribirColor(Material m, Color c)
        {
            if (m.HasProperty(IdBaseColor)) m.SetColor(IdBaseColor, c);
            else m.color = c;
        }
    }
}
