using System;
using UnityEngine;

namespace Manor.Gameplay
{
    public static class NoiseBus
    {
        public readonly struct Noise
        {
            public readonly Vector3 Position;
            public readonly float Radius;
            public readonly GameObject Source;
            public Noise(Vector3 position, float radius, GameObject source) { Position = position; Radius = radius; Source = source; }
        }

        public static event Action<Noise> Emitted;
        public static void Emit(Vector3 position, float radius, GameObject source = null)
        {
            if (radius <= 0f) return;
            Emitted?.Invoke(new Noise(position, radius, source));
        }
    }
}
