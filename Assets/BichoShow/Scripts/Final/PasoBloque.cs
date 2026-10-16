using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso 1: un bloque enorme tapa un pasillo estrecho. Pesa demasiado para uno solo; entre
    /// los dos se empuja hasta que cae a un foso y queda como puente.
    /// </summary>
    public class PasoBloque : PasoCooperativo
    {
        public Rigidbody bloque;
        [Tooltip("Hacia dónde hay que empujarlo.")]
        public Vector3 adelante = Vector3.forward;
        [Tooltip("A dónde ir cuando el bloque ya está en el foso.")]
        public Vector3 salida;
        [Tooltip("Masa con pareja: uno solo no puede moverlo, dos sí.")]
        public float masaEnPareja = 45f;
        [Tooltip("Masa si el equipo es de un solo jugador.")]
        public float masaEnSolitario = 22f;
        [Tooltip("Rozamiento del bloque con el suelo. Con 45 kg y 0.45, uno solo no puede y dos sí, con margen.")]
        public float rozamiento = 0.45f;

        [Header("Empuje asistido")]
        [Tooltip("Con toda la pareja pegada detrás, el bloque avanza a esta velocidad (m/s) pase lo que pase con la física.")]
        public float velocidadEmpujado = 1.6f;
        [Tooltip("Hasta qué distancia de la cara de atrás cuenta un bicho como 'pegado' (m, desde su centro).")]
        public float profundidadEmpuje = 0.9f;
        [Tooltip("Cuánto tiene que apuntar el stick hacia delante para contar como que empuja (0-1).")]
        [Range(0f, 1f)] public float intencionMinima = 0.35f;
        public Color colorFaltaUno = new Color(1f, 0.6f, 0.2f);
        public Color colorEmpujando = new Color(0.45f, 0.95f, 0.5f);

        /// <summary>Cuántos de la pareja están pegados detrás del bloque ahora mismo.</summary>
        public int Empujando { get; private set; }

        Vector3 posicionInicial;
        Quaternion rotacionInicial;
        bool guardado;
        Material material;
        Color colorBase;
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        bool Caido => bloque != null && bloque.position.y < -1f;

        void Awake()
        {
            Guardar();

            // El pasillo deja solo 10 cm por lado: si el bloque gira un poco al empujarlo
            // desequilibrado, las esquinas se clavan en las paredes y se queda encajado.
            // Que solo pueda avanzar recto (y caer al foso).
            if (bloque != null) bloque.constraints = RestriccionesNormales;

            var col = bloque != null ? bloque.GetComponent<Collider>() : null;
            if (col != null)
            {
                col.sharedMaterial = new PhysicsMaterial("Bloque")
                {
                    dynamicFriction = rozamiento,
                    staticFriction = rozamiento,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                };
            }

            var rend = bloque != null ? bloque.GetComponent<Renderer>() : null;
            if (rend != null)
            {
                material = rend.material;
                colorBase = material.HasProperty(IdBaseColor) ? material.GetColor(IdBaseColor) : material.color;
            }
        }

        /// <summary>Cuántos hacen falta empujando: los dos de la pareja (o uno si el equipo es de uno).</summary>
        int Necesarios()
        {
            var ronda = RondaFinal.Instancia;
            return ronda != null ? Mathf.Clamp(ronda.MiembrosDe(equipo), 1, 2) : 2;
        }

        void FixedUpdate()
        {
            if (bloque == null) return;
            if (Caido || soltando) { Empujando = 0; return; }

            // Toda la base sobre el foso: dejar de avanzar y caer recto (aunque sigan empujando).
            if (SinSueloDetras())
            {
                Soltar();
                return;
            }

            Empujando = ContarEmpujando();

            // Con todos los necesarios detrás, avanza a ritmo fijo (no depende de cuánto apriete la física).
            if (Empujando >= Necesarios())
            {
                Vector3 adelantePlano = Plano(adelante).normalized;
                float actual = Vector3.Dot(bloque.linearVelocity, adelantePlano);
                if (actual < velocidadEmpujado)
                    bloque.AddForce(adelantePlano * (velocidadEmpujado - actual) / Time.fixedDeltaTime * 0.3f, ForceMode.Acceleration);
            }
        }

        // ---------------------------------------------------------------------
        // Quién empuja
        // ---------------------------------------------------------------------

        static readonly Collider[] Encontrados = new Collider[16];
        static readonly System.Collections.Generic.HashSet<ControladorBicho> Vistos = new System.Collections.Generic.HashSet<ControladorBicho>();

        /// <summary>Los de la pareja pegados a la cara de atrás y empujando de verdad hacia delante.</summary>
        int ContarEmpujando()
        {
            Vector3 adelantePlano = Plano(adelante).normalized;
            Vector3 escala = bloque.transform.lossyScale;
            float medioLargo = escala.z * 0.5f;
            Vector3 centroZona = bloque.position - adelantePlano * (medioLargo + profundidadEmpuje * 0.5f);
            centroZona.y = bloque.position.y - escala.y * 0.5f + 1f;
            var mitad = new Vector3(escala.x * 0.5f + 0.2f, 1f, profundidadEmpuje * 0.5f);

            int n = Physics.OverlapBoxNonAlloc(centroZona, mitad, Encontrados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Vistos.Clear();
            for (int i = 0; i < n; i++)
            {
                var rb = Encontrados[i].attachedRigidbody;
                var b = rb != null ? rb.GetComponent<ControladorBicho>() : null;
                if (b == null || b.Retirado || RondaBase.EquipoDe(b) != equipo) continue;
                if (Vector3.Dot(b.Intencion, adelantePlano) < intencionMinima) continue; // detrás pero sin empujar
                Vistos.Add(b);
            }
            return Vistos.Count;
        }

        // ---------------------------------------------------------------------
        // Caída al foso
        // ---------------------------------------------------------------------

        bool soltando;
        static readonly RaycastHit[] Impactos = new RaycastHit[8];

        /// <summary>¿Ya no hay suelo bajo la parte de atrás? Entonces toda la base está sobre el foso.</summary>
        bool SinSueloDetras()
        {
            Vector3 adelantePlano = Plano(adelante).normalized;
            Vector3 escala = bloque.transform.lossyScale;
            Vector3 origen = bloque.position - adelantePlano * (escala.z * 0.5f - 0.05f);
            origen.y = bloque.position.y - escala.y * 0.5f + 0.1f;

            int n = Physics.RaycastNonAlloc(origen, Vector3.down, Impactos, 0.35f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var rb = Impactos[i].collider.attachedRigidbody;
                if (rb == bloque) continue;
                if (rb != null && rb.GetComponent<ControladorBicho>() != null) continue;
                return false; // hay suelo
            }
            return true;
        }

        void Soltar()
        {
            soltando = true;
            Empujando = 0;
            // Un empujoncito para que la cara de atrás no quede rozando el borde del foso.
            bloque.position += Plano(adelante).normalized * 0.1f;
            // Que no avance más: cae recto y encaja en el foso.
            bloque.constraints |= Mathf.Abs(adelante.x) < 0.5f ? RigidbodyConstraints.FreezePositionZ : RigidbodyConstraints.FreezePositionX;
            var v = bloque.linearVelocity;
            bloque.linearVelocity = new Vector3(0f, Mathf.Min(v.y, 0f), 0f);
            bloque.AddForce(Vector3.down * 6f, ForceMode.VelocityChange);
        }

        RigidbodyConstraints RestriccionesNormales =>
            RigidbodyConstraints.FreezeRotation |
            (Mathf.Abs(adelante.x) < 0.5f ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionZ);

        void Update()
        {
            if (material == null) return;
            Color c = colorBase;
            if (!Caido && Empujando > 0)
                c = Color.Lerp(colorBase, Empujando >= Necesarios() ? colorEmpujando : colorFaltaUno, 0.7f);
            if (material.HasProperty(IdBaseColor)) material.SetColor(IdBaseColor, c);
            else material.color = c;
        }

        void Guardar()
        {
            if (guardado || bloque == null) return;
            posicionInicial = bloque.position;
            rotacionInicial = bloque.rotation;
            guardado = true;
        }

        public void AjustarMasa(int miembros)
        {
            if (bloque != null) bloque.mass = miembros <= 1 ? masaEnSolitario : masaEnPareja;
        }

        public override void Reiniciar()
        {
            Guardar();
            if (bloque == null) return;
            soltando = false;
            bloque.constraints = RestriccionesNormales;
            bloque.linearVelocity = Vector3.zero;
            bloque.angularVelocity = Vector3.zero;
            bloque.position = posicionInicial;
            bloque.rotation = rotacionInicial;
            bloque.transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
        }

        public override bool HaciendoFuerza(ControladorBicho bot)
        {
            if (bloque == null || Caido) return false;
            Vector3 adelantePlano = Plano(adelante).normalized;
            Vector3 relativo = Plano(bot.transform.position - bloque.position);
            // Pegado a la cara de atrás del bloque.
            return Vector3.Dot(relativo, adelantePlano) < -1.1f && relativo.magnitude < 2.6f;
        }

        public override bool EsperandoCompanero(ControladorBicho bot) => HaciendoFuerza(bot) && Empujando < Necesarios();

        public override string Estado(ControladorBicho bot)
        {
            if (bloque == null) return "sin bloque";
            if (Caido) return "bloque en el foso: ¡adelante!";
            return HaciendoFuerza(bot) ? $"empujando ({Empujando}/{Necesarios()})" : "yendo detrás del bloque";
        }

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            if (bloque == null || Caido) return salida;

            Vector3 adelantePlano = Plano(adelante).normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, adelantePlano) * (rol == 0 ? -0.6f : 0.6f);
            Vector3 centro = Plano(bloque.position);
            Vector3 detras = centro - adelantePlano * 2f + lateral;

            // Ya detrás y pegado: empujar hacia delante con todo.
            Vector3 yo = Plano(bot.transform.position);
            bool estaDetras = Vector3.Dot(yo - centro, adelantePlano) < -1.1f;
            if (estaDetras && (yo - detras).magnitude < 1f)
                return centro + adelantePlano * 4f + lateral;

            return detras;
        }
    }
}
