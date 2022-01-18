using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CTTDebloatNET.Models;
using ReactiveUI;

// ReSharper disable once IdentifierTypo
namespace CTTDebloatNET.ViewModels {
	/// <summary>
	/// What the buttons are requesting to be done.
	/// </summary>
	/* Did it this way to prevent having a bunch of method that are just going to end up doing pretty much the same thing. */
	public enum ButtonRequest : ushort {
		// System Tweaks
		EssentialTweaks = 0,
		UndoEssentialTweaks,
		DisableActionCenter,
		EnableActionCenter,
		ShowTrayIcons,
		HideTrayIcons,
		SystemDarkMode,
		SystemLightMode,
		AppearanceFx,
		PerformanceFx,
		DisallowBackgroundApps,
		AllowBackgroundApps,
		DeleteOneDrive,
		EnableOneDrive,
		DisableCortana,
		EnableCortana,
		EnableLocationTracking,
		EnableClipboardHistory,
		EnableHibernation,
		SetTimeToUtc,
		ReinstallMsStoreApps,
		RemoveMsStoreApps,

		// Misc Fixes
		YourPhoneFix,
		WindowsUpdateReset,
		NetworkConnections,
		OldControlPanel,
		OldSoundPanel,
		OldSystemPanel,

		// Windows Update
		DefaultSettings,
		SecurityOnly
	}

	/// <summary>
	/// The main window's ViewModel.
	/// </summary>
	public class MainWindowViewModel : ViewModelBase {
		/// <summary>
		/// If the ViewModel is processing data or not.
		/// </summary>
		public bool IsProcessing {
			get => isProcessing;
			private set => this.RaiseAndSetIfChanged( ref isProcessing, value );
		}

		/// <summary>
		/// A string of everything that has been outputted during it's operations.
		/// </summary>
		public string LogOutput => outputBuilder.ToString();

		public SelectionMode ProgramListSelectionMode => SelectionMode.Multiple | SelectionMode.Toggle;

		/// <summary>
		/// The command that most buttons will call upon.
		/// </summary>
		public ICommand MainButtonCommand { get; }

		/// <summary>
		/// A special command that only install buttons should use.
		/// </summary>
		public ICommand InstallProgram { get; }

		public ObservableCollection<ProgramInstallOptionViewModel> UtilPrograms         { get; }
		public ObservableCollection<ProgramInstallOptionViewModel> UtilProgramsSelected { get; }

		public ObservableCollection<ProgramInstallOptionViewModel> BrowserPrograms         { get; }
		public ObservableCollection<ProgramInstallOptionViewModel> BrowserProgramsSelected { get; }

		public ObservableCollection<ProgramInstallOptionViewModel> MultiPrograms         { get; }
		public ObservableCollection<ProgramInstallOptionViewModel> MultiProgramsSelected { get; }

		public ObservableCollection<ProgramInstallOptionViewModel> DocPrograms         { get; }
		public ObservableCollection<ProgramInstallOptionViewModel> DocProgramsSelected { get; }

		private bool isProcessing;

		private readonly StringBuilder outputBuilder;

		public MainWindowViewModel() {
			outputBuilder     = new StringBuilder();
			MainButtonCommand = ReactiveCommand.CreateFromTask( ( Func<ButtonRequest, Task> )ProcessButtonRequestHandler );
			InstallProgram    = ReactiveCommand.CreateFromTask( ( Func<Task> )InstallProgramHandler );

			UtilPrograms            = new ObservableCollection<ProgramInstallOptionViewModel>();
			UtilProgramsSelected    = new ObservableCollection<ProgramInstallOptionViewModel>();
			BrowserPrograms         = new ObservableCollection<ProgramInstallOptionViewModel>();
			BrowserProgramsSelected = new ObservableCollection<ProgramInstallOptionViewModel>();
			MultiPrograms           = new ObservableCollection<ProgramInstallOptionViewModel>();
			MultiProgramsSelected   = new ObservableCollection<ProgramInstallOptionViewModel>();
			DocPrograms             = new ObservableCollection<ProgramInstallOptionViewModel>();
			DocProgramsSelected     = new ObservableCollection<ProgramInstallOptionViewModel>();

			foreach ( var util in ProgramHandler.Utils ) {
				UtilPrograms.Add( new ProgramInstallOptionViewModel( util ) );
			}

			foreach ( var browser in ProgramHandler.Browsers ) {
				BrowserPrograms.Add( new ProgramInstallOptionViewModel( browser ) );
			}

			foreach ( var multi in ProgramHandler.Multimedia ) {
				MultiPrograms.Add( new ProgramInstallOptionViewModel( multi ) );
			}

			foreach ( var doc in ProgramHandler.DocumentTools ) {
				DocPrograms.Add( new ProgramInstallOptionViewModel( doc ) );
			}
		}

		// This method simply set's the `IsProcessing` flag and calls the `InstallProgramAsync` method.
		private async Task InstallProgramHandler() {
			IsProcessing = true;

			WriteOutput( "Beginning to install the utility programs selected." );

			await InstallProgramList( WriteOutput, UtilProgramsSelected );
			
			UtilProgramsSelected.Clear();

			WriteOutput( "Beginning to install the selected web browsers." );

			await InstallProgramList( WriteOutput, BrowserProgramsSelected );
			
			BrowserProgramsSelected.Clear();

			WriteOutput( "Beginning to install the Multimedia programs selected." );

			await InstallProgramList( WriteOutput, MultiProgramsSelected );
			
			MultiProgramsSelected.Clear();

			WriteOutput( "Beginning to install the Document Editors and Viewers." );

			await InstallProgramList( WriteOutput, DocProgramsSelected );
			
			DocProgramsSelected.Clear();

			IsProcessing = false;

			static async Task InstallProgramList( Action<string> output, IEnumerable<ProgramInstallOptionViewModel> programViewModels ) {
				foreach ( var programViewModel in programViewModels ) {
					var programInfo = programViewModel.Program;

					await ProgramHandler.InstallProgramAsync( output, programInfo );
				}
			}
		}

		// This method is the initial kickoff, and does some basic things before and after.
		private async Task ProcessButtonRequestHandler( ButtonRequest request ) {
			IsProcessing = true;

			WriteOutput( "Beginning the request, depending on the request, this might take a while." );

			try {
				await ProcessRequest( request );
			} catch ( Exception ) {
				WriteOutput( "The request finished with an error." );

				IsProcessing = false;

				throw;
			}

			WriteOutput( "The request is finished." );

			IsProcessing = false;
		}

		/* This method is a lot more complex, and has been split into multiple methods for easier readability. Though been simplified since
		 * the program install has been changed to do things differently. */
		[SuppressMessage( "ReSharper", "ConvertIfStatementToSwitchExpression" )]
		private Task ProcessRequest( ButtonRequest request ) {
			if ( request <= ButtonRequest.RemoveMsStoreApps ) {
				return TweakHandler( request );
			}

			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			// ReSharper disable StringLiteralTypo
			return request switch {
				ButtonRequest.YourPhoneFix       => Task.Run( () => Fixer.FixYouPhoneApp( WriteOutput ) ),
				ButtonRequest.WindowsUpdateReset => Fixer.FixWindowsUpdate( WriteOutput ),
				ButtonRequest.NetworkConnections => ProgramHandler.StartProgram( "ncpa.cpl" ),
				ButtonRequest.OldControlPanel    => ProgramHandler.StartProgram( "control" ),
				ButtonRequest.OldSoundPanel      => ProgramHandler.StartProgram( "mmsys.cpl" ),
				ButtonRequest.OldSystemPanel     => Task.CompletedTask,
				ButtonRequest.DefaultSettings    => Utilities.ToggleWindowsUpdateType( false ),
				ButtonRequest.SecurityOnly       => Utilities.ToggleWindowsUpdateType( true ),
				_                                => throw new ArgumentOutOfRangeException( nameof( request ), request, null )
			};
			// ReSharper restore StringLiteralTypo
		}

		// This method is simply used to reduce the amount of entries in the `ProcessRequest` method, in my opinion, helps with readability.
		[SuppressMessage( "ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault" )]
		private Task TweakHandler( ButtonRequest request ) => request switch {
			// Methods that already return a Task.
			ButtonRequest.DeleteOneDrive       => Tweaks.ToggleOneDrive( false ),
			ButtonRequest.EnableOneDrive       => Tweaks.ToggleOneDrive( true ),
			ButtonRequest.ReinstallMsStoreApps => Tweaks.ReinstallBloatware(),
			ButtonRequest.RemoveMsStoreApps    => Tweaks.PurgeBloatware(),
			ButtonRequest.AppearanceFx         => Tweaks.ToggleVisualEffects( false ),
			ButtonRequest.PerformanceFx        => Tweaks.ToggleVisualEffects( true ),
			ButtonRequest.EssentialTweaks      => Tweaks.EssentialTweaks( WriteOutput ),
			ButtonRequest.UndoEssentialTweaks  => Tweaks.UndoEssentialTweaks( WriteOutput ),
			// Methods where Task.Run is needed.
			ButtonRequest.DisableActionCenter    => Task.Run( () => Tweaks.ToggleActionCenter( false ) ),
			ButtonRequest.EnableActionCenter     => Task.Run( () => Tweaks.ToggleActionCenter( true ) ),
			ButtonRequest.ShowTrayIcons          => Task.Run( () => Tweaks.ToggleTrayIcons( false ) ),
			ButtonRequest.HideTrayIcons          => Task.Run( () => Tweaks.ToggleTrayIcons( true ) ),
			ButtonRequest.SystemDarkMode         => Task.Run( () => Tweaks.ToggleThemeMode( true ) ),
			ButtonRequest.SystemLightMode        => Task.Run( () => Tweaks.ToggleThemeMode( false ) ),
			ButtonRequest.DisableCortana         => Task.Run( () => Tweaks.ToggleCortana() ),
			ButtonRequest.EnableCortana          => Task.Run( () => Tweaks.ToggleCortana( false ) ),
			ButtonRequest.SetTimeToUtc           => Task.Run( Tweaks.SetTimeToUniversal ),
			ButtonRequest.DisallowBackgroundApps => Task.Run( () => Tweaks.ToggleBackgroundApplications( WriteOutput ) ),
			ButtonRequest.AllowBackgroundApps    => Task.Run( () => Tweaks.ToggleBackgroundApplications( WriteOutput, false ) ),
			ButtonRequest.EnableClipboardHistory => Task.Run( Tweaks.EnableClipboardHistory ),
			ButtonRequest.EnableLocationTracking => Task.Run( Tweaks.EnableLocation ),
			ButtonRequest.EnableHibernation      => Task.Run( Tweaks.EnableHibernation ),
			_                                    => throw new ArgumentOutOfRangeException( nameof( request ), "Expected a tweak request." ),
		};

		// Basic method to write data to the output TextBlock.
		private void WriteOutput( string message ) {
			outputBuilder.AppendLine( message );

			this.RaisePropertyChanged( nameof( LogOutput ) );
		}
	}
}
