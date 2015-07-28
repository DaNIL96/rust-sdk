using UnityEngine;
using UnityEditor;

[CustomEditor( typeof( WorkshopBase ), true )]
public class WorkshopBaseEditor : Editor
{
	public string changeNotes;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		WorkshopBase item = ( WorkshopBase)target;

		//
		// Tools Area
		//
		{
			GUILayout.Space( 16 );

			GUILayout.Label( "Tools", EditorStyles.boldLabel );

			if ( GUILayout.Button( "Preview In Scene" ) )
			{
				item.StartPreview();
			}
		}

		GUILayout.FlexibleSpace();

		//
		// Workshop Area
		//
		{
			GUILayout.Label( "Workshop", EditorStyles.boldLabel );

			EditorGUILayout.HelpBox( "When you press the button below changes will be made to your workshop items.", MessageType.Info );

			bool canUpload = true;
			//if ( changeNotes.Length <= 1 ) { EditorGUILayout.HelpBox( "Enter a note in the box below to let people know what you're changing", MessageType.Error ); canUpload = false; }
			if ( item.title.Length <= 1 ) { EditorGUILayout.HelpBox( "Your title is too short", MessageType.Error ); canUpload = false; }
			if ( item.description.Length <= 1 ) { EditorGUILayout.HelpBox( "Your title is too short", MessageType.Error ); canUpload = false; }
			if ( item.previewImage == null ) { EditorGUILayout.HelpBox( "You don't have a preview image set", MessageType.Error ); canUpload = false; }

			EditorGUILayout.LabelField( "Change Notes:" );
			changeNotes = EditorGUILayout.TextArea( changeNotes, GUILayout.Height( 64 ) );

			EditorGUILayout.BeginHorizontal();

			if ( item.itemID > 0 )
			{
				if ( GUILayout.Button( "VIEW ONLINE", GUILayout.ExpandWidth( false ) ) )
				{
					Application.OpenURL( "http://steamcommunity.com/sharedfiles/filedetails/?id=489329801" );
				}
			}

			EditorGUILayout.Space();

			GUI.enabled = canUpload;
            if ( GUILayout.Button( item.itemID == 0 ? "Create & Upload" : "Upload Changes", GUILayout.ExpandWidth( false ) ) )
			{
				Steamworks.SteamAPI.Init();
				UploadToWorkshop( item );
				Steamworks.SteamAPI.Shutdown();
				UnityEditor.EditorUtility.ClearProgressBar();
			}
			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
		}
	}

	public void UploadToWorkshop( WorkshopBase item )
	{
		//
		// Item doesn't exist, so create it
		//
		if ( item.itemID == 0 )
		{
			UnityEditor.EditorUtility.DisplayProgressBar( "Workshop Upload", "Creating Item", 0.0f );
			var handle = Steamworks.SteamUGC.CreateItem( Facepunch.Steam.appID, Steamworks.EWorkshopFileType.k_EWorkshopFileTypeMicrotransaction );
			Facepunch.Steam.Wait( handle );
			var result = Facepunch.Steam.Result<Steamworks.CreateItemResult_t>( handle );
			Debug.Log( "RESULT:" + result.m_eResult );
			Debug.Log( "ID:" + result.m_nPublishedFileId.m_PublishedFileId );
			item.itemID = result.m_nPublishedFileId.m_PublishedFileId;
        }

		UnityEditor.EditorUtility.DisplayProgressBar( "Uploading Item", "Starting", 0.0f );

		//
		// Update the item
		//
		{
			var rootFolder = System.IO.Path.GetDirectoryName( Application.dataPath ); // strips off "/Assets"
			var updateHandle = Steamworks.SteamUGC.StartItemUpdate( Facepunch.Steam.appID, new Steamworks.PublishedFileId_t( item.itemID ) );

			Steamworks.SteamUGC.SetItemTitle( updateHandle, item.title );
			Steamworks.SteamUGC.SetItemDescription( updateHandle, item.description );
			Steamworks.SteamUGC.SetItemMetadata( updateHandle, "" );
			Steamworks.SteamUGC.SetItemVisibility( updateHandle, Steamworks.ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic );

			var path = AssetDatabase.GetAssetPath( item );
			path = rootFolder + "/" + System.IO.Path.GetDirectoryName( path );
			Steamworks.SteamUGC.SetItemContent( updateHandle, path );
			Steamworks.SteamUGC.SetItemTags( updateHandle, item.GetTags() );

			var previewPath = rootFolder + "/" + AssetDatabase.GetAssetPath( item.previewImage );
			Steamworks.SteamUGC.SetItemPreview( updateHandle, previewPath );

			var handle = Steamworks.SteamUGC.SubmitItemUpdate( updateHandle, changeNotes );

			while ( !Facepunch.Steam.IsFinished( handle ) )
			{
				Steamworks.SteamAPI.RunCallbacks();

				ulong processed, total;
				var progress = 0.0f;
				var status = Steamworks.SteamUGC.GetItemUpdateProgress( updateHandle, out processed, out total );

				var str = string.Format( "{0}", status );

				if ( processed > 0 && total > 0 )
				{
					progress = ( (float)processed ) / ( (float)total );
					str = string.Format( "{0} - {1}/{2}", status, processed, total );
				}

				UnityEditor.EditorUtility.DisplayProgressBar( "Uploading Item", str, progress );
			}

			var result = Facepunch.Steam.Result<Steamworks.SubmitItemUpdateResult_t>( handle );
			if ( result.m_eResult != Steamworks.EResult.k_EResultOK )
			{
				Debug.LogError( "Upload Result: " + result.m_eResult );
			}
		}
	}
}