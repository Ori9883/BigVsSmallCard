using UnityEngine;

namespace FirstView
{
    /// <summary>
    /// Animates a Point Light to simulate candle flicker using Perlin noise.
    /// Attach to any GameObject with a Light component.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CandleFlicker : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [SerializeField] private float baseIntensity = 1.2f;
        [SerializeField] private float noiseAmplitude = 0.3f;
        [SerializeField] private float noiseSpeed = 3f;
        [SerializeField] private float jitterAmount = 0.05f;
        [SerializeField] private float jitterFrequency = 15f;

        [Header("Color Variation")]
        [SerializeField] private Color warmColor = new Color(1f, 0.85f, 0.6f);
        [SerializeField] private Color hotColor = new Color(1f, 0.7f, 0.4f);
        [SerializeField] private float colorBlendSpeed = 2f;

        private Light light;
        private float seed;

        private void Awake()
        {
            light = GetComponent<Light>();
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (light == null) return;

            // Perlin noise for smooth flicker
            float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, seed);
            float flicker = (noise - 0.5f) * 2f * noiseAmplitude;

            // Random jitter for sharp candle pops
            float jitter = Mathf.Sin(Time.time * jitterFrequency + seed)
                         * Mathf.Sin(Time.time * jitterFrequency * 1.3f + seed * 2f)
                         * jitterAmount;

            light.intensity = baseIntensity + flicker + jitter;

            // Subtle color variation
            float colorNoise = Mathf.PerlinNoise(Time.time * colorBlendSpeed + seed, seed + 50f);
            light.color = Color.Lerp(warmColor, hotColor, colorNoise);
        }
    }
}
