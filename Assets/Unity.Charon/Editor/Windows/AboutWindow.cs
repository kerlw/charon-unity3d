﻿/*
	Copyright (c) 2016 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Linq;
using Assets.Unity.Charon.Editor.Models;
using Assets.Unity.Charon.Editor.Tasks;
using Assets.Unity.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Assets.Unity.Charon.Editor.Windows
{
	class AboutWindow : EditorWindow
	{
		private string toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		private string licenseHolder = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		private string licenseKey = Resources.UI_UNITYPLUGIN_WINDOWCHECKINGVERSION;
		[NonSerialized]
		private ExecuteCommandTask checkToolsVersion;
		[NonSerialized]
		private Coroutine<LicenseInfo> getLicense;

		public AboutWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOWABOUTCHARONTITLE);
			this.maxSize = minSize = new Vector2(380, 326);
			this.position = new Rect(
				(Screen.width - this.maxSize.x) / 2,
				(Screen.height - this.maxSize.y) / 2,
				this.maxSize.x,
				this.maxSize.y
			);
		}

		protected void OnGUI()
		{
			GUILayout.Box("Charon", new GUIStyle { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWINFOGROUP, EditorStyles.boldLabel);
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSVERSIONLABEL, this.toolsVersion);
			GUI.enabled = false;
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOWLICENSEHOLDER, this.licenseHolder);
			GUI.enabled = this.getLicense != null && this.getLicense.IsCompleted;
			EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWLICENSEKEY, this.licenseKey);
			GUI.enabled = true;
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOWSETTINGSGROUP, EditorStyles.boldLabel);
			GUI.enabled = System.IO.File.Exists(Settings.Current.ToolsPath) == false;
			Settings.Current.ToolsPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSPATH, Settings.Current.ToolsPath);
			GUI.enabled = true;
			Settings.Current.ToolsPort = EditorGUILayout.IntField(Resources.UI_UNITYPLUGIN_WINDOWTOOLSPORT, Settings.Current.ToolsPort);
			Settings.Current.Browser = (Browser)EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOWBROWSER, Settings.Current.Browser);
			if (Settings.Current.Browser == Browser.Custom)
			{
				EditorGUILayout.BeginHorizontal();
				{
					Settings.Current.BrowserPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOWBROWSERPATH, Settings.Current.BrowserPath);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWBROWSEBUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
					{
						Settings.Current.BrowserPath = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_WINDOWBROWSERPATHTITLE, "", "");
						GUI.changed = true;
						this.Repaint();
					}
					GUILayout.Space(5);
				}
				EditorGUILayout.EndHorizontal();
			}
			else
				GUILayout.Space(18);

			GUILayout.Space(18);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOWOKBUTTON, GUILayout.Width(80)))
				this.Close();
			GUILayout.EndHorizontal();

			if (GUI.changed)
			{
				Settings.Current.Version++;
				Settings.Current.Save();
			}
		}

		protected void Update()
		{
			switch (ToolsUtils.CheckTools())
			{
				case ToolsCheckResult.MissingRuntime:
					this.toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKRESULTMISSINGMONOORDOTNET;
					break;
				case ToolsCheckResult.MissingTools:
					this.toolsVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECKRESULTMISSINGTOOLS;
					this.licenseHolder = "";
					this.licenseKey = "";
					break;
				case ToolsCheckResult.Ok:
					if (this.checkToolsVersion == null)
					{
						this.checkToolsVersion = new ExecuteCommandTask(
							Settings.Current.ToolsPath,
							(s, ea) => { if (!string.IsNullOrEmpty(ea.Data)) this.toolsVersion = ea.Data; },
							(s, ea) => { if (!string.IsNullOrEmpty(ea.Data)) this.toolsVersion = ea.Data; },
							"VERSION");
						this.checkToolsVersion.IgnoreFault().ContinueWith(_ => this.Repaint());
						this.checkToolsVersion.RequireDotNetRuntime();
						this.checkToolsVersion.Start();
					}
					else if (this.checkToolsVersion != null && !this.checkToolsVersion.IsRunning && this.getLicense == null)
					{
						this.getLicense = Licenses.GetLicense(scheduleCoroutine: true);
						this.getLicense.ContinueWith((Promise<LicenseInfo> p) =>
						{
							var selectedLicense = p.HasErrors ? default(LicenseInfo) : p.GetResult();
							if (selectedLicense != null)
							{
								this.licenseHolder = selectedLicense.Recipient.FirstName + " " + selectedLicense.Recipient.LastName;
								this.licenseKey = selectedLicense.SerialNumber;
							}
							else
							{
								this.licenseHolder = "";
								this.licenseKey = "";
							}
							this.Repaint();
						});
					}
					break;

			}
		}
	}
}