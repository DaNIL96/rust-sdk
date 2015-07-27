using UnityEngine;
using System.Collections;

[CreateAssetMenu( menuName = "Workshop/Skin Meta", fileName = "meta.asset" )]
public class WorkshopSkin : WorkshopBase
{
	public enum SkinType : int
	{
		TShirt,
	}

	public static string[] skinPrefabs =
	{
		"Assets/Content/Clothing/tshirt/tshirt.preview.prefab"
	};

	[Header( "Skin Setup" )]
	public SkinType skinType;
	public Material skinMaterial;

	public override void StartPreview()
	{
		var oldPreview = GameObject.Find( "PreviewPrefab" );
		if ( oldPreview != null )
		{
			GameObject.DestroyImmediate( oldPreview, true );
		}

		var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>( skinPrefabs[ (int) skinType ] );
        var pf = GameObject.Instantiate<GameObject>( prefab );

		foreach ( var obj in pf.GetComponentsInChildren<Renderer>())
		{
			if ( !obj.CompareTag( "skin0" ) ) continue;

			var mats = obj.sharedMaterials;
			mats[0] = skinMaterial;
			obj.sharedMaterials = mats;
        }

		pf.name = "PreviewPrefab";
		pf.transform.position = Vector3.zero;
		pf.transform.rotation = Quaternion.identity;
    }
}
