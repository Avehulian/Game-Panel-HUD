using System.Collections.Generic;

namespace GamePanelHUDCore.Utils
{
    /// <summary>
    /// Tracks whether a value has changed since it was last accepted. Used to gate UI writes so a
    /// view only touches TextMeshPro / GameObject state on frames where its inputs actually moved,
    /// which keeps the shared canvas clean and avoids a rebuild.
    ///
    /// Ported from the SPT 5.0 build (upstream performance fork); pure C# with no Unity dependency.
    /// </summary>
    public struct Changed<T>
    {
        private T _last;
        private bool _hasValue;

        /// <summary>
        /// Records <paramref name="value"/> and returns true if it differs from the last accepted
        /// value. The first call always returns true, even for default(T) — otherwise a field whose
        /// real first value happens to be zero or null would never get its initial paint.
        /// </summary>
        public bool TrySet(T value)
        {
            if (_hasValue && EqualityComparer<T>.Default.Equals(_last, value))
            {
                return false;
            }

            _last = value;
            _hasValue = true;
            return true;
        }

        /// <summary>Forces the next TrySet to report a change. Use when the UI is torn down and must be repainted.</summary>
        public void Reset()
        {
            _last = default;
            _hasValue = false;
        }
    }
}
