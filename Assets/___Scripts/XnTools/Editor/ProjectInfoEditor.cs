﻿// Original file was created by Unity.com
// Small modifications to allow \n \t in text by Jeremy G. Bond <jeremy@exninja.com>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;


[CustomEditor( typeof( ProjectInfo_SO ) )]
[InitializeOnLoad]
public class ProjectInfoEditor : Editor {
	const string ProjectMenuHeader = "Help";

	static string kShowedProjectInfoSessionStateName = "ProjectInfoEditor.showedProjectInfo";

	static float kSpace = 16f;

	static ProjectInfoEditor() {
		EditorApplication.delayCall += SelectProjectInfoAutomatically;
	}

	static void SelectProjectInfoAutomatically() {
		if ( !SessionState.GetBool( kShowedProjectInfoSessionStateName, false ) ) {
			var pInfo = SelectProjectInfo();
			SessionState.SetBool( kShowedProjectInfoSessionStateName, true );

			//if ( pInfo && !pInfo.loadedLayout ) {
			//	// LoadLayout();
			//	pInfo.loadedLayout = true;
			//}
		}
	}

	// static void LoadLayout() {
	// 	var assembly = typeof( EditorApplication ).Assembly;
	// 	var windowLayoutType = assembly.GetType( "UnityEditor.WindowLayout", true );
	// 	var method = windowLayoutType.GetMethod( "LoadWindowLayout", BindingFlags.Public | BindingFlags.Static );
	// 	method.Invoke( null, new object[] { Path.Combine( Application.dataPath, "TutorialInfo/Layout.wlt" ), false } );
	// }

	[MenuItem( ProjectMenuHeader+"/Show ProjectInfo", false, 1 )]
	static ProjectInfo_SO SelectProjectInfo() {
		var ids = AssetDatabase.FindAssets( "t:ProjectInfo_SO" );
		if ( ids.Length == 1 ) {
			var pInfoObject = AssetDatabase.LoadMainAssetAtPath( AssetDatabase.GUIDToAssetPath( ids[0] ) );

			Selection.objects = new UnityEngine.Object[] { pInfoObject };

			return (ProjectInfo_SO) pInfoObject;
		} else if (ids.Length == 0) {
			Debug.Log( "Couldn't find a ProjectInfo" );
			return null;
		} else {
			Debug.Log( "Found more than 1 ProjectInfo file" );
			return null;
        }
	}
	
	/// <summary>
	/// Adds an "Edit..." item to the context menu for InfoComponent
	/// </summary>
	/// <param name="command"></param>
	[MenuItem("CONTEXT/ProjectInfo/Edit Project Info...")]
	static void EnableEdit(MenuCommand command)
	{
		ProjectInfo_SO info = (ProjectInfo_SO)command.context;
		info.showReadMeEditor = !info.showReadMeEditor;
	}

	protected override void OnHeaderGUI() {
		var pInfo = (ProjectInfo_SO) target;
		Init();

		var iconWidth = Mathf.Min( EditorGUIUtility.currentViewWidth / 3f - 20f, pInfo.iconMaxWidth);

		GUILayout.BeginHorizontal( "In BigTitle" );
		{
			GUILayout.Label( pInfo.icon, GUILayout.Width( iconWidth ), GUILayout.Height( iconWidth ) );
			GUILayout.BeginVertical( "In BigTitle", GUILayout.ExpandHeight( true ) );
			{
				if ( pInfo.projectName != null ) {
					string titleString = ReplaceTabsAndNewLines( pInfo.projectName );
					GUILayout.Label( titleString, TitleStyle );
				} else {
					GUILayout.Label( "You must set this projectName", TitleStyle );
				}
				GUILayout.FlexibleSpace();
				GUILayout.Label($"<b>Author:</b> {pInfo.author}", SubTitleStyle );
				
				if (pInfo.showReadMeEditor) {
					if ( GUILayout.Button( "Finish Editing This ReadMe" ) ) {
						pInfo.showReadMeEditor = false;
					}
				} else {
					if ( GUILayout.Button( "Edit This ReadMe File" ) ) {
						pInfo.showReadMeEditor = true;
					}
				}
			}
			GUILayout.EndVertical();
		}
		GUILayout.EndHorizontal();
	}

	public override void OnInspectorGUI() {
		var pInfo = (ProjectInfo_SO) target;
		Init();
		
		// if (pInfo.showReadMeEditor) {
		// 	if ( GUILayout.Button( "Finish Editing This ReadMe" ) ) {
		// 		pInfo.showReadMeEditor = false;
		// 	}
		// 	GUILayout.Space(10);
		// } else {
		// 	if ( GUILayout.Button( "Edit This ReadMe File" ) ) {
		// 		pInfo.showReadMeEditor = true;
		// 	}
		// 	GUILayout.Space(10);
		// }

		if (pInfo.showReadMeEditor) {
			GUILayout.Label( "<b>Button Actions</b>", HeadingStyle );
			if ( GUILayout.Button( "Export Info to ReadMe.md File for Git" ) ) {
				if ( EditorUtility.DisplayDialog( "Are you sure you want to replace the ReadMe.md file?",
					    "This will replace the ReadMe.md MarkDown file that you see in GitHub and GitLab" +
					    " with information from this file. This cannot be undone.",
					    "Yes, Replace ReadMe.md", "Cancel" ) ) {
					// Undo.RecordObjects(pInfo, "Reset ReadMe to Defaults");
					ExportReadMeMarkDown(pInfo);
				}
			}
			if ( GUILayout.Button( "Reset ReadMe to Defaults" ) ) {
				if ( EditorUtility.DisplayDialog( "Reset ReadMe to Defaults?",
					    "Are you sure you want to reset this ReadMe file to the default values for" +
					    " MI 231 projects? This cannot be undone.",
					    "Yes, Reset It", "Cancel" ) ) {
					// Undo.RecordObjects(pInfo, "Reset ReadMe to Defaults");
					pInfo.ResetSectionsToDefault();
				}
			}
			GUILayout.Space(20);
			GUILayout.Label( "<b>ReadMe Editing Area</b>", HeadingStyle );
			GUILayout.Label( "<i>You can see a preview of what each section will look like below this editor.\n" +
			                 "Edit the <b>Sections</b> below to answer required questions.</i>", BodyStyle );
			GUILayout.Space(10);
			DrawDefaultInspector();
			GUILayout.Space(10);
			GUILayout.Label( "<b>ReadMe Preview Area</b>", HeadingStyle );
			GUILayout.Space(10);
		}
		
		if ( pInfo.sections != null ) {
			foreach ( var section in pInfo.sections ) {
				if ( !string.IsNullOrEmpty( section.heading ) ) {
					GUILayout.Label( section.heading, HeadingStyle );
				}
				if ( !string.IsNullOrEmpty( section.text ) ) {
					string sTxt = ReplaceTabsAndNewLines(section.text);
					GUILayout.Label( sTxt, BodyStyle );
				}
				if ( !string.IsNullOrEmpty( section.linkText ) ) {
					if ( LinkLabel( new GUIContent( section.linkText ) ) ) {
						Application.OpenURL( section.url );
					}
				}
				GUILayout.Space( kSpace );
			}
		}


	}

	void ExportReadMeMarkDown( ProjectInfo_SO pInfo ) {
		string mdText = pInfo.ToMarkDownString();
		Debug.Log( mdText );
	}

	string ReplaceTabsAndNewLines( string sIn ) {
		string sOut = sIn.Replace( "\\n", "\n" ).Replace( "\\t", "\t" );
		return sOut;
	}


	bool m_Initialized;

	GUIStyle LinkStyle { get { return m_LinkStyle; } }
	[SerializeField] GUIStyle m_LinkStyle;

	GUIStyle TitleStyle { get { return m_TitleStyle; } }
	[SerializeField] GUIStyle m_TitleStyle;

	GUIStyle SubTitleStyle { get { return m_SubTitleStyle; } }
	[SerializeField] GUIStyle m_SubTitleStyle;
	
	GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
	[SerializeField] GUIStyle m_HeadingStyle;

	GUIStyle BodyStyle { get { return m_BodyStyle; } }
	[SerializeField] GUIStyle m_BodyStyle;

	void Init() {
		if ( m_Initialized )
			return;
		
		m_BodyStyle = new GUIStyle( EditorStyles.label );
		m_BodyStyle.wordWrap = true;
		m_BodyStyle.fontSize = 14;
		m_BodyStyle.richText = true;

		m_TitleStyle = new GUIStyle( m_BodyStyle );
		m_TitleStyle.fontSize = 26;
		m_TitleStyle.alignment = TextAnchor.MiddleCenter;
		
		m_SubTitleStyle = new GUIStyle( m_BodyStyle );
		m_SubTitleStyle.fontSize = 18;
		m_SubTitleStyle.alignment = TextAnchor.MiddleCenter;

		m_HeadingStyle = new GUIStyle( m_BodyStyle );
		m_HeadingStyle.fontSize = 18;

		m_LinkStyle = new GUIStyle( m_BodyStyle );
		m_LinkStyle.wordWrap = false;
		// Match selection color which works nicely for both light and dark skins
		m_LinkStyle.normal.textColor = new Color( 0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f );
		m_LinkStyle.stretchWidth = false;

		m_Initialized = true;
	}

	bool LinkLabel( GUIContent label, params GUILayoutOption[] options ) {
		var position = GUILayoutUtility.GetRect( label, LinkStyle, options );

		Handles.BeginGUI();
		Handles.color = LinkStyle.normal.textColor;
		Handles.DrawLine( new Vector3( position.xMin, position.yMax ), new Vector3( position.xMax, position.yMax ) );
		Handles.color = Color.white;
		Handles.EndGUI();

		EditorGUIUtility.AddCursorRect( position, MouseCursor.Link );

		return GUI.Button( position, label, LinkStyle );
	}
}

/*


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

[CustomEditor(typeof(ReadMe))]
[InitializeOnLoad]
public class ReadMeEditor : Editor {
	
	static string kShowedReadMeSessionStateName = "ReadMeEditor.showedReadMe";
	
	static float kSpace = 16f;
	
	static ReadMeEditor()
	{
		EditorApplication.delayCall += SelectReadMeAutomatically;
	}
	
	static void SelectReadMeAutomatically()
	{
		if (!SessionState.GetBool(kShowedReadMeSessionStateName, false ))
		{
			var pInfo = SelectReadMe();
			SessionState.SetBool(kShowedReadMeSessionStateName, true);
			
			if (pInfo && !pInfo.loadedLayout)
			{
				LoadLayout();
				pInfo.loadedLayout = true;
			}
		} 
	}
	
	static void LoadLayout()
	{
		EditorUtility.LoadWindowLayout(Path.Combine(Application.dataPath, "Utilities/»ReadMe/ReadMe.wlt"));
	}
	
	[MenuItem("Tutorial/ReadMe")]
	static ReadMe SelectReadMe() 
	{
		var ids = AssetDatabase.FindAssets("ReadMe t:ReadMe");
		if (ids.Length == 1)
		{
			var pInfoObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));
			
			Selection.objects = new UnityEngine.Object[]{pInfoObject};
			
			return (ReadMe)pInfoObject;
		}
		else
		{
			Debug.Log("Couldn't find a pInfo");
			return null;
		}
	}
	
	protected override void OnHeaderGUI()
	{
		var pInfo = (ReadMe)target;
		Init();
		
		var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth/3f - 20f, pInfo.iconMaxWidth);
		
		GUILayout.BeginHorizontal("In BigTitle");
		{
			GUILayout.Label(pInfo.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
			GUILayout.Label(pInfo.projectName, TitleStyle);
		}
		GUILayout.EndHorizontal();
	}
	
	public override void OnInspectorGUI()
	{
		var pInfo = (ReadMe)target;
		Init();
		
		foreach (var section in pInfo.sections)
		{
			if (!string.IsNullOrEmpty(section.heading))
			{
				GUILayout.Label(section.heading, HeadingStyle);
			}
			if (!string.IsNullOrEmpty(section.text))
			{
				GUILayout.Label(section.text, BodyStyle);
			}
			if (!string.IsNullOrEmpty(section.linkText))
			{
				GUILayout.Space(kSpace / 2);
				if (LinkLabel(new GUIContent(section.linkText)))
				{
					Application.OpenURL(section.url);
				}
			}
			GUILayout.Space(kSpace);
		}
	}
	
	
	bool m_Initialized;
	
	GUIStyle LinkStyle { get { return m_LinkStyle; } }
	[SerializeField] GUIStyle m_LinkStyle;
	
	GUIStyle TitleStyle { get { return m_TitleStyle; } }
	[SerializeField] GUIStyle m_TitleStyle;
	
	GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
	[SerializeField] GUIStyle m_HeadingStyle;
	
	GUIStyle BodyStyle { get { return m_BodyStyle; } }
	[SerializeField] GUIStyle m_BodyStyle;
	
	void Init()
	{
		if (m_Initialized)
			return;
		m_BodyStyle = new GUIStyle(EditorStyles.label);
		m_BodyStyle.wordWrap = true;
		m_BodyStyle.fontSize = 14;
		
		m_TitleStyle = new GUIStyle(m_BodyStyle);
		m_TitleStyle.fontSize = 26;

		m_HeadingStyle = new GUIStyle(m_BodyStyle);
		m_HeadingStyle.fontSize = 18;
		m_HeadingStyle.fontStyle = FontStyle.Bold;
		
		m_LinkStyle = new GUIStyle(m_BodyStyle);
		// Match selection color which works nicely for both light and dark skins
		m_LinkStyle.normal.textColor = new Color (0x00/255f, 0x78/255f, 0xDA/255f, 1f);
		m_LinkStyle.stretchWidth = false;
		
		m_Initialized = true;
	}
	
	bool LinkLabel (GUIContent label, params GUILayoutOption[] options)
	{
		var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

		Handles.BeginGUI ();
		Handles.color = LinkStyle.normal.textColor;
		Handles.DrawLine (new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
		Handles.color = Color.white;
		Handles.EndGUI ();

		EditorGUIUtility.AddCursorRect (position, MouseCursor.Link);

		return GUI.Button (position, label, LinkStyle);
	}
}

*/