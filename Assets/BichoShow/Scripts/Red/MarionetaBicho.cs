using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// En un invitado de una partida online: la copia de un bicho del anfitrión. Guarda las
    /// últimas "fotos" que llegan por la red y dibuja el bicho un poco en el pasado (RedShow.TiempoDibujo),
    /// interpolando entre dos fotos: así se mueve suave aunque los mensajes lleguen a trompicones.
    /// </summary>
    [RequireComponent(typeof(ControladorBicho))]
    public class MarionetaBicho : MonoBehaviour
    {
        public const byte FlagEnSuelo = 1, FlagTumbado = 2, FlagMano = 4, FlagRetirado = 8, FlagBot = 16, FlagCelebrando = 32;

        public struct Foto
        {
            public float tiempo;
            public Vector3 posicion;
            public Quaternion rotacion;
            public Vector3 velocidad;
            public Vector3 puntoMano;
            public byte flags;
        }

        /// <summary>Id de red del bicho (lo pone el anfitrión).</summary>
        public byte Id;
        /// <summary>Ordenador que lo controla y número de mando allí (para saber si es "mío").</summary>
        public ulong Dueno;
        public byte Local;
        public int Indice;
        public bool EsBot;
        public Color ColorJugador { get; private set; } = Color.clear;

        readonly List<Foto> fotos = new List<Foto>();
        ControladorBicho bicho;
        AnimadorBicho animador;
        Vestuario vestuario;
        int gorro = -1, complemento = -1, cuerpo = -1;

        public ControladorBicho Bicho => bicho;

        void Awake()
        {
            bicho = GetComponent<ControladorBicho>();
            animador = GetComponent<AnimadorBicho>();
            vestuario = GetComponent<Vestuario>();
        }

        public void Recibir(Foto foto)
        {
            if (fotos.Count > 0 && foto.tiempo <= fotos[fotos.Count - 1].tiempo) return; // llegó tarde
            fotos.Add(foto);
            if (fotos.Count > 30) fotos.RemoveAt(0);
        }

        /// <summary>Color y ropa (llegan en cada foto; solo se aplican si cambian).</summary>
        public void Aspecto(Color color, int gorroNuevo, int complementoNuevo, int cuerpoNuevo)
        {
            if (color != ColorJugador)
            {
                ColorJugador = color;
                bicho.AsignarColor(color);
            }
            if (vestuario != null && (gorroNuevo != gorro || complementoNuevo != complemento || cuerpoNuevo != cuerpo))
            {
                gorro = gorroNuevo;
                complemento = complementoNuevo;
                cuerpo = cuerpoNuevo;
                vestuario.Poner(gorro, complemento, cuerpo);
            }
        }

        void Update()
        {
            if (fotos.Count == 0) return;
            float t = (float)RedShow.TiempoDibujo;

            // Quedarse con la última foto anterior a t y las siguientes.
            while (fotos.Count >= 2 && fotos[1].tiempo <= t) fotos.RemoveAt(0);

            Foto a = fotos[0];
            Foto b = fotos.Count > 1 ? fotos[1] : a;
            Vector3 pos;
            Quaternion rot;
            Vector3 vel, mano;
            byte flags;
            if (fotos.Count > 1 && b.tiempo > a.tiempo)
            {
                float k = Mathf.Clamp01((t - a.tiempo) / (b.tiempo - a.tiempo));
                pos = Vector3.Lerp(a.posicion, b.posicion, k);
                rot = Quaternion.Slerp(a.rotacion, b.rotacion, k);
                vel = Vector3.Lerp(a.velocidad, b.velocidad, k);
                mano = Vector3.Lerp(a.puntoMano, b.puntoMano, k);
                flags = k < 0.5f ? a.flags : b.flags;
            }
            else
            {
                // Sin foto nueva aún: seguir un poquito con su velocidad (como mucho 0.15 s).
                float extra = Mathf.Clamp(t - a.tiempo, 0f, 0.15f);
                pos = a.posicion + a.velocidad * extra;
                rot = a.rotacion;
                vel = a.velocidad;
                mano = a.puntoMano;
                flags = a.flags;
            }

            bool retirado = (flags & FlagRetirado) != 0;
            if (retirado != bicho.Retirado) bicho.Retirar(retirado);
            if (retirado) return;

            bicho.AplicarEstadoRed(pos, rot, vel, (flags & FlagEnSuelo) != 0, (flags & FlagTumbado) != 0,
                                   (flags & FlagMano) != 0, mano);
            if ((flags & FlagCelebrando) != 0 && animador != null) animador.Celebrar(0.2f);
        }
    }
}
