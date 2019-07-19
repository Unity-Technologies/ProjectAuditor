using System.Collections;
using System.Collections.Generic;
using Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

class IssueTable : TreeView
{
    enum ColumnIndex
    {
        Category = 0,
        Area,
        Method
    }

    readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

    List<ProjectIssue> m_Issues;

    public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, List<ProjectIssue> issues) : base(state, multicolumnHeader)
    {
        m_Issues = issues;
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        int idForhiddenRoot = -1;
        int depthForHiddenRoot = -1;
        var root = new IssueTableItem(idForhiddenRoot, depthForHiddenRoot, "root", new ProjectIssue());

        int index = 0;
        foreach (var issue in m_Issues)
        {
            var item = new IssueTableItem(index++, 0, "", issue);
            root.AddChild(item);            
        }

        return root;
    }

    protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
    {
        m_Rows.Clear();

        if (rootItem != null && rootItem.children != null)
        {
            foreach (IssueTableItem node in rootItem.children)
            {
                m_Rows.Add(node);
            }
        }

        // SortIfNeeded(m_Rows);

        return m_Rows;
    }
    
    protected override void RowGUI(RowGUIArgs args)
    {
        var item = (IssueTableItem)args.item;
        for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
        {
            CellGUI(args.GetCellRect(i), i, item.m_projectIssue, ref args);
        }
    }

    void CellGUI(Rect cellRect, int column, ProjectIssue projectIssue, ref RowGUIArgs args)
    {
        switch ((ColumnIndex)column)
        {
            case ColumnIndex.Category :
                EditorGUI.LabelField(cellRect, new GUIContent(projectIssue.category, projectIssue.category));
                break;
            case ColumnIndex.Area :
                EditorGUI.LabelField(cellRect, new GUIContent(projectIssue.def.area, projectIssue.def.area));
                break;
            case ColumnIndex.Method :
                string text = $"{projectIssue.def.type.ToString()}.{projectIssue.def.method}"; 
                EditorGUI.LabelField(cellRect, new GUIContent(text, projectIssue.def.method));
                break;
        }
    }
}