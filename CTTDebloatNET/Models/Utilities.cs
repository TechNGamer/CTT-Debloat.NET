using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Microsoft.Win32;

// ReSharper disable once IdentifierTypo
namespace CTTDebloatNET.Models {
	/// <summary>
	/// Helps manage what to do and how to do it.
	/// </summary>
	[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
	public static class Utilities {
		/// <summary>
		/// Toggles whether to enable Security Only updates or to receive all the updates.
		/// </summary>
		/// <param name="securityOnly">Whether to apply only security updates or not.</param>
		/// <returns>A background task that is going to set Windows to the one requested.</returns>
		public static Task ToggleWindowsUpdateType( bool securityOnly ) {
			const string DEVICE_METADATA_LOCATION   = @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata";
			const string DRIVER_SEARCH_LOCATION     = @"SOFTWARE\Policies\Microsoft\Windows\DriverSearching";
			const string WINDOWS_UPDATE_LOCATION    = @"SOFTWARE\Policies\Microsoft\WindowsUpdate";
			const string AU_WINDOWS_UPDATE_LOCATION = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
			const string DEVICE_META_KEY            = "PreventDeviceMetadataFromNetwork";
			const string PROMPT_FOR_UPDATE_KEY      = "DontPromptForWindowsUpdate";
			const string SEARCH_UPDATE_KEY          = "DontSearchWindowsUpdate";
			const string UPDATE_WIZARD_KEY          = "DriverUpdateWizardWuSearchEnabled";
			const string DRIVER_QUALITY_UPDATE_KEY  = "ExcludeWUDriversInQualityUpdate";
			const string NO_AUTO_REBOOT_KEY         = "NoAutoRebootWithLoggedOnUsers";
			const string POWER_MANAGEMENT           = "AUPowerManagement";

			return Task.Run( () => {
				if ( securityOnly ) {
					SecurityOnlyUpdate();
				} else {
					NormalUpdate();
				}
			} );

			void SecurityOnlyUpdate() {
				// Device Metadata.
				ChangeDWordRegistryKey( Registry.LocalMachine, DEVICE_METADATA_LOCATION, DEVICE_META_KEY, 1 );
				// Driver Searching.
				ChangeDWordRegistryKey( Registry.LocalMachine, DRIVER_SEARCH_LOCATION, PROMPT_FOR_UPDATE_KEY, 1 );
				ChangeDWordRegistryKey( Registry.LocalMachine, DRIVER_SEARCH_LOCATION, SEARCH_UPDATE_KEY, 1 );
				ChangeDWordRegistryKey( Registry.LocalMachine, DRIVER_SEARCH_LOCATION, UPDATE_WIZARD_KEY );
				// Windows Update.
				ChangeDWordRegistryKey( Registry.LocalMachine, WINDOWS_UPDATE_LOCATION, DRIVER_QUALITY_UPDATE_KEY, 1 );
				ChangeDWordRegistryKey( Registry.LocalMachine, AU_WINDOWS_UPDATE_LOCATION, NO_AUTO_REBOOT_KEY );
				ChangeDWordRegistryKey( Registry.LocalMachine, AU_WINDOWS_UPDATE_LOCATION, POWER_MANAGEMENT );
			}

			void NormalUpdate() {
				// Registry
				var deviceMeta   = Registry.LocalMachine.OpenSubKey( DEVICE_META_KEY, RegistryRights.Delete );
				var driverSearch = Registry.LocalMachine.OpenSubKey( DRIVER_SEARCH_LOCATION, RegistryRights.Delete );
				var windowUpdate = Registry.LocalMachine.OpenSubKey( WINDOWS_UPDATE_LOCATION, RegistryRights.Delete );
				var auUpdate     = Registry.LocalMachine.OpenSubKey( AU_WINDOWS_UPDATE_LOCATION, RegistryRights.Delete );

				deviceMeta?.DeleteValue( DEVICE_META_KEY );
				driverSearch?.DeleteValue( PROMPT_FOR_UPDATE_KEY );
				driverSearch?.DeleteValue( SEARCH_UPDATE_KEY );
				driverSearch?.DeleteValue( UPDATE_WIZARD_KEY );
				windowUpdate?.DeleteValue( DRIVER_QUALITY_UPDATE_KEY );
				auUpdate?.DeleteValue( NO_AUTO_REBOOT_KEY );
				auUpdate?.DeleteValue( POWER_MANAGEMENT );
			}
		}

		/// <summary>
		/// Splits an array into multiple arrays that can be used on separate threads.
		/// </summary>
		/// <remarks>
		/// This method uses <see cref="Environment.ProcessorCount"/>.
		/// </remarks>
		/// <param name="originalArray">The original array.</param>
		/// <typeparam name="T">The type of the array.</typeparam>
		/// <returns>A two dimensional array with arrays for each task.</returns>
		internal static T[][] SplitArrayForMultipleThreads<T>( T[] originalArray ) {
			var top       = new T[Environment.ProcessorCount][];
			var listCount = originalArray.Length / top.Length;

			for ( var i = 0; i < top.Length; ++i ) {
				var sub = new T[listCount];

				Array.Copy( originalArray, i * listCount, sub, 0, listCount );

				top[i] = sub;
			}

			return top;
		}

		/// <summary>
		/// This method is used to change a DWord in the registry.
		/// </summary>
		/// <param name="regKey">The Registry Key to start off.</param>
		/// <param name="location">The location within <paramref name="regKey"/>.</param>
		/// <param name="key">The key to change it's value.</param>
		/// <param name="value">The new value to write to it.</param>
		internal static void ChangeDWordRegistryKey( RegistryKey regKey, string location, string key, uint value = 0 ) {
			using var subKey = regKey.CreateSubKey( location );

			subKey!.SetValue( key, value, RegistryValueKind.DWord );
		}

		/// <summary>
		/// Used to extract content from the assembly.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		internal static Stream GetResourceFile( string file ) {
			var myAsm          = typeof( Utilities ).Assembly;
			var resourceStream = myAsm.GetManifestResourceStream( $"CTTDebloatNET.Resources.{file}" );

			return resourceStream;
		}

		internal static async Task<string> WriteResourceToFile( string file ) {
			var             resourceStream = GetResourceFile( file );
			var             tmpFile        = Path.Combine( Path.GetTempPath(), file );
			await using var fStream        = new FileStream( tmpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read );

			await resourceStream.CopyToAsync( fStream );

			return tmpFile;
		}

		internal static async Task<FileStream> WriteResourceToFileStream( string file ) {
			var       tmpFileLoc = await WriteResourceToFile( file );
			await using var fStream    = new FileStream( tmpFileLoc, FileMode.Open, FileAccess.ReadWrite, FileShare.Read );

			return fStream;
		}

		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		internal static async Task<string> DownloadFile( string url ) {
			var httpClientHandle = new HttpClientHandler() {
				AllowAutoRedirect = true
			};
			var             tmpPath        = Path.GetTempFileName();
			using var       httpClient     = new HttpClient( httpClientHandle, true );
			await using var fStream        = new FileStream( tmpPath, FileMode.Create, FileAccess.Write, FileShare.Read );
			await using var downloadStream = await httpClient.GetStreamAsync( url );

			await downloadStream.CopyToAsync( fStream );

			return tmpPath;
		}
	}
}
