using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>A tiny particle system for uGUI: confetti, star bursts, chip fountains.</summary>
    public class UIParticles : MonoBehaviour
    {
        private class P
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Vel;
            public float Spin;
            public float Angle;
            public float Life;
            public float Age;
            public float Gravity;
            public float Drag;
            public float StartScale;
            public float EndScale;
            public bool Flutter;
        }

        private readonly List<P> _live = new List<P>();
        private RectTransform _layer;

        public static UIParticles Create(Transform parent)
        {
            var rt = UIFactory.Stretch("Particles", parent);
            var p = rt.gameObject.AddComponent<UIParticles>();
            p._layer = rt;
            return p;
        }

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.82f, 0.25f), new Color(0.95f, 0.25f, 0.35f), new Color(0.3f, 0.75f, 1f),
            new Color(0.4f, 0.95f, 0.5f), new Color(1f, 1f, 1f), new Color(0.85f, 0.45f, 1f)
        };

        /// <summary>Confetti raining down from the top of the screen.</summary>
        public void Confetti(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(Random.Range(-960f, 960f), Random.Range(560f, 700f));
                var size = new Vector2(Random.Range(10f, 18f), Random.Range(16f, 28f));
                var p = Spawn(null, pos, size, ConfettiColors[Random.Range(0, ConfettiColors.Length)]);
                p.Vel = new Vector2(Random.Range(-120f, 120f), Random.Range(-250f, -50f));
                p.Gravity = -380f;
                p.Drag = 0.6f;
                p.Spin = Random.Range(-540f, 540f);
                p.Life = Random.Range(2.6f, 4f);
                p.Flutter = true;
            }
        }

        /// <summary>Radial burst of sprites (stars, chips) from a point.</summary>
        public void Burst(Sprite sprite, Vector2 at, int count, float speed, float size, float life, float gravity = -900f, Color? tint = null)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float spd = speed * Random.Range(0.45f, 1f);
                float s = size * Random.Range(0.6f, 1.15f);
                var p = Spawn(sprite, at, new Vector2(s, s), tint ?? Color.white);
                p.Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd + new Vector2(0, speed * 0.35f);
                p.Gravity = gravity;
                p.Drag = 1.2f;
                p.Spin = Random.Range(-400f, 400f);
                p.Life = life * Random.Range(0.7f, 1.1f);
                p.StartScale = 1.2f;
                p.EndScale = 0.4f;
            }
        }

        /// <summary>Sprites flying from one point to another along an arc (payout chips to the bankroll).</summary>
        public void Fountain(Sprite sprite, Vector2 from, Vector2 to, int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Spawn(sprite, from + Random.insideUnitCircle * 30f, new Vector2(size, size), Color.white);
                float t = Random.Range(0.75f, 1.05f);
                float g = -1500f;
                Vector2 d = to - from;
                p.Vel = new Vector2(d.x / t, (d.y - 0.5f * g * t * t) / t);
                p.Gravity = g;
                p.Drag = 0f;
                p.Spin = Random.Range(-200f, 200f);
                p.Life = t;
                p.StartScale = 1f;
                p.EndScale = 0.7f;
            }
        }

        private P Spawn(Sprite sprite, Vector2 pos, Vector2 size, Color color)
        {
            var img = UIFactory.Image("p", _layer, pos, size, sprite, color);
            img.type = Image.Type.Simple;
            var p = new P { Rt = img.rectTransform, Img = img, StartScale = 1f, EndScale = 1f };
            _live.Add(p);
            return p;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.Rt == null)
                {
                    if (p.Rt != null) Destroy(p.Rt.gameObject);
                    _live.RemoveAt(i);
                    continue;
                }
                p.Vel.y += p.Gravity * dt;
                p.Vel *= 1f / (1f + p.Drag * dt);
                if (p.Flutter) p.Vel.x += Mathf.Sin((p.Age + i) * 6f) * 140f * dt;
                p.Rt.anchoredPosition += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Angle += p.Spin * dt;
                p.Rt.localRotation = Quaternion.Euler(0, p.Flutter ? Mathf.Sin(p.Age * 9f + i) * 70f : 0f, p.Angle);
                p.Rt.localScale = Vector3.one * Mathf.Lerp(p.StartScale, p.EndScale, k);
                var c = p.Img.color;
                c.a = k > 0.75f ? Mathf.Lerp(1f, 0f, (k - 0.75f) / 0.25f) : 1f;
                p.Img.color = c;
            }
        }
    }
}
