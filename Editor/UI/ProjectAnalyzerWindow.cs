using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

class ProjectAnalyzerWindow : EditorWindow
{
    private ProjectReport m_ProjectReport;

    private IssueTable m_IssueTable;

    public static GUIStyle Toolbar;
    public static readonly GUIContent analyzeButton = new GUIContent("Analyze Project", "Analyze Project.\nAnalyze Project and list all issues found.");

    private void OnEnable()
    {
        m_ProjectReport = new ProjectReport();
    }

    private void OnGUI()
    {
        Toolbar = "Toolbar";

        Draw();

        if (m_IssueTable != null)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_IssueTable.OnGUI(r);

            DrawDetails();            
        }                
    }
    
    private void Analyze()
    {
        m_ProjectReport.Create();
            
        MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[]
        {
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Category", "Category"),
                width = 100,
                minWidth = 100,
                autoResize = true
            },
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Area", "Area"),
                width = 100,
                minWidth = 100,
                autoResize = true
            },
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Description", "Description"),
                width = 400,
                minWidth = 400,
                autoResize = true
            },
        };

        m_IssueTable = new IssueTable(new TreeViewState(),
            new MultiColumnHeader(new MultiColumnHeaderState(columns)), m_ProjectReport.m_ProjectIssues);

    }

    private void Clear()
    {
        m_IssueTable = null;
        m_ProjectReport.m_ProjectIssues.Clear();
    }
    
    private void Serialize()
    {
        m_ProjectReport.WriteToFile();
    }
    
    private void Draw()
    {
        EditorGUILayout.BeginHorizontal(Toolbar);
        
        GUIStyle buttonStyle = GUI.skin.button;
        if (GUILayout.Button("Analyze", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(60)))
            Analyze();
        if (GUILayout.Button("Clear", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(60)))
            Clear();
        if (GUILayout.Button("Serialize", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(60)))
            Serialize();
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDetails()
    {
        if (m_IssueTable.HasSelection())
        {
            var index = m_IssueTable.GetSelection()[0];
            var issue = m_ProjectReport.m_ProjectIssues[index];
            string text = $"{issue.url}({issue.line},{issue.column})";
            EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
            text = $"Problem: {issue.def.problem}";
            EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
            text = $"Recommendation: {issue.def.solution}";
            EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
        }
    }

    [MenuItem("Window/Analysis/Project Analyzer")]
    public static ProjectAnalyzerWindow ShowDrDREWindow()
    {
        var wnd = GetWindow(typeof(ProjectAnalyzerWindow)) as ProjectAnalyzerWindow;
        if (wnd != null)
        {
            wnd.titleContent = EditorGUIUtility.TrTextContent("Project Analyzer");
        }
        return wnd;
    }
}