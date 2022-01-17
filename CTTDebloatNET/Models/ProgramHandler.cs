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
		/// The arguments to pass to `winget` to allow it to install the program.
		/// </summary>
		private const string WINGET_ARGS = "install -e {0}";

		private const string WINGET = "winget";

		public static ProgramInfo[] Utils { get; }

		public static ProgramInfo[] Browsers { get; }

		public static ProgramInfo[] Multimedia { get; }

		public static ProgramInfo[] DocumentTools { get; }

		static ProgramHandler() {
			using var resStream = Utilities.GetResourceFile( "defaults.json" );
			using var reader    = new StreamReader( resStream, Encoding.UTF8 );
			var       json      = reader.ReadToEnd();
			var       programs  = JsonConvert.DeserializeObject<Dictionary<string, ProgramInfo[]>>( json );

			if ( programs == null ) {
				throw new NullReferenceException( "Expected programs to not be null." );
			}

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
	}
}
