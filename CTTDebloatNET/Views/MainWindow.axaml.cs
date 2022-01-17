using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using CTTDebloatNET.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace CTTDebloatNET.Views {
	public partial class MainWindow : ReactiveWindow<MainWindowViewModel> {
		private static IMsBoxWindow<ButtonResult> CreateErrorDialog( string title, string message, ButtonEnum options = ButtonEnum.YesNoAbort ) =>
			MessageBoxManager.GetMessageBoxStandardWindow(
				title,
				message,
				options,
				MessageBox.Avalonia.Enums.Icon.Error
			);

		private static string CreateErrorLog( Exception e ) {
			var builder = new StringBuilder();

			var ex = e;

			// writer.WriteLine( "Please send this log file to the developer please, it could help.\n\n" );

			while ( ex != null ) {
				builder.Append( "Exception: " ).AppendLine( ex.GetType().FullName )
					.Append( "Message: " ).AppendLine( ex.Message )
					.AppendLine( "Stacktrace:" ).AppendLine( ex.StackTrace );

				// writer.Flush();

				ex = ex.InnerException;

				if ( ex == null ) {
					continue;
				}

				builder.Append( new string( '-', 15 ) ).Append( "INNER EXCEPTION" ).Append( new string( '-', 15 ) );
			}

			return builder.ToString();
		}

		private static void WriteErrorToFile( string errorStr, Type errType ) {
			var       filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), $"{DateTimeOffset.Now:yy-MM-dd_hh-mm-ss}-{errType.FullName}.log" );
			using var fStream  = new FileStream( filePath, FileMode.Create, FileAccess.Write, FileShare.Read );
			using var writer   = new StreamWriter( fStream, Encoding.Unicode );

			writer.WriteLine( "THIS PROGRAM MADE AN ERROR! PLEASE SEND IT TO THE DEVELOPER TO SEE IF THEY CAN'T DO THEIR MAGIC!" );
			writer.Write( errorStr );
		}

		private static void CreateConsoleOutput() {
			const uint STD_OUTPUT_HANDLE = 0xFFFFFFF5;

			AllocConsole();

			var defaultStdOut = new IntPtr( 7 );
			var currentStdOut = GetStdHandle( STD_OUTPUT_HANDLE );

			if ( currentStdOut != defaultStdOut ) {
				SetStdHandle( STD_OUTPUT_HANDLE, defaultStdOut );
			}

			var writer = new StreamWriter( Console.OpenStandardOutput() ) {
				AutoFlush = true
			};

			Console.SetOut( writer );

			[DllImport( "kernel32.dll" )]
			static extern IntPtr GetStdHandle( uint nStdHandle );

			[DllImport( "kernel32.dll" )]
			static extern void SetStdHandle( uint nStdHandle, IntPtr handle );

			[DllImport( "kernel32.dll" )]
			static extern bool AllocConsole();
		}

		public MainWindow() {
			InitializeComponent();
			#if DEBUG
			this.AttachDevTools();
			#endif

			RxApp.DefaultExceptionHandler = Observer.Create<Exception>( ShowErrorDialogAsync );
		}

		private async void ShowErrorDialogAsync( Exception ex ) {
			var errorDialog = CreateErrorDialog( "An exception occoured.", "Something happened, but the program seems stable at the moment. Would you like more details?" );
			var answer      = await errorDialog.ShowDialog( this );

			// That is the job of default JetBrains!
			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch ( answer ) {
				case ButtonResult.Yes:
					var errLog = CreateErrorLog( ex );
					
					WriteErrorToFile( errLog, ex.GetType() );

					await MessageBoxManager.GetMessageBoxStandardWindow(
						"Error log create!",
						"The error log has been created and placed on your desktop. Please send it to the developer.",
						ButtonEnum.Ok,
						MessageBox.Avalonia.Enums.Icon.Info
					).ShowDialog( this );
					break;
				case ButtonResult.No:
					return;
				case ButtonResult.Abort:
					Close();
					break;
				default:
					await CreateErrorDialog( "Unknown answer.", $"An invalid answer was passed.", ButtonEnum.Ok ).ShowDialog( this );
					goto case ButtonResult.Abort;
			}
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load( this );
		}
	}
}
