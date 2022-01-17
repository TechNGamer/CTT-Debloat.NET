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
	/// <summary>
	/// This class handles pretty much anything that has to deal with programs.
	/// </summary>
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
			const string UTILITIES_KEY  = "utilities";
			const string BROWSERS_KEY   = "browsers";
			const string MULTIMEDIA_KEY = "multimedia";
			const string DOCUMENTS_KEY  = "documents";
			using var    resStream      = Utilities.GetResourceFile( "defaults.json" );
			using var    reader         = new StreamReader( resStream, Encoding.UTF8 );
			var          json           = reader.ReadToEnd();
			var          programs       = JsonConvert.DeserializeObject<Dictionary<string, ProgramInfo[]>>( json );

			if ( programs == null ) {
				throw new NullReferenceException( "Expected programs to not be null." );
			}

			/* This if has to branches to go down, as you can see. It checks to see if a file called `expand.json` exists within
			 * the same directory it is in. If such a file exists, it attempt to parse that JSON and add those programs to the
			 * proper program list. If it fails at any of those steps, it aborts to commiting what data is in the list to the arrays.
			 * If there is no JSON with that name, it will just default to making the properties reference those arrays.
			 *
			 * It has to be done like this since those properties only have getters, which means only the constructor can set them.
			 * And since this is a static class, it has to be done in the static constructor.
			 */
			if ( File.Exists( "expand.json" ) ) {
				var tmpUtils    = new List<ProgramInfo>( programs[UTILITIES_KEY] );
				var tmpBrowsers = new List<ProgramInfo>( programs[BROWSERS_KEY] );
				var tmpMult     = new List<ProgramInfo>( programs[MULTIMEDIA_KEY] );
				var tmpDoc      = new List<ProgramInfo>( programs[DOCUMENTS_KEY] );

				try {
					using var fStream  = new FileStream( "expand.json", FileMode.Open, FileAccess.Read, FileShare.Read );
					using var myReader = new StreamReader( fStream );

					json = myReader.ReadToEnd();

					programs = JsonConvert.DeserializeObject<Dictionary<string, ProgramInfo[]>>( json );

					tmpUtils.AddRange( programs![UTILITIES_KEY] );
					tmpBrowsers.AddRange( programs![BROWSERS_KEY] );
					tmpMult.AddRange( programs![MULTIMEDIA_KEY] );
					tmpDoc.AddRange( programs![DOCUMENTS_KEY] );
				} catch ( Exception ) {
					// We'll just ignore the exception.
				}

				Utils         = tmpUtils.ToArray();
				Browsers      = tmpBrowsers.ToArray();
				Multimedia    = tmpMult.ToArray();
				DocumentTools = tmpDoc.ToArray();
			} else {
				Utils         = programs[UTILITIES_KEY];
				Browsers      = programs[BROWSERS_KEY];
				Multimedia    = programs[MULTIMEDIA_KEY];
				DocumentTools = programs[DOCUMENTS_KEY];
			}
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
