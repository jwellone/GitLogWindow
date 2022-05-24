using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

#nullable enable

namespace jwelloneEditor
{
	public class GitLogWindow : EditorWindow
	{
		const string USER_SETTING_KEY_TOP_URL = "GIT_LOG_TOP_URL";
		const string USER_SETTING_KEY_LOG_NUM = "GIT_LOG_NUM";

		class Data
		{
			public readonly string commit;
			public readonly string author;
			public readonly string date;
			public readonly string comment;

			public Data(string[] args)
			{
				commit = args[0].Replace("commit ", "");
				author = args[1].Replace("Author: ", "");
				date = args[2].Replace("Date: ", "");
				comment = args.Length > 4 ? args[4] : args[3];
			}
		}

		[SerializeField] UnityEngine.Object? _selectObject;

		string _topUrl = string.Empty;
		string _logNum = string.Empty;
		Vector2 _scrollPosition;
		List<Data> _data = new List<Data>();

		[MenuItem("jwellone/Window/GitLogWindow")]
		static void Open()
		{
			GetWindow<GitLogWindow>("Git log");
		}


		void OnGUI()
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label("Git page top url", GUILayout.Width(92));
			_topUrl = GUILayout.TextField(_topUrl);

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Label("Select", GUILayout.Width(92));
			EditorGUILayout.ObjectField(_selectObject, typeof(UnityEngine.Object), false);
			EditorGUI.EndDisabledGroup();

			if (GUILayout.Button("Open Url", GUILayout.Width(64)))
			{
				var url = Path.Combine(_topUrl, $"{AssetDatabase.GetAssetPath(_selectObject)}");
				Application.OpenURL(url);
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			GUILayout.Label("Number of logs", GUILayout.Width(92));
			_logNum = GUILayout.TextField(_logNum);
			GUILayout.EndHorizontal();


			EditorGUILayout.Space();

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("commit", GUILayout.Width(128));
			EditorGUILayout.LabelField("author", GUILayout.Width(300));
			EditorGUILayout.LabelField("date", GUILayout.Width(210));
			EditorGUILayout.LabelField("comment");
			EditorGUILayout.EndHorizontal();

			GUILayout.Box("", GUILayout.Width(position.width), GUILayout.Height(1));


			foreach (var data in _data)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.TextField(data.commit, GUILayout.Width(128));
				EditorGUILayout.LabelField(data.author, GUILayout.Width(300));
				EditorGUILayout.LabelField(data.date, GUILayout.Width(210));
				EditorGUILayout.TextArea(data.comment);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();
		}

		void OnEnable()
		{
			_topUrl = EditorUserSettings.GetConfigValue(USER_SETTING_KEY_TOP_URL);
			if (string.IsNullOrEmpty(_topUrl))
			{
				_topUrl = "Exsample https://github.com/xxx/xxx/tree/main";
			}

			_logNum = EditorUserSettings.GetConfigValue(USER_SETTING_KEY_LOG_NUM);
			if (!int.TryParse(_logNum, out var result))
			{
				_logNum = string.Empty;
			}

			Selection.selectionChanged += OnSelectionChanged;
		}

		void OnInspectorUpdate()
		{
			Repaint();
		}

		void OnDisable()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			EditorUserSettings.SetConfigValue(USER_SETTING_KEY_TOP_URL, _topUrl);
			EditorUserSettings.SetConfigValue(USER_SETTING_KEY_LOG_NUM, _logNum);
		}

		void OnSelectionChanged()
		{
			var objs = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
			_selectObject = objs.Length > 0 ? objs[0] : null;

			if (_selectObject == null)
			{
				return;
			}

			var option = string.Empty;
			if (int.TryParse(_logNum, out var num))
			{
				option = $"-n {num} ";
			}

			var path = AssetDatabase.GetAssetPath(_selectObject).Replace("Assets/", "");
			var command = GitCommand.Exec($"log {option}{path}");
			command.Wait();

			var log = command.Result;
			_data.Clear();

			if (string.IsNullOrEmpty(log))
			{
				return;
			}

			var args = log.Split('\n');
			var list = new List<string>();
			var termTag = "commit";
			for (var i = 0; i < args.Length; ++i)
			{
				var arg = args[i];

				if (i == args.Length - 1)
				{
					list.Add(arg);
					_data.Add(new Data(list.ToArray()));
					continue;
				}

				if ((i > 0 && arg.Length >= termTag.Length && arg.Substring(0, termTag.Length) == termTag))
				{
					_data.Add(new Data(list.ToArray()));
					list.Clear();
				}

				list.Add(arg);
			}
		}
	}
}