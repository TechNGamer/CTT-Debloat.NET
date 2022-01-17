using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CTTDebloatNET.Models;
using ReactiveUI;

// ReSharper disable once IdentifierTypo
namespace CTTDebloatNET.ViewModels {
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

	public class MainWindowViewModel : ViewModelBase {
		public bool IsProcessing {
			get => isProcessing;
			private set => this.RaiseAndSetIfChanged( ref isProcessing, value );
		}

		public string LogOutput => outputBuilder.ToString();

		public ICommand MainButtonCommand { get; }
		public ICommand InstallProgram    { get; }

		private bool isProcessing;

		private readonly StringBuilder outputBuilder;

		public MainWindowViewModel() {
			outputBuilder     = new StringBuilder();
			MainButtonCommand = ReactiveCommand.CreateFromTask( ( Func<ButtonRequest, Task> )ProcessButtonRequestHandler );
			InstallProgram    = ReactiveCommand.CreateFromTask( ( Func<ProgramInfo, Task> )InstallProgramHandler );
		}

		private async Task InstallProgramHandler( ProgramInfo info ) {
			IsProcessing = true;
			
			await ProgramHandler.InstallProgramAsync( WriteOutput, info );

			IsProcessing = false;
		}

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

		private Task TweakHandler( ButtonRequest request ) {
			// This method only handles tweaks.
			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			return request switch {
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
		}

		private void WriteOutput( string message ) {
			outputBuilder.AppendLine( message );

			this.RaisePropertyChanged( nameof( LogOutput ) );
		}
	}
}
