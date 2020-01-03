
using System;

namespace Unity.ProjectAuditor.Editor
{
    public struct AssemblyIdentifier
    {
        public string assemblyNameWithIndex { get; private set; }
        public string name { get; private set; }
        // SteveM TODO - Pretty sure this can go. Assemblies don't have indeces. I think the most we'll need is a flag
        // to say whether this is the "All" AssemblyIdentifier (i.e. (assemblyNameWithIndex == "All"))
        public int index { get; private set; }

        public static int kAll = -1;
        public static int kSingle = 0;

        public AssemblyIdentifier(string name, int index)
        {
            this.name = name;
            this.index = index;
            if (index == kAll)
                assemblyNameWithIndex = string.Format("All:{1}", index, name);
            else
                assemblyNameWithIndex = string.Format("{0}:{1}", index, name);
        }

        public AssemblyIdentifier(AssemblyIdentifier assemblyIdentifier)
        {
            name = assemblyIdentifier.name;
            index = assemblyIdentifier.index;
            assemblyNameWithIndex = assemblyIdentifier.assemblyNameWithIndex;
        }

        public AssemblyIdentifier(string assemblyNameWithIndex)
        {
            // SteveM TODO - Pretty sure this can go. Assembly names don't have a foo:N (or N:foo?) naming convention like threads do.
            // So index should probably always be treated as 0 (sorry, "kSingle")
            this.assemblyNameWithIndex = assemblyNameWithIndex;

            string[] tokens = assemblyNameWithIndex.Split(':');
            if (tokens.Length >= 2)
            {
                name = tokens[1];
                string indexString = tokens[0];
                if (indexString == "All")
                {
                    index = kAll;
                }
                else
                {
                    int intValue;
                    if (Int32.TryParse(tokens[0], out intValue))
                        index = intValue;
                    else
                        index = kSingle;
                }
            }
            else
            {
                index = kSingle;
                name = assemblyNameWithIndex;
            }
        }

        void UpdateAssemblyNameWithIndex()
        {
            if (index == kAll)
                assemblyNameWithIndex = string.Format("All:{1}", index, name);
            else
                assemblyNameWithIndex = string.Format("{0}:{1}", index, name);
        }

        public void SetName(string newName)
        {
            name = newName;
            UpdateAssemblyNameWithIndex();
        }

        public void SetIndex(int newIndex)
        {
            index = newIndex;
            UpdateAssemblyNameWithIndex();
        }

        public void SetAll()
        {
            SetIndex(kAll);
        }
    }
}
