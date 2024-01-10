using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.Util;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class PPtrResolver
    {
        IdProvider<string> m_SerializedFileIdProvider = new IdProvider<string>();
        ObjectIdProvider m_ObjectIdProvider = new ObjectIdProvider();
        // Used to map serialized PPtr fileId to a global serialized file id (the id returned by m_SerializedFileIdProvider).
        List<int> m_ExternalReferenceFileIds = new List<int>();

        public void BeginSerializedFile(string filename, IReadOnlyList<ExternalReference> externalReferences)
        {
            if (m_ExternalReferenceFileIds.Count != 0)
            {
                throw new InvalidOperationException("BeginSerializedFile called twice");
            }

            int serializedFileId = m_SerializedFileIdProvider.GetId(filename.ToLower());

            m_ExternalReferenceFileIds.Clear();
            // The first one is always the current file.
            m_ExternalReferenceFileIds.Add(serializedFileId);
            foreach (var extRef in externalReferences)
            {
                m_ExternalReferenceFileIds.Add(m_SerializedFileIdProvider.GetId(extRef.Path.Substring(extRef.Path.LastIndexOf('/') + 1).ToLower()));
            }
        }

        public int GetObjectId(int localFileId, long pathId)
        {
            if (m_ExternalReferenceFileIds.Count == 0)
            {
                throw new InvalidOperationException("BeginSerializedFile must be called first");
            }

            return m_ObjectIdProvider.GetId((m_ExternalReferenceFileIds[localFileId], pathId));
        }

        public void EndSerializedFile()
        {
            if (m_ExternalReferenceFileIds.Count == 0)
            {
                throw new InvalidOperationException("BeginSerializedFile must be called first");
            }

            m_ExternalReferenceFileIds.Clear();
        }

        public void Reset()
        {
            m_SerializedFileIdProvider = new IdProvider<string>();
            m_ObjectIdProvider = new ObjectIdProvider();
            m_ExternalReferenceFileIds = new List<int>();
        }
    }
}
