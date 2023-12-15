using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    [Serializable]
    internal class FolderList
    {
        const int k_ElementHeight = 24;
        const int k_ButtonWidth = 28;
        const int k_SidePadding = 4;
        const string k_BaseControlName = "PA_FolderListControl_";

        public EditorWindow Window;

        [SerializeField]
        GUIContent m_FolderIcon;
        [SerializeField]
        GUIContent m_AddIcon;
        [SerializeField]
        GUIContent m_RemoveIcon;

        [SerializeField]
        List<Folder> m_IncludeList = new List<Folder>();
        List<Folder> m_ToRemoveFromInclude = new List<Folder>();
        ReorderableList m_IncludeReorderableList;

        Action m_OnChangedCallback;

        static int m_ControlIndex = 0;

        [SerializeField]
        string m_TitleLabel;

        public List<Folder> Folders => m_IncludeList;

        public FolderList(EditorWindow window, string titleLabel)
        {
            Window = window;

            m_FolderIcon = Utility.GetIcon(Utility.IconType.LoadFolder);
            m_AddIcon = Utility.GetIcon(Utility.IconType.Add);
            m_RemoveIcon = Utility.GetIcon(Utility.IconType.Remove);
            m_TitleLabel = titleLabel;
        }

        public void AddFolder(string fullPath)
        {
            m_IncludeList.Add(new Folder(fullPath));
        }

        public void Draw(Action changeCallback, bool drawEditableFolder)
        {
            m_OnChangedCallback = changeCallback;

            if (m_IncludeReorderableList == null)
            {
                m_IncludeReorderableList = SetupReorderableList(m_TitleLabel, m_IncludeList, m_ToRemoveFromInclude, (list, toRemoveList, fullRect, index) => DrawElement(list, toRemoveList, fullRect, index, drawEditableFolder));
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(k_SidePadding);

            var overallRect = EditorGUILayout.BeginVertical();

            var includeListRect = overallRect;
            includeListRect.height = m_IncludeReorderableList.GetHeight();
            GUILayout.Space(includeListRect.height);

            EditorGUILayout.EndVertical();
            GUILayout.Space(k_SidePadding);
            EditorGUILayout.EndHorizontal();

            m_IncludeReorderableList.elementHeight = k_ElementHeight + (m_IncludeList.Count == 0 ? 2 : 0);

            m_IncludeReorderableList.DoList(includeListRect);

            foreach (var toRemove in m_ToRemoveFromInclude)
            {
                m_IncludeList.Remove(toRemove);
            }
            m_ToRemoveFromInclude.Clear();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape)
            {
                var focusedControl = GUI.GetNameOfFocusedControl();
                if (focusedControl.StartsWith(k_BaseControlName))
                {
                    GUI.FocusControl("");
                    Window.Repaint();
                }
            }
        }

        static ReorderableList SetupReorderableList(string labelText, List<Folder> elements, List<Folder> toRemove, Action<List<Folder>, List<Folder>, Rect, int> drawFunc)
        {
            var newList = new ReorderableList(elements, typeof(Folder));
            newList.drawHeaderCallback = rect =>
            {
                var labelRect = rect;
                labelRect.xMax -= k_ButtonWidth * 3;
                labelRect.xMax -= 1;
                GUI.Label(labelRect, labelText);
                var buttonRect = rect;
                buttonRect.xMin = labelRect.xMax;
                buttonRect.width += 1.5f;
                if (GUI.Button(buttonRect, "Clear all"))
                {
                    elements.Clear();
                }
            };
            newList.displayAdd = false;
            newList.displayRemove = false;
            newList.drawElementCallback = (rect, index, unused, unused1) =>
            {
                drawFunc(elements, toRemove, rect, index);
            };
            newList.drawNoneElementCallback = rect =>
            {
                drawFunc(elements, toRemove, rect, 0);
            };

            return newList;
        }

        void DrawElement(List<Folder> list, List<Folder> toRemoveList, Rect fullRect, int index,
            bool showEditableFolder)
        {
            fullRect.height = k_ElementHeight;

            var filterPathRect = fullRect;
            var folderButtonRect = fullRect;
            var addButtonRect = fullRect;
            var removeButtonRect = fullRect;
            var warningLabelRect = fullRect;

            folderButtonRect.width = k_ButtonWidth + 3;
            addButtonRect.width = k_ButtonWidth;
            removeButtonRect.width = k_ButtonWidth;
            warningLabelRect.width = k_ButtonWidth;

            filterPathRect.xMax -= k_ButtonWidth * 4;
            filterPathRect.xMax -= 3;

            folderButtonRect.x = filterPathRect.xMax + 3;
            addButtonRect.x = folderButtonRect.xMax - 0.5f;
            removeButtonRect.x = addButtonRect.xMax - 0.5f;
            warningLabelRect.x = removeButtonRect.xMax;

            var baseStyle = GUI.skin.button;
            string baseStyleName = baseStyle.name;
            var middleStyle = GUI.skin.FindStyle(baseStyleName + "mid") ?? baseStyle;
            var leftStyle = GUI.skin.FindStyle(baseStyleName + "left") ?? middleStyle;
            var rightStyle = GUI.skin.FindStyle(baseStyleName + "right") ?? middleStyle;

            Folder element = null;
            if (list.Count != 0)
            {
                element = list[index];
                element.Draw(filterPathRect, m_OnChangedCallback, showEditableFolder);
            }
            else
            {
                GUI.Label(filterPathRect, "Empty List");
                GUI.enabled = false;
            }

            if (GUI.Button(folderButtonRect, m_FolderIcon, leftStyle))
            {
                element?.SelectionPopup(m_OnChangedCallback);
            }

            if (GUI.Button(removeButtonRect, m_RemoveIcon, rightStyle))
            {
                toRemoveList.Add(element);
                GUI.FocusControl("");
                m_OnChangedCallback?.Invoke();
            }

            if (list.Count == 0)
            {
                GUI.enabled = true;
            }

            if (GUI.Button(addButtonRect, m_AddIcon, middleStyle))
            {
                if (list.Count == 0)
                {
                    list.Add(new Folder());
                }
                else
                {
                    list.Insert(index + 1, new Folder(list[index].FullPathString));
                }
                GUI.FocusControl("");
                m_OnChangedCallback?.Invoke();
            }

            if (index < list.Count && list[index].m_Status != Folder.FolderStatus.IsValidFolder)
            {
                GUIContent warningContent = Utility.GetIcon(Utility.IconType.Warning, list[index].GetStatusTooltip());
                GUI.Label(warningLabelRect, warningContent, middleStyle);
            }
        }

        [Serializable]
        internal class Folder
        {
            internal enum FolderStatus
            {
                IsValidFolder,
                IsInvalidFolderName,
                IsFileName,
            }

            readonly string k_IsInvalidFolderTooltip = "Folder does not exist";
            readonly string k_IsFileNameTooltip = "File name, not a folder";
            readonly string k_IsValidFolderTooltip = "Valid folder";

            public string FullPathString;

            // not serialized on purpose
            string m_ControlName;

            public FolderStatus m_Status;

            public Folder(string defaultPath = "*")
            {
                SetPath(defaultPath);
            }

            public void Draw(Rect rect, Action changedCallback, bool showEditableFolder)
            {
                if (m_ControlName == null)
                {
                    m_ControlName = k_BaseControlName + m_ControlIndex++;
                }
                // TODO: tokenized stuffs here
                GUI.SetNextControlName(m_ControlName);

                if (showEditableFolder)
                {
                    var newPath = EditorGUI.TextField(rect, FullPathString);
                    if (newPath != FullPathString)
                    {
                        SetPath(newPath);
                        changedCallback?.Invoke();
                    }
                }
                else
                {
                    EditorGUI.LabelField(rect, Utility.ShortenPathToWidth(FullPathString, GUI.skin.label, rect.width));
                }
            }

            public void SelectionPopup(Action onChangedCallback)
            {
                var absolutePath = Path.GetFullPath(FullPathString);
                var folder = EditorUtility.OpenFolderPanel("Filter path", absolutePath, "");

                SetPath(folder);

                onChangedCallback?.Invoke();

                // unfocus textbox so the selected path shows up immediately
                GUI.FocusControl("");
                GUIUtility.ExitGUI();
            }

            private void SetPath(string folder)
            {
                if (!string.IsNullOrEmpty(folder))
                {
                    FullPathString = Path.GetFullPath(folder);
                }

                ValidateCurrentPath();
            }

            private void ValidateCurrentPath()
            {
                if (File.Exists(FullPathString))
                {
                    m_Status = FolderStatus.IsFileName;
                }
                if (Directory.Exists(FullPathString))
                {
                    m_Status = FolderStatus.IsValidFolder;
                }
                else
                {
                    m_Status = FolderStatus.IsInvalidFolderName;
                }
            }

            public string GetStatusTooltip()
            {
                switch (m_Status)
                {
                    case FolderStatus.IsInvalidFolderName:
                        return k_IsInvalidFolderTooltip;
                    case FolderStatus.IsFileName:
                        return k_IsFileNameTooltip;

                    default:
                        return k_IsValidFolderTooltip;
                }
            }
        }
    }
}
