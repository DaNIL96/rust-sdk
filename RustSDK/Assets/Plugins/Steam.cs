using UnityEngine;
using System.Collections;
using Steamworks;
using System;
using System.Runtime.InteropServices;

namespace Facepunch
{
	public static class Steam
	{
		public static string personaName { get { return SteamFriends.GetPersonaName(); } }
		public static AppId_t appID { get { return SteamUtils.GetAppID(); } }

		public static CSteamID steamID { get { return SteamUser.GetSteamID(); } }
		
		public static Steamworks.CallResult<T> Callback<T>( this SteamAPICall_t call, CallResult<T>.APIDispatchDelegate func )
		{
			var cr = new Steamworks.CallResult<T>();
			cr.Set( call, func );
			return cr;
		}

		public static bool IsFinished( SteamAPICall_t handle )
		{
			bool failed;
			return Steamworks.SteamUtils.IsAPICallCompleted( handle, out failed );
        }

		public static void Wait( SteamAPICall_t handle )
		{
			while ( !IsFinished( handle ) )
			{
				SteamAPI.RunCallbacks();
				System.Threading.Thread.Sleep( 10 );
			}
		}

		public static T Result<T>( SteamAPICall_t handle ) where T : new()
		{
			var t = new T();
			var size = Marshal.SizeOf( t );
            System.IntPtr unmanagedAddr = Marshal.AllocHGlobal( size );
			Marshal.StructureToPtr( t, unmanagedAddr, true );
			bool failed;

			Steamworks.SteamUtils.GetAPICallResult( handle, unmanagedAddr, (int)size, CallbackIdentities.GetCallbackIdentity( typeof(T) ), out failed );

			t = (T) Marshal.PtrToStructure( unmanagedAddr, typeof( T ) );
			Marshal.FreeHGlobal( unmanagedAddr );
			unmanagedAddr = IntPtr.Zero;

			return t;
		}
	}
}
