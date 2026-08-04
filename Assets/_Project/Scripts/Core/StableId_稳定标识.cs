using System;
using UnityEngine;

namespace Manor.Core
{
    [Serializable]
    public struct StableId : IEquatable<StableId>
    {
        [SerializeField] private string _value;

        public StableId(string value)
        {
            _value = Normalize(value);
        }

        public string Value => _value ?? string.Empty;
        public bool IsValid => IsValidValue(Value);

        public static bool IsValidValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!(char.IsUpper(character) || char.IsDigit(character) || character == '_')) return false;
            }

            return true;
        }

        public static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }
}
