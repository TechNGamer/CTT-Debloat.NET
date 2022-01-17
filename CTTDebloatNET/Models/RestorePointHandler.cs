using System;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Runtime.InteropServices;

namespace CTTDebloatNET.Models {
	public static class RestorePointHandler {
		
		/// <summary>
		/// The types of events.
		/// </summary>
		[SuppressMessage( "ReSharper", "UnusedMember.Local" )]
		private enum EventType {
			/// <summary>
			/// A system change has begun. A subsequent nested call does not create a new restore point.
			///
			/// <para>
			///		Subsequent calls must use <see cref="EndNestedSystemChange"/>, not <see cref="EndSystemChange"/>.
			/// </para>
			/// </summary>
			BeginNestedSystemChange = 0x66,

			/// <summary>
			/// A system change has begun.
			/// </summary>
			BeginSystemChange = 0x64,

			/// <summary>
			/// A system change has ended.
			/// </summary>
			EndNestedSystemChange = 0x67,

			/// <summary>
			/// A system change has ended.
			/// </summary>
			EndSystemChange = 0x65
		};

		[SuppressMessage( "ReSharper", "UnusedMember.Local" )]
		private enum RestorePointTypes {
			/// <summary>
			/// An application has been installed.
			/// </summary>
			ApplicationInstall = 0x0,

			/// <summary>
			/// An application has been uninstalled.
			/// </summary>
			ApplicationUninstall = 0x1,

			/// <summary>
			/// An application needs to delete the restore point it created. For example, an application would use this flag when a user cancels an installation.
			/// </summary>
			CancelledOperation = 0xd,

			/// <summary>
			/// A device driver has been installed.
			/// </summary>
			DeviceDriverInstall = 0xa,

			/// <summary>
			/// An application has had features added or removed.
			/// </summary>
			ModifiedSettings = 0xc
		}

		/// <summary>
		/// Create a system restore point.
		/// </summary>
		/// <param name="description">The description to use on the restore point.</param>
		/// <exception cref="PlatformNotSupportedException">If the platform is not Windows.</exception>
		[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
		public static void CreateSystemRestorePoint( string description ) {
			if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
				throw new PlatformNotSupportedException( "This program only supports Windows, please make sure you are running it under Windows." );
			}
			
			var       mScope     = new ManagementScope( @"\\localhost\root\default" );
			var       mPath      = new ManagementPath( "SystemRestore" );
			var       options    = new ObjectGetOptions();
			using var mClass     = new ManagementClass( mScope, mPath, options );
			using var parameters = mClass.GetMethodParameters( "CreateRestorePoint" );

			parameters["Description"]      = description;
			parameters["EventType"]        = ( int )EventType.BeginSystemChange;
			parameters["RestorePointType"] = ( int )RestorePointTypes.ModifiedSettings;

			mClass.InvokeMethod( "CreateRestorePoint", parameters, null );
		}
	}
}
