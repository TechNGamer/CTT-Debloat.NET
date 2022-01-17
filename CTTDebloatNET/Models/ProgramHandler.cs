using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CTTDebloatNET.Models {
	[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
	public static class ProgramHandler {
		/// <summary>
		/// An enum representing what programs to install.
		/// </summary>
		public enum Program : ushort {
			WindowsTerminal,
			PowerToys,
			SevenZip,
			AutoHotkey,
			Discord,
			GithubDesktop,
			TranslucentTaskbar,
			Etcher,
			Putty,
			AdvancedIpScanner,
			EverythingSearch,
			Brave,
			Firefox,
			Chrome,
			ShareX,
			ImageGlass,
			Gimp,
			Vlc,
			MediaPlayerClassic,
			VisualStudioCodium,
			VisualStudioCode,
			NotepadPlusPlus,
			AdobeReader,
			SumatraPdf,
			EarTrumpet,
		}

		/// <summary>
		/// The arguments to pass to `winget` to allow it to install the program.
		/// </summary>
		private const string WINGET_ARGS = "install -e {0}";

		private const string WINGET = "winget";

		public static ProgramInfo[] Utils { get; }

		public static ProgramInfo[] Browsers { get; }

		public static ProgramInfo[] Multimedia { get; }

		public static ProgramInfo[] DocumentTools { get; }

		// This dictionary is meant as a quick way to get what each enum value means.
		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		private static readonly Dictionary<Program, string> WINGET_ALIASES = new Dictionary<Program, string> {
			{ Program.Brave, "BraveSoftware.BraveBrowser" },
			{ Program.Chrome, "Google.Chrome" },
			{ Program.Firefox, "Mozilla.Firefox" },
			{ Program.AutoHotkey, "Lexikos.AutoHotkey" },
			{ Program.ImageGlass, "DuongDieuPhap.ImageGlass" },
			{ Program.Discord, "Discord.Discord" },
			{ Program.AdobeReader, "Adobe.AdobeAcrobatReaderDC" },
			{ Program.NotepadPlusPlus, "Notepad++.Notepad++" },
			{ Program.Vlc, "VideoLAN.VLC" },
			{ Program.MediaPlayerClassic, "clsid2.mpc-hc" },
			{ Program.SevenZip, "7zip.7zip" },
			{ Program.VisualStudioCode, "Microsoft.VisualStudioCode" },
			{ Program.VisualStudioCodium, "VSCodium.VSCodium" },
			{ Program.WindowsTerminal, "Microsoft.WindowsTerminal" },
			{ Program.PowerToys, "Microsoft.PowerToys" },
			{ Program.EverythingSearch, "voidtools.Everything" },
			{ Program.SumatraPdf, "SumatraPDF.SumatraPDF" },
			{ Program.GithubDesktop, "GitHub.GitHubDesktop" },
			{ Program.TranslucentTaskbar, "TranslucentTB.TranslucentTB" },
			{ Program.Etcher, "Balena.Etcher" },
			{ Program.AdvancedIpScanner, "Famatech.AdvancedIPScanner" },
			{ Program.ShareX, "ShareX.ShareX" },
			{ Program.Gimp, "GIMP.GIMP" },
			{ Program.EarTrumpet, "File-New-Project.EarTrumpet" }
		};

		static ProgramHandler() {
			using var resStream = Utilities.GetResourceFile( "defaults.json" );
			using var reader    = new StreamReader( resStream, Encoding.UTF8 );
			var       json      = reader.ReadToEnd();
			var       programs  = JsonConvert.DeserializeObject<Dictionary<string, ProgramInfo[]>>( json );

			Utils         = programs["utilities"];
			Browsers      = programs["browsers"];
			Multimedia    = programs["multimedia"];
			DocumentTools = programs["documents"];
		}

		public static async Task InstallProgramAsync( Action<string> output, ProgramInfo info ) {
			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Expected a method to output messages to." );
			}

			output( $"Preparing to install {info.DisplayName} onto the computer.\nInfo Data:\n{info}\n" );

			foreach ( var id in info.IDs ) {
				using var wingetProc = new Process {
					StartInfo = {
						FileName  = WINGET,
						Arguments = string.Format( WINGET_ARGS, id )
					}
				};

				output( $"Installing `{id}` using winget." );

				wingetProc.Start();

				await wingetProc.WaitForExitAsync();
			}
		}

		/// <summary>
		/// Installs the program that is passed to it. This is mostly done through `winget`.
		/// </summary>
		/// <param name="output">A method to output logging information to.</param>
		/// <param name="program">The program to download.</param>
		public static async Task InstallProgramAsync( Action<string> output, Program program ) {
			Process wingetProc;

			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Expected a method to report back to." );
			}

			output( "Getting program ID." );

			// Since the PuTTY option includes a second program, it gets special treatment.
			if ( program == Program.Putty ) {
				await InstallPutty();

				return;
			}

			var wingetProgramId = GetProgramId( program );

			output( $"Using `{wingetProgramId}` to grab program." );

			wingetProc = CreateWingetProcess( wingetProgramId );

			await StartWinget( wingetProc );

			output( "Winget is done installing the program." );

			async Task InstallPutty() {
				const string PUTTY_ARG   = "PuTTY.PuTTY";
				const string WIN_SCP_ARG = "WinSCP.WinSCP";

				output( "Installing both PuTTY and WinSCP." );

				wingetProc = CreateWingetProcess( PUTTY_ARG );

				await StartWinget( wingetProc );

				wingetProc = CreateWingetProcess( WIN_SCP_ARG );

				await StartWinget( wingetProc );

				output( "Winget is done installing the program." );
			}

			async Task StartWinget( Process process ) {
				process.Start();
				process.OutputDataReceived += ( _, args ) => output( $"winget: {args.Data}" );

				await process.WaitForExitAsync();
			}
		}

		internal static async Task StartProgram( string program, string args = "" ) {
			using var proc = new Process {
				StartInfo = {
					FileName  = program,
					Arguments = args
				}
			};

			proc.Start();

			await proc.WaitForExitAsync();
		}

		internal static void StopServices( params string[] serviceNames ) {
			var services = new List<ServiceController>( ServiceController.GetServices() );

			foreach ( var serviceName in serviceNames ) {
				for ( var i = 0; i < services.Count; ++i ) {
					var service = services[i];

					if ( !service.ServiceName.Equals( serviceName, StringComparison.OrdinalIgnoreCase ) ) {
						continue;
					}

					service.Stop( true );

					services.RemoveAt( i );

					break;
				}
			}
		}

		internal static void StartServices( params string[] serviceNames ) {
			var services = new List<ServiceController>( ServiceController.GetServices() );

			foreach ( var serviceName in serviceNames ) {
				for ( var i = 0; i < services.Count; ++i ) {
					var service = services[i];

					if ( !service.ServiceName.Equals( serviceName, StringComparison.OrdinalIgnoreCase ) ) {
						continue;
					}

					service.Start();

					services.RemoveAt( i );

					break;
				}
			}
		}

		private static Process CreateWingetProcess( string program ) {
			var wingetProc = new Process {
				StartInfo = {
					FileName               = WINGET,
					Arguments              = string.Format( WINGET_ARGS, program ),
					UseShellExecute        = false,
					RedirectStandardOutput = true,
					CreateNoWindow         = true,
				}
			};

			return wingetProc;
		}

		private static string GetProgramId( Program program ) {
			try {
				return WINGET_ALIASES[program];
			} catch ( Exception e ) {
				throw new ArgumentException( "Expected a program that has an alias for Winget.", nameof( program ), e );
			}
		}
	}
}
