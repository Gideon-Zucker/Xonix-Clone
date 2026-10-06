using System.Collections.Generic;
using UnityEngine;

namespace AirXonix
{
    /// <summary>Small shared helpers used across the game.</summary>
    public static class Util
    {
        static Font _font;

        /// <summary>A font that exists on every Unity version / platform without importing TMP.</summary>
        public static Font UIFont()
        {
            if (_font != null) return _font;

            string[] builtin = { "LegacyRuntime.ttf", "Arial.ttf" };
            foreach (var n in builtin)
            {
                try { _font = Resources.GetBuiltinResource<Font>(n); } catch { _font = null; }
                if (_font != null) return _font;
            }

            try { _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Sans" }, 24); }
            catch { /* ignored */ }

            return _font;
        }
    }

    /// <summary>Caches Unity's built-in primitive meshes so entities need no imported art.</summary>
    public static class PrimMesh
    {
        static readonly Dictionary<PrimitiveType, Mesh> _cache = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Get(PrimitiveType type)
        {
            if (_cache.TryGetValue(type, out var m) && m != null) return m;

            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            _cache[type] = m;
            return m;
        }
    }
}
