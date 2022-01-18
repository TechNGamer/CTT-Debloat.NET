using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using CTTDebloatNET.Models;
using CTTDebloatNET.ViewModels;
using MessageBox.Avalonia;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;
using ReactiveUI;

namespace CTTDebloatNET.Views {
	/// <summary>
	/// The main window of the application.
	/// </summary>
	public partial class MainWindow : ReactiveWindow<MainWindowViewModel> {
		// This method is because I found myself typing the same thing over and over, which means it needs to be a method.
		private static IMsBoxWindow<ButtonResult> CreateErrorDialog( string title, string message, ButtonEnum options = ButtonEnum.YesNoAbort ) =>
			MessageBoxManager.GetMessageBoxStandardWindow(
				title,
				message,
				options,
				MessageBox.Avalonia.Enums.Icon.Error
			);

		// Simply creates an error message that expands out an exception if there are inner exceptions to also log.
		private static string CreateErrorLog( Exception e ) {
			var builder = new StringBuilder();

			var ex = e;

			// Doing this to not use recursion and prevent a StackOverflowException.
			while ( ex != null ) {
				builder.Append( "Exception: " ).AppendLine( ex.GetType().FullName )
					.Append( "Message: " ).AppendLine( ex.Message )
					.AppendLine( "Stacktrace:" ).AppendLine( ex.StackTrace );

				ex = ex.InnerException;

				if ( ex == null ) {
					continue;
				}

				builder.Append( new string( '-', 15 ) ).Append( "INNER EXCEPTION" ).Append( new string( '-', 15 ) );
			}

			return builder.ToString();
		}

		// As the same suggests, it write any string out to a file.
		private static void WriteErrorToFile( string errorStr, Type errType ) {
			var       filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.DesktopDirectory ), $"{DateTimeOffset.Now:yy-MM-dd_hh-mm-ss}-{errType.FullName}.log" );
			using var fStream  = new FileStream( filePath, FileMode.Create, FileAccess.Write, FileShare.Read );
			using var writer   = new StreamWriter( fStream, Encoding.Unicode );

			writer.WriteLine( "THIS PROGRAM MADE AN ERROR! PLEASE SEND IT TO THE DEVELOPER TO SEE IF THEY CAN'T DO THEIR MAGIC!" );
			writer.Write( errorStr );
		}

		public MainWindow() {
			// Initializing Avalonia, and the Dev Tools if in debug.
			InitializeComponent();
			#if DEBUG
			this.AttachDevTools();
			#endif

			// Redirects the exception handler instead of crashing the program.
			RxApp.DefaultExceptionHandler = Observer.Create<Exception>( ShowErrorDialogAsync );
		}

		// This method is to show an error dialog and asks how the user wishes to proceed.
		private async void ShowErrorDialogAsync( Exception ex ) {
			var errorDialog = CreateErrorDialog( "An exception occurred.", "Something happened, but the program seems stable at the moment. Would you like more details?" );
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
