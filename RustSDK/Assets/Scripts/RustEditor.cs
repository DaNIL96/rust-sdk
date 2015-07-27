using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Steamworks;

[InitializeOnLoad]
internal sealed class RustEditor : ScriptableObject
{
	static RustEditor()
	{
		var instance = CreateInstance<RustEditor>();
		instance.Init();
	}

	void Init()
	{
		SteamAPI.Init();
		EditorApplication.update += OnUpdate;
    }

	private void OnUpdate()
	{
		SteamAPI.RunCallbacks(); 
	}
	 
	void OnDisable()  
	{ 
		EditorApplication.update -= OnUpdate;
		SteamAPI.Shutdown();
	}
}