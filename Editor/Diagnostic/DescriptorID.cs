using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    // stephenm TODO: This needs API documentation comments.
    [Serializable]
    public struct DescriptorID : IEquatable<DescriptorID>
    {
        [SerializeField]
        int m_AsInt;

        [SerializeField]
        string m_AsString;

        public static implicit operator string(DescriptorID d) => d.m_AsString;
        public static implicit operator DescriptorID(string id) => new DescriptorID(id);
        public static implicit operator int(DescriptorID d) => d.m_AsInt;

        public override string ToString() => m_AsString;

        public DescriptorID(string id)
        {
            m_AsString = id;

            if (string.IsNullOrEmpty(id))
            {
                // No error message. This is valid when constructing non-diagnostic ProjectIssues
                m_AsInt = -1;
                return;
            }

            // ID must be a whole word (\b), beginning with exactly 3 uppercase letters, followed by exactly 4 digits
            var regExp = new Regex(@"\b[A-Z]{3}\d{4}\b");
            if (!regExp.IsMatch(id))
            {
                Debug.LogError("Invalid ID string supplied to DescriptorID");
                m_AsInt = -1;
                return;
            }

            m_AsInt = HashDescriptorString(id);
        }

        public static int HashDescriptorString(string id)
        {
            var characters = (short)((char)(id[0] - 'A') << 10 | (char)(id[1] - 'A') << 5 | (char)(id[2] - 'A'));
            var numerical = UInt16.Parse(id.Substring(3));
            return characters << 16 | numerical;
        }

        public bool IsValid()
        {
            return m_AsInt >= 0;
        }

        public int AsInt()
        {
            return m_AsInt;
        }

        public string AsString()
        {
            return m_AsString;
        }

        public Descriptor GetDescriptor()
        {
            return DescriptorLibrary.GetDescriptor(AsInt());
        }

        public bool Equals(DescriptorID other)
        {
            return m_AsInt == other.m_AsInt;
        }

        public bool Equals(string other)
        {
            return m_AsString == other;
        }
    }
}
