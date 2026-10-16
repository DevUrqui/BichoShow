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

        Vector3 posicionInicial;
        Quaternion rotacionInicial;
        bool guardado;

        bool Caido => bloque != null && bloque.position.y < -1f;

        void Awake()
        {
            Guardar();

            // El pasillo deja solo 10 cm por lado: si el bloque gira un poco al empujarlo
            // desequilibrado, las esquinas se clavan en las paredes y se queda encajado.
            // Que solo pueda avanzar recto (y caer al foso).
            if (bloque != null)
            {
                bloque.constraints = RigidbodyConstraints.FreezeRotation |
                                     (Mathf.Abs(adelante.x) < 0.5f ? RigidbodyConstraints.FreezePositionX : RigidbodyConstraints.FreezePositionZ);
            }

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
