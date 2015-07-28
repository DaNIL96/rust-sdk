using UnityEngine;
using System.Collections.Generic;

public class WorkshopBase : ScriptableObject
{
	[Tooltip( "Leave this at 0 if this is a new item. That way the item will be created when you press upload." )]
	public ulong itemID;

	public string title;

	[TextArea( 8, 8 )]
	public string description;

	public Texture2D previewImage;

#if UNITY_EDITOR

	public virtual void StartPreview()
	{
		Debug.LogWarning( "StartPreview is not Implemented" );
	}

	public virtual List<string> GetTags()
	{
		return new List<string>();
	}

#endif
}
