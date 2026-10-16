using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Paso 1: un bloque enorme tapa un pasillo estrecho. Pesa demasiado para uno solo; entre
    /// los dos se empuja hasta un foso. En cuanto su centro pasa el borde, vuelca hacia delante
    /// y cae; al asentarse se queda fijo y centrado, a ras del suelo, como puente.
    ///
    /// Empuje asistido: con toda la pareja pegada detrás y empujando hacia delante, avanza a
    /// ritmo fijo (no depende de la física fina del contacto). Naranja = falta uno; verde = ¡vamos!
    /// </summary>
    public class PasoBloque : PasoCooperativo
    {
        enum Fase { Empujable, Cayendo, Asentado }

        public Rigidbody bloque;
        [Tooltip("Hacia dónde hay que empujarlo.")]
        public Vector3 adelante = Vector3.forward;
        [Tooltip("A dónde ir cuando el bloque ya está en el foso.")]
        public Vector3 salida;
        [Tooltip("Centro del foso (en el suelo). El bloque se centra aquí al asentarse.")]
        public Vector3 centroFoso;
        [Tooltip("Masa con pareja: uno solo no puede moverlo, dos sí.")]
        public float masaEnPareja = 45f;
        [Tooltip("Masa si el equipo es de un solo jugador.")]
        public float masaEnSolitario = 22f;
        [Tooltip("Rozamiento del bloque con el suelo. Con 45 kg y 0.45, uno solo no puede y dos sí, con margen.")]
        public float rozamiento = 0.45f;

        [Header("Empuje asistido")]
        [Tooltip("Con toda la pareja pegada detrás, el bloque avanza a esta velocidad (m/s).")]
        public float velocidadEmpujado = 1.6f;
        [Tooltip("Hasta qué distancia de la cara de atrás cuenta un bicho como 'pegado' (m, desde su centro).")]
        public float profundidadEmpuje = 0.9f;
        [Tooltip("Cuánto tiene que apuntar el stick hacia delante para contar como que empuja (0-1).")]
        [Range(0f, 1f)] public float intencionMinima = 0.35f;
        public Color colorFaltaUno = new Color(1f, 0.6f, 0.2f);
        public Color colorEmpujando = new Color(0.45f, 0.95f, 0.5f);

        /// <summary>Cuántos de la pareja están pegados detrás del bloque y empujando ahora mismo.</summary>
        public int Empujando { get; private set; }

        Fase fase = Fase.Empujable;
        float inicioCaida;
        float quietoDesde;
        Vector3 posicionInicial;
        Quaternion rotacionInicial;
        bool guardado;
        Material material;
        Color colorBase;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Collider[] Encontrados = new Collider[16];
        static readonly HashSet<ControladorBicho> Vistos = new HashSet<ControladorBicho>();
        static readonly RaycastHit[] Impactos = new RaycastHit[8];

        /// <summary>Ya está abajo, en el foso (aunque todavía se esté asentando).</summary>
        bool Caido => bloque != null && (fase == Fase.Asentado || bloque.position.y < posicionInicial.y - 2f);

        bool HaciaZ => Mathf.Abs(adelante.x) < 0.5f;
        Vector3 AdelantePlano => Plano(adelante).normalized;

        RigidbodyConstraints RestriccionesEmpujando =>
            // El pasillo deja poca holgura: si gira, las esquinas se clavan en las paredes.
            RigidbodyConstraints.FreezeRotation | (HaciaZ ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionZ);

        RigidbodyConstraints RestriccionesVolcando =>
            // Puede girar solo hacia delante (volcar al foso), sin salirse del pasillo.
            HaciaZ
                ? RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ
                : RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX;

        void Awake()
        {
            Guardar();
            if (bloque == null) return;
            bloque.constraints = RestriccionesEmpujando;

            var col = bloque.GetComponent<Collider>();
            if (col != null)
            {
                col.sharedMaterial = new PhysicsMaterial("Bloque")
                {
                    dynamicFriction = rozamiento,
                    staticFriction = rozamiento,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                };
            }

            var rend = bloque.GetComponent<Renderer>();
            if (rend != null)
            {
                material = rend.material;
                colorBase = material.HasProperty(IdBaseColor) ? material.GetColor(IdBaseColor) : material.color;
            }
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
            fase = Fase.Empujable;
            bloque.isKinematic = false;
            bloque.constraints = RestriccionesEmpujando;
            bloque.linearVelocity = Vector3.zero;
            bloque.angularVelocity = Vector3.zero;
            bloque.position = posicionInicial;
            bloque.rotation = rotacionInicial;
            bloque.transform.SetPositionAndRotation(posicionInicial, rotacionInicial);
        }

        /// <summary>Cuántos hacen falta empujando: los dos de la pareja (o uno si el equipo es de uno).</summary>
        int Necesarios()
        {
            var ronda = RondaFinal.Instancia;
            return ronda != null ? Mathf.Clamp(ronda.MiembrosDe(equipo), 1, 2) : 2;
        }

        // ---------------------------------------------------------------------
        // Física
        // ---------------------------------------------------------------------

        void FixedUpdate()
        {
            if (bloque == null) return;

            switch (fase)
            {
                case Fase.Empujable:
                    // El centro ya ha pasado el borde del foso: ¡que vuelque!
                    if (!HaySueloBajoElCentro())
                    {
                        Volcar();
                        return;
                    }

                    Empujando = ContarEmpujando();
                    if (Empujando >= Necesarios())
                    {
                        float actual = Vector3.Dot(bloque.linearVelocity, AdelantePlano);
                        if (actual < velocidadEmpujado)
                            bloque.AddForce(AdelantePlano * (velocidadEmpujado - actual) / Time.fixedDeltaTime * 0.3f, ForceMode.Acceleration);
                    }
                    break;

                case Fase.Cayendo:
                    Empujando = 0;
                    bool quieto = bloque.linearVelocity.magnitude < 0.15f && bloque.angularVelocity.magnitude < 0.25f;
                    if (!quieto) quietoDesde = Time.time;
                    // Abajo y quieto, o ha pasado demasiado tiempo (atravesado): asentarlo.
                    if ((Caido && Time.time - quietoDesde > 0.3f) || Time.time - inicioCaida > 3f) Asentar();
                    break;
            }
        }

        void Volcar()
        {
            fase = Fase.Cayendo;
            inicioCaida = Time.time;
            quietoDesde = Time.time;
            Empujando = 0;
            bloque.constraints = RestriccionesVolcando;
            // Un empujoncito de giro hacia delante para que vuelque decidido.
            Vector3 eje = Vector3.Cross(Vector3.up, AdelantePlano);
            bloque.AddTorque(eje * 1.2f, ForceMode.VelocityChange);
            bloque.AddForce(AdelantePlano * 0.6f, ForceMode.VelocityChange);
        }

        /// <summary>Lo deja fijo, recto y centrado en el foso, a ras del suelo: un puente estable.</summary>
        void Asentar()
        {
            fase = Fase.Asentado;
            Vector3 e = bloque.rotation.eulerAngles;
            if (HaciaZ) e.x = Mathf.Round(e.x / 90f) * 90f;
            else e.z = Mathf.Round(e.z / 90f) * 90f;
            Quaternion rotacion = Quaternion.Euler(e);

            Vector3 p = bloque.position;
            float alturaAbajo = posicionInicial.y - bloque.transform.lossyScale.y;
            Vector3 posicion = centroFoso != Vector3.zero
                ? new Vector3(HaciaZ ? p.x : centroFoso.x, alturaAbajo, HaciaZ ? centroFoso.z : p.z)
                : new Vector3(p.x, alturaAbajo, p.z);

            bloque.isKinematic = true;
            bloque.position = posicion;
            bloque.rotation = rotacion;
            bloque.transform.SetPositionAndRotation(posicion, rotacion);
        }

        bool HaySueloBajoElCentro()
        {
            Vector3 origen = bloque.position;
            origen.y = bloque.position.y - bloque.transform.lossyScale.y * 0.5f + 0.1f;

            int n = Physics.RaycastNonAlloc(origen, Vector3.down, Impactos, 0.35f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var rb = Impactos[i].collider.attachedRigidbody;
                if (rb == bloque) continue;
                if (rb != null && rb.GetComponent<ControladorBicho>() != null) continue;
                return true;
            }
            return false;
        }

        /// <summary>Los de la pareja pegados a la cara de atrás y empujando de verdad hacia delante.</summary>
        int ContarEmpujando()
        {
            Vector3 escala = bloque.transform.lossyScale;
            float medioLargo = (HaciaZ ? escala.z : escala.x) * 0.5f;
            Vector3 centroZona = bloque.position - AdelantePlano * (medioLargo + profundidadEmpuje * 0.5f);
            centroZona.y = bloque.position.y - escala.y * 0.5f + 1f;
            float medioAncho = (HaciaZ ? escala.x : escala.z) * 0.5f + 0.2f;
            var mitad = HaciaZ
                ? new Vector3(medioAncho, 1f, profundidadEmpuje * 0.5f)
                : new Vector3(profundidadEmpuje * 0.5f, 1f, medioAncho);

            int n = Physics.OverlapBoxNonAlloc(centroZona, mitad, Encontrados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Vistos.Clear();
            for (int i = 0; i < n; i++)
            {
                var rb = Encontrados[i].attachedRigidbody;
                var b = rb != null ? rb.GetComponent<ControladorBicho>() : null;
                if (b == null || b.Retirado || RondaBase.EquipoDe(b) != equipo) continue;
                if (Vector3.Dot(b.Intencion, AdelantePlano) < intencionMinima) continue; // detrás pero sin empujar
                Vistos.Add(b);
            }
            return Vistos.Count;
        }

        void Update()
        {
            if (material == null) return;
            Color c = colorBase;
            if (fase == Fase.Empujable && Empujando > 0)
                c = Color.Lerp(colorBase, Empujando >= Necesarios() ? colorEmpujando : colorFaltaUno, 0.7f);
            if (material.HasProperty(IdBaseColor)) material.SetColor(IdBaseColor, c);
            else material.color = c;
        }

        // ---------------------------------------------------------------------
        // Bots
        // ---------------------------------------------------------------------

        public override bool HaciendoFuerza(ControladorBicho bot)
        {
            if (bloque == null || fase != Fase.Empujable) return false;
            Vector3 relativo = Plano(bot.transform.position - bloque.position);
            // Pegado a la cara de atrás del bloque.
            return Vector3.Dot(relativo, AdelantePlano) < -1.1f && relativo.magnitude < 2.6f;
        }

        public override bool EsperandoCompanero(ControladorBicho bot) => HaciendoFuerza(bot) && Empujando < Necesarios();

        public override string Estado(ControladorBicho bot)
        {
            if (bloque == null) return "sin bloque";
            if (fase == Fase.Asentado) return "bloque en el foso: ¡adelante!";
            if (fase == Fase.Cayendo) return "¡el bloque cae al foso!";
            return HaciendoFuerza(bot) ? $"empujando ({Empujando}/{Necesarios()})" : "yendo detrás del bloque";
        }

        public override Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad)
        {
            intensidad = 1f;
            if (bloque == null || fase == Fase.Asentado) return salida;

            Vector3 lateral = Vector3.Cross(Vector3.up, AdelantePlano) * (rol == 0 ? -0.6f : 0.6f);
            Vector3 centro = Plano(bloque.position);
            Vector3 detras = centro - AdelantePlano * 2f + lateral;

            // Cayendo: esperar detrás, sin meterse en el foso.
            if (fase == Fase.Cayendo)
            {
                Vector3 espera = Plano(posicionInicial) + AdelantePlano * 1.5f + lateral;
                intensidad = Plano(bot.transform.position - espera).magnitude < 0.5f ? 0f : 1f;
                return espera;
            }

            // Ya detrás y pegado: empujar hacia delante con todo.
            Vector3 yo = Plano(bot.transform.position);
            bool estaDetras = Vector3.Dot(yo - centro, AdelantePlano) < -1.1f;
            if (estaDetras && (yo - detras).magnitude < 1f)
                return centro + AdelantePlano * 4f + lateral;

            return detras;
        }
    }
}
