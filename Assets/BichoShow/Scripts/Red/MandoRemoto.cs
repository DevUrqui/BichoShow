using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// En un invitado de una partida online: el jugador de este ordenador. Su bicho se esconde
    /// (sin física ni dibujo: el de verdad lo mueve el anfitrión) y queda solo el mando, que
    /// RedShow manda al anfitrión. Al quitar este componente el bicho vuelve a ser normal.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class MandoRemoto : MonoBehaviour
    {
        public static readonly List<MandoRemoto> Todos = new List<MandoRemoto>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos() => Todos.Clear();

        PlayerInput entrada;
        ControladorBicho bicho;
        Vestuario vestuario;
        InputAction accionMover, accionSaltar, accionAgarrar, accionLanzarse;
        byte saltos, lanzamientos;

        /// <summary>Número del mando en este ordenador.</summary>
        public byte Local => (byte)(entrada != null ? entrada.playerIndex : 0);
        public int Gorro => vestuario != null ? vestuario.Gorro : 0;
        public int Complemento => vestuario != null ? vestuario.Complemento : 0;
        public byte Saltos => saltos;
        public byte Lanzamientos => lanzamientos;
        public bool AgarrarPulsado => !ControladorBicho.EntradaBloqueada && accionAgarrar != null && accionAgarrar.IsPressed();

        /// <summary>Hacia dónde empuja el stick, ya en el mundo (girado con la cámara de este ordenador).</summary>
        public Vector2 DireccionMundo
        {
            get
            {
                if (ControladorBicho.EntradaBloqueada || accionMover == null) return Vector2.zero;
                Vector2 stick = Vector2.ClampMagnitude(accionMover.ReadValue<Vector2>(), 1f);
                var cam = Camera.main;
                if (cam == null) return stick;
                Vector3 adelante = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
                Vector3 derecha = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;
                Vector3 d = adelante * stick.y + derecha * stick.x;
                return new Vector2(d.x, d.z);
            }
        }

        void Awake()
        {
            entrada = GetComponent<PlayerInput>();
            bicho = GetComponent<ControladorBicho>();
            vestuario = GetComponent<Vestuario>();
            if (bicho != null)
            {
                bicho.Retirar(true);   // invisible, sin colisiones ni física
                bicho.enabled = false; // y sin leer el mando por su cuenta
            }
            Todos.Add(this);
        }

        void Start()
        {
            if (entrada == null || entrada.actions == null) return;
            accionMover = entrada.actions.FindAction("Mover");
            accionSaltar = entrada.actions.FindAction("Saltar");
            accionAgarrar = entrada.actions.FindAction("Agarrar");
            accionLanzarse = entrada.actions.FindAction("Lanzarse");
        }

        void Update()
        {
            if (ControladorBicho.EntradaBloqueada) return;
            if (accionSaltar != null && accionSaltar.WasPressedThisFrame()) saltos++;
            if (accionLanzarse != null && accionLanzarse.WasPressedThisFrame()) lanzamientos++;
        }

        void OnDestroy()
        {
            Todos.Remove(this);
            if (bicho != null)
            {
                bicho.enabled = true;
                bicho.Retirar(false);
            }
        }
    }
}
