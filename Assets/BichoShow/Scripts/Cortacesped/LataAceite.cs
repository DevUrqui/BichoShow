using System;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": lata de aceite que escupe el robot. Si pilla a un bicho, lo tumba
    /// como una tuerca (ProyectilChatarra); al caer en el césped revienta y deja un charco de aceite
    /// (CharcoAceite) donde se resbala.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class LataAceite : MonoBehaviour
    {
        [Tooltip("Plantilla del charco (apagada).")]
        public GameObject charco;

        [NonSerialized] public CortacespedMecha robot;

        bool rota;
        float nacio;

        void OnEnable() => nacio = Time.time;

        void OnCollisionEnter(Collision c)
        {
            if (rota || robot == null || Time.time - nacio < 0.25f) return;
            if (c.rigidbody != null && c.rigidbody.GetComponent<ControladorBicho>() != null) return; // eso lo hace ProyectilChatarra
            if (transform.position.y > robot.Centro.y + 1.5f) return;                                 // aún en el aire (el robot, otra lata…)
            rota = true;

            Vector3 p = c.contactCount > 0 ? c.GetContact(0).point : transform.position;
            p.y = robot.Centro.y + 0.03f;
            Vector3 rel = p - robot.Centro;
            rel.y = 0f;
            // Solo deja charco en el césped (no en el foso ni fuera).
            if (charco != null && rel.magnitude > robot.radioFoso + 0.5f && rel.magnitude < robot.radioPista)
            {
                var go = Instantiate(charco, p, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                go.name = charco.name;
                if (go.TryGetComponent(out CharcoAceite ch)) robot.RegistrarCharco(ch);
                go.SetActive(true);
            }
            Efectos.Impacto(p + Vector3.up * 0.3f, new Color(0.35f, 0.25f, 0.15f), 0.6f);
            Sonidos.Reproducir(Sonido.Rotura, p, 0.7f, 1.4f);
            Destroy(gameObject);
        }
    }
}
