using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

namespace CTTDebloatNET {
	internal static class Program {
		
		// Initialization code. Don't use any Avalonia, third-party APIs or any
		// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
		// yet and stuff might break.
		[STAThread]
		public static void Main( string[] args ) {
			#if !WINDOWS
			return;
			#else

			// Creates the path of where winget should be.
			var wingetLoc = Path.Combine(
				Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
				"Microsoft",
				"WindowsApps",
				"winget.exe"
			);

			#if !DEBUG
			// Simply elevates to admin levels if it isn't already. Basically sudo but dumber.
			if ( !IsElevated() ) {
				var newProc = new Process {
					StartInfo = {
						FileName = Process.GetCurrentProcess().MainModule.FileName,
						Verb = "RunAs",
						UseShellExecute = true
					}
				};

				newProc.Start();

				return;
			}
			#endif

			// We need winget in order to do things, so this will install it if it isn't present.
			if ( !File.Exists( wingetLoc ) ) {
				var wingetInstaller = new Process() {
					StartInfo = {
						FileName = "ms-appinstaller:?source=https://aka.ms/getwinget"
					}
				};

				wingetInstaller.Start();
				wingetInstaller.WaitForExit();
			}

			// Runs the program.
			BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime( args );
			#endif
		}

		#if WINDOWS
		[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
		private static bool IsElevated() {
			using var identity  = WindowsIdentity.GetCurrent();
			var       principal = new WindowsPrincipal( identity );

			return principal.IsInRole( WindowsBuiltInRole.Administrator );
		}
		#endif

		// Avalonia configuration, don't remove; also used by visual designer.
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace()
				.UseReactiveUI();
	}
}
