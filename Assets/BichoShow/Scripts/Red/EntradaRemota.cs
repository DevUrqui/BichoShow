using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// En el anfitrión de una partida online: los controles de un jugador que juega desde otro
    /// ordenador. RedShow le pasa lo que llega por la red y el bicho lo lee como si fuera un mando.
    /// </summary>
    public class EntradaRemota : MonoBehaviour, IEntradaBicho
    {
        /// <summary>Ordenador del jugador (id de cliente de Netcode).</summary>
        public ulong Cliente;
        /// <summary>Número del mando dentro de ese ordenador (puede haber varios jugadores en uno).</summary>
        public byte Local;

        [Tooltip("Si deja de llegar el mando (red cortada), el bicho se para pasado este tiempo (s).")]
        public float tiempoSinMando = 0.5f;

        Vector3 direccion;     // en el mundo, ya girada con la cámara del invitado
        bool agarrar;
        byte saltos, saltosVistos, lanzamientos, lanzamientosVistos;
        bool primera = true;
        float ultimoMensaje = -10f;

        public bool EsPersona => true;

        /// <summary>Lo último que ha mandado el invitado. Las pulsaciones llegan como contadores, así no se pierde ninguna.</summary>
        public void Recibir(Vector2 direccionMundo, bool agarrarPulsado, byte contadorSaltos, byte contadorLanzamientos)
        {
            if (primera)
            {
                saltosVistos = contadorSaltos;
                lanzamientosVistos = contadorLanzamientos;
                primera = false;
            }
            direccion = Vector3.ClampMagnitude(new Vector3(direccionMundo.x, 0f, direccionMundo.y), 1f);
            agarrar = agarrarPulsado;
            saltos = contadorSaltos;
            lanzamientos = contadorLanzamientos;
            ultimoMensaje = Time.unscaledTime;
        }

        bool Activo => Time.unscaledTime - ultimoMensaje < tiempoSinMando;

        /// <summary>El bicho espera el stick relativo a la cámara: se deshace el giro de la cámara de aquí.</summary>
        public Vector2 Mover
        {
            get
            {
                if (!Activo || direccion.sqrMagnitude < 0.0001f) return Vector2.zero;
                var cam = Camera.main;
                if (cam == null) return new Vector2(direccion.x, direccion.z);
                Vector3 adelante = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
                Vector3 derecha = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;
                return new Vector2(Vector3.Dot(direccion, derecha), Vector3.Dot(direccion, adelante));
            }
        }

        public bool Agarrar => Activo && agarrar;

        public bool ConsumirSaltar()
        {
            if (saltos == saltosVistos) return false;
            saltosVistos = saltos;
            return true;
        }

        public bool ConsumirLanzarse()
        {
            if (lanzamientos == lanzamientosVistos) return false;
            lanzamientosVistos = lanzamientos;
            return true;
        }
    }
}
