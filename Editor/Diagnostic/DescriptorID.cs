using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    /// <summary>
    /// An unique identifier for a diagnostic descriptor. IDs must have exactly 3 upper case characters, followed by 4 digits.
    /// </summary>
    /// <remarks>
    /// Diagnostic <seealso cref="ProjectIssue"/>s (as opposed to purely informational non-diagnostic ones) all contain a DescriptorID.
    /// The DescriptorID is used to reference the information about a particular issue (for example, "PAC2002" is the code for "Object allocation"),
    /// and the ProjectIssue simply contains the file:line location in code where the allocation occurs and a "PAC2002" DescriptorID.
    /// The descriptor can generally be treated as a string, but for efficient descriptor lookups the string is converted to an int in the struct's constructor.
    /// </remarks>
    [Serializable]
    public struct DescriptorID : IEquatable<DescriptorID>
    {
        [SerializeField] [HideInInspector]
        int m_AsInt;

        [SerializeField]
        string m_AsString;

        public static implicit operator string(DescriptorID d) => d.m_AsString;
        public static implicit operator DescriptorID(string id) => new DescriptorID(id);
        public static implicit operator int(DescriptorID d) => d.m_AsInt;

        /// <summary>
        /// Returns the string representation of the ID
        /// </summary>
        /// <returns>The DescriptorID in string form.</returns>
        public override string ToString() => m_AsString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">String representing a valid and unique Descriptor ID. IDs must have exactly 3 upper case characters, followed by 4 digits.</param>
        public DescriptorID(string id)
        {
            m_AsString = id;

            if (string.IsNullOrEmpty(id))
            {
                // No error message. This is valid when constructing non-diagnostic ProjectIssues
                m_AsInt = -1;
                return;
            }

            // ID must be a whole word (\b), beginning with exactly 3 uppercase letters ([A-Z]{3}), followed by exactly 4 digits (\d{4})
            var regExp = new Regex(@"\b[A-Z]{3}\d{4}\b");
            if (!regExp.IsMatch(id))
            {
                Debug.LogError("Invalid ID string supplied to DescriptorID");
                m_AsInt = -1;
                return;
            }

            m_AsInt = HashDescriptorString(id);
        }

        /// <summary>
        /// Checks whether the ID has been successfully constructed from a correctly-formatted string.
        /// </summary>
        /// <returns>True if the ID is valid</returns>
        public bool IsValid()
        {
            return m_AsInt >= 0;
        }

        /// <summary>
        /// Get the hashed integer representation of the ID
        /// </summary>
        /// <returns>The ID as an int</returns>
        public int AsInt()
        {
            return m_AsInt;
        }

        /// <summary>
        /// Get the string representation of the ID
        /// </summary>
        /// <returns>The ID as a string</returns>
        public string AsString()
        {
            return m_AsString;
        }

        /// <summary>
        /// Get the Descriptor which corresponds to this ID.
        /// </summary>
        /// <returns>The Descriptor which corresponds to this ID. If the ID is invalid, throws an exception.</returns>
        public Descriptor GetDescriptor()
        {
            return DescriptorLibrary.GetDescriptor(AsInt());
        }

        /// <summary>
        /// Checks whether two DescriptorIDs contain the same ID.
        /// </summary>
        /// <param name="other">A DescriptorID to compare to.</param>
        /// <returns>True if the descriptors are the same, false if they are different.</returns>
        public bool Equals(DescriptorID other)
        {
            return m_AsInt == other.m_AsInt;
        }

        /// <summary>
        /// Checks whether two DescriptorIDs contain the same ID.
        /// </summary>
        /// <param name="other">A string representing a descriptor ID to compare to.</param>
        /// <returns>True if the descriptors are the same, false if they are different.</returns>
        public bool Equals(string other)
        {
            return m_AsString == other;
        }

        private static int HashDescriptorString(string id)
        {
            var characters = (short)((char)(id[0] - 'A') << 10 | (char)(id[1] - 'A') << 5 | (char)(id[2] - 'A'));
            var numerical = UInt16.Parse(id.Substring(3));
            return characters << 16 | numerical;
        }
    }
}
