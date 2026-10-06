using UnityEngine;

namespace AirXonix
{
    /// <summary>Tiny procedural blips so the build ships with sound and no audio assets.</summary>
    public class Sfx : MonoBehaviour
    {
        AudioSource _src;

        public void Init()
        {
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
        }

        public void Capture() => Play(640f, 0.12f, 0.28f);
        public void Death() => Play(110f, 0.4f, 0.35f);
        public void Level() => Play(880f, 0.28f, 0.3f);

        void Play(float freq, float dur, float vol)
        {
            if (_src == null) return;

            int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var clip = AudioClip.Create("sfx", n, 1, rate, false);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = 1f - i / (float)n;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * env * env;
            }
            clip.SetData(data, 0);
            _src.PlayOneShot(clip);
        }
    }
}
