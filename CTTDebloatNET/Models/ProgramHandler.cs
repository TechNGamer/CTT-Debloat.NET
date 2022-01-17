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

		/// <summary>
		/// An array that contains all of the programs that are utilities.
		/// </summary>
		public static ProgramInfo[] Utils { get; }

		/// <summary>
		/// An array that contains all of the programs that are web browsers.
		/// </summary>
		public static ProgramInfo[] Browsers { get; }

		/// <summary>
		/// An array that contains all of the programs that are Multimedia-related.
		/// </summary>
		public static ProgramInfo[] Multimedia { get; }

		/// <summary>
		/// An array that contains all of the programs that are used to edit/view documents.
		/// </summary>
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

		/// <summary>
		/// Installs a program asynchronously through `winget`.
		/// </summary>
		/// <param name="output">A method that is used to output messages to.</param>
		/// <param name="info">The program to be installed.</param>
		/// <exception cref="ArgumentNullException">Occurs if <paramref name="output"/> is null.</exception>
		public static async Task InstallProgramAsync( Action<string> output, ProgramInfo info ) {
			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Expected a method to output messages to." );
			}

			output( $"Preparing to install {info.DisplayName} onto the computer.\nInfo Data:\n{info}\n" );

			// Loops through each ID to install all the programs associated with that program.
			foreach ( var id in info.IDs ) {
				using var wingetProc = new Process {
					StartInfo = {
						FileName  = WINGET,
						Arguments = string.Format( WINGET_ARGS, id )
					}
				};

				// Meant to notify the use what program is being installed.
				output( $"Installing `{id}` using winget." );

				wingetProc.Start();

				await wingetProc.WaitForExitAsync();
			}
		}

		// Easy way to just start a program without having to create it and await it in the caller's body.
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

			/* This is a bit of a mess, but it is also easy to follow. It simply loops through each service name, using that name,
			 * it looks through all the services that was gathered to see if that service is the one it needs to stop.
			 * If that service is the one to stop it will stop it and remove it from the list and return to the outer loop.
			 * If it cannot find the service, it simply skips it.
			 */
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

			/* This is a bit of a mess, but it is also easy to follow. It simply loops through each service name, using that name,
			 * it looks through all the services that was gathered to see if that service is the one it needs to start.
			 * If that service is the one to start it will start it and remove it from the list and return to the outer loop.
			 * If it cannot find the service, it simply skips it.
			 */
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
