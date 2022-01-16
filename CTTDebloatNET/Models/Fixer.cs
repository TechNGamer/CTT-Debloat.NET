using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CTTDebloatNET.Models {
	public static class Fixer {

		static Fixer() {
			if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
				throw new PlatformNotSupportedException( "Please run this program under Windows." );
			}
		}

		public static void FixYouPhoneApp(Action<string> output) {
			throw new NotImplementedException( "How many people actual use that app?" );
		}
		
		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
		public static async Task FixWindowsUpdate( Action<string> output ) {
			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Cannot have a null action as input." );
			}

			// ReSharper disable StringLiteralTypo
			var services = new[] {
				"BITS",
				"wauserv",
				"appidsvc",
				"cryptsvc"
			};
			// ReSharper restore StringLiteralTypo

			output( "Stopping update services." );

			await Task.Run( () => ProgramHandler.StopServices( services ) );

			output( "Deleting QMGR Data files." );

			await Task.Run( DeleteQManagerDataFiles );

			output( "Making backups of Software Distribution and CatRoot" );

			await Task.Run( MakeBackups );

			output( "Removing old Windows Update logs." );

			RemoveOldUpdateLogs();

			output( "Resetting the Windows Update Services to default settings." );

			await ProgramHandler.StartProgram( "sc.exe", "sdset bits D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;PU)" );
			await ProgramHandler.StartProgram( "sc.exe", "sdset wuauserv D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;AU)(A;;CCLCSWRPWPDTLOCRRC;;;PU)" );

			output( "Registering some Dynamic Link Libraries." );

			await RegisterDlls();

			output( "Removing WSUS client settings." );

			await Task.Run( DeleteValues );

			output( "Resetting the WinSock." );

			await ProgramHandler.StartProgram( "netsh", "winsock reset" );
			await ProgramHandler.StartProgram( "netsh", "winhttp reset proxy" );

			output( "Deleting all BITS jobs." );

			await Task.Run( RemoveBits );

			output( "Attempting to install the Windows Update Agent." );

			await ProgramHandler.StartProgram( "wusa", $"Windows8-KB2937636-{( Environment.Is64BitProcess ? "x64" : "x86" )}" );

			output( "Starting services back up." );
			
			await Task.Run( () => ProgramHandler.StartServices( services ) );

			output( "Forcing discovery." );

			await ProgramHandler.StartProgram( "wuauclt", "/resetauthorization /detectnow" );

			static void RemoveBits() {
				var downloadManager = new Windows.Bits.DownloadManager();

				foreach ( var job in downloadManager.GetAll() ) {
					job.Cancel();
				}
			}

			static void DeleteValues() {
				var windowsUpdateReg = Registry.LocalMachine.CreateSubKey( @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate" );

				if ( windowsUpdateReg == null ) {
					return;
				}
				
				windowsUpdateReg.DeleteValue( "AccountDomainSid" );
				windowsUpdateReg.DeleteValue( "PingID" );
				windowsUpdateReg.DeleteValue( "SusClientId" );
			}

			static async Task RegisterDlls() {
				const string REGISTER_SERVICE = "regsvr32.exe";
				var dlls = new[] {
					"atl.dll",
					"urlmon.dll",
					"mshtml.dll",
					"shdocvw.dll",
					"browseui.dll",
					"jscript.dll",
					"vbscript.dll",
					"scrrun.dll",
					"msxml.dll",
					"msxml3.dll",
					"msxml6.dll",
					"actxprxy.dll",
					"softpub.dll",
					"wintrust.dll",
					"dssenh.dll",
					"rsaenh.dll",
					"gpkcsp.dll",
					"sccbase.dll",
					"slbcsp.dll",
					"cryptdlg.dll",
					"oleaut32.dll",
					"ole32.dll",
					"shell32.dll",
					"initpki.dll",
					"wuapi.dll",
					"wuaueng.dll",
					"wuaueng1.dll",
					"wucltui.dll",
					"wups.dll",
					"wups2.dll",
					"wuweb.dll",
					"qmgr.dll",
					"qmgrprxy.dll",
					"wucltux.dll",
					"muweb.dll",
				};

				foreach ( var dll in dlls ) {
					var dllPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "System32", dll );

					await ProgramHandler.StartProgram( REGISTER_SERVICE, $"/s \"{dllPath}\"" );
				}
			}

			static void RemoveOldUpdateLogs() {
				var logFile = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "WindowsUpdate.log" );

				if ( File.Exists( logFile ) ) {
					File.Delete( logFile );
				}
			}

			static void MakeBackups() {
				var catRoot      = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "Catroot2" );
				var softwareDist = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "SoftwareDistribution" );

				if ( Directory.Exists( catRoot ) ) {
					Directory.Move( catRoot, catRoot + ".bak" );
				}

				if ( Directory.Exists( softwareDist ) ) {
					Directory.Move( softwareDist, softwareDist + ".bak" );
				}
			}

			static void DeleteQManagerDataFiles() {
				var qMgrFileLocations = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ), "Microsoft", "Network", "Downloader" );

				if ( !Directory.Exists( qMgrFileLocations ) ) {
					qMgrFileLocations = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ), "Application Data", "Microsoft", "Network", "Downloader" );

					if ( !Directory.Exists( qMgrFileLocations ) ) {
						return;
					}
				}

				foreach ( var qMgrFile in Directory.EnumerateFiles( qMgrFileLocations, "qmgr*.dat", SearchOption.AllDirectories ) ) {
					File.Delete( qMgrFile );
				}
			}
		}
	}
}
