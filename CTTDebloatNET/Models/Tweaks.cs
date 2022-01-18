using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CTTDebloatNET.Models {
	/// <summary>
	/// This class tweaks windows in either massive ways or little ways.
	/// </summary>
	[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
	public static class Tweaks {
		// This is to help keep the registry all cleaned up.
		// ReSharper disable IdentifierTypo StringLiteralTypo CommentTypo
		#region Registry
		#region Current User
		private const string HKCU_CONTENT_DELIVERY_MANAGER_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
		private const string HKCU_FEEDBACK_LOCATION                 = @"SOFTWARE\Microsoft\Siuf\Rules";
		private const string HKCU_TAILORED_LOCATION                 = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
		private const string HKCU_STORAGE_SENSE                     = @"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy";
		private const string HKCU_FILE_OPERATIONS_LOCATION          = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager";
		private const string HKCU_ADVANCE_LOCATION                  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
		private const string HKCU_PEOPLE_ICON_LOCATION              = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People";
		private const string HKCU_EXPLORER_LOCATION                 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer";
		private const string HKCU_WINDOWS_FEEDBACK_LOCATION         = @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds";
		private const string HKCU_EXPLORER_POLICY_LOCATION          = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
		private const string HKCU_BACKGROUND_ACCESS_APPLICATIONS    = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";
		#endregion

		#region Local Machine
		private const string HKLM_CLOUD_CONTENT_POLICY_LOCATION  = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
		private const string HKLM_ACTIVITY_HISTORY_LOCATION      = @"SOFTWARE\Policies\Microsoft\Windows\System";
		private const string HKLM_MAP_UPDATE_LOCATION            = @"SYSTEM\Maps";
		private const string HKLM_FEEDBACK_POLICY                = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
		private const string HKLM_ADVERTISING_ID                 = @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo";
		private const string HKLM_HOTSPOT_LOCATION               = @"Software\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting";
		private const string HKLM_AUTO_CONNECT_LOCATION          = @"Software\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots";
		private const string HKLM_ERROR_REPORTING_LOCATION       = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";
		private const string HKLM_DELIVERY_OPTIMIZATION_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config";
		private const string HKLM_REMOTE_ASSISTANCE_LOCATION     = @"SYSTEM\CurrentControlSet\Control\Remote Assistance";
		private const string HKLM_3D_FOLDER_ICON_LOCATION        = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}";
		private const string HKLM_IRP_STACK_SIZE_LOCATION        = @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters";

		private static readonly string[] HKLM_TELEMETRY_LOCATIONS = {
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
			HKLM_FEEDBACK_POLICY
		};

		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		private static readonly string[] HKLM_LOCATION_TRACKING_LOCATIONS = {
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
			@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}",
			@"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration",
		};

		private static readonly string[] HKLM_HIBERNATION_LOCATIONS = {
			@"System\CurrentControlSet\Control\Session Manager\Power",
			@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings",
		};
		#endregion

		#region Users
		private const string HKU_KEYBOARD = @".DEFAULT\Control Panel\Keyboard";
		#endregion

		#region Keys
		private const uint   INIT_KEYBOARD_VALUE         = 2147483650;
		private const string TAILORED_KEY                = "DisableTailoredExperiencesWithDiagnosticData";
		private const string TELEMETRY_KEY               = "AllowTelemetry";
		private const string MAP_UPDATE_KEY              = "AutoUpdateEnabled";
		private const string ADVERTISING_ID_KEY          = "DisabledByGroupPolicy";
		private const string FILE_OPERATION_KEY          = "EnthusiastMode";
		private const string SHOW_TASK_VIEW_BUTTON_KEY   = "ShowTaskViewButton";
		private const string PEOPLE_ICON_KEY             = "PeopleBand";
		private const string AUTO_TRAY_KEY               = "EnableAutoTray";
		private const string INIT_KEYBOARD_KEY           = "InitialKeyboardIndicators";
		private const string LAUNCH_TO_KEY               = "LaunchTo";
		private const string IRP_STACK_KEY               = "IRPStackSize";
		private const string ENABLE_FEEDBACK_KEY         = "EnableFeeds";
		private const string FEEDS_TASKBAR_VIEW_MODE_KEY = "ShellFeedsTaskbarViewMode";
		private const string HIDE_FILE_EXT_KEY           = "HideFileExt";

		private static readonly string[] CLOUD_CONTENT_KEYS = {
			"ContentDeliveryAllowed",
			"OemPreInstalledAppsEnabled",
			"PreInstalledAppsEnabled",
			"PreInstalledAppsEverEnabled",
			"SilentInstalledAppsEnabled",
			"SubscribedContent-338387Enabled",
			"SubscribedContent-338388Enabled",
			"SubscribedContent-338389Enabled",
			"SubscribedContent-353698Enabled",
			"SystemPaneSuggestionsEnabled",
		};

		private static readonly string[] ACTIVE_HISTORY_KEYS = {
			"EnableActivityFeed",
			"PublishUserActivities",
			"UploadUserActivities",
		};
		#endregion
		#endregion

		#region Tasks
		private const string ERROR_REPORTING_TASK = @"Microsoft\Windows\Windows Error Reporting\QueueReporting";

		private static readonly string[] TELEMETRY_TASKS = {
			@"Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
			@"Microsoft\Windows\Application Experience\ProgramDataUpdater",
			@"Microsoft\Windows\Autochk\Proxy",
			@"Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
			@"Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
			@"Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
		};

		private static readonly string[] FEEDBACK_TASKS = {
			@"Microsoft\Windows\Feedback\Siuf\DmClient",
			@"Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
		};
		#endregion

		#region Services
		private const string DIAGNOSTIC_TRACKING_SERVICE = "DiagTrack";
		private const string WAP_PUSH_SERVICE            = "dmwappushservice";
		private const string HOME_GROUP_LISTENER_SERVICE = "HomeGroupListener";
		private const string HOME_GROUP_PROVIDER_SERVICE = "HomeGroupProvider";
		private const string SUPER_FETCH_SERVICE         = "SysMain";

		private static readonly string[] SERVICES_THAT_DONT_NEED_TO_RUN = {
			"diagnosticshub.standardcollector.service", // Microsoft (R) Diagnostics Hub Standard Collector Service
			"DiagTrack",                                // Diagnostics Tracking Service
			"dmwappushservice",                         // WAP Push Message Routing Service (see known issues)
			"lfsvc",                                    // Geolocation Service
			"MapsBroker",                               // Downloaded Maps Manager
			"NetTcpPortSharing",                        // Net.Tcp Port Sharing Service
			"RemoteAccess",                             // Routing and Remote Access
			"RemoteRegistry",                           // Remote Registry
			"SharedAccess",                             // Internet Connection Sharing (ICS)
			"TrkWks",                                   // Distributed Link Tracking Client
			//"WbioSrvc",								// Windows Biometric Service (required for Fingerprint reader / facial detection)
			//"WlanSvc",								// WLAN AutoConfig
			"WMPNetworkSvc", // Windows Media Player Network Sharing Service
			//"wscsvc",									// Windows Security Center Service
			"WSearch",        // Windows Search
			"XblAuthManager", // Xbox Live Auth Manager
			"XblGameSave",    // Xbox Live Game Save Service
			"XboxNetApiSvc",  // Xbox Live Networking Service
			"XboxGipSvc",     // Disables Xbox Accessory Management Service
			"ndu",            // Windows Network Data Usage Monitor
			"WerSvc",         // Disables windows error reporting
			//"Spooler",								// Disables your printer
			"Fax",         // Disables fax
			"fhsvc",       // Disables fax history
			"gupdate",     // Disables google update
			"gupdatem",    // Disable another google update
			"stisvc",      // Disables Windows Image Acquisition (WIA)
			"AJRouter",    // Disables (needed for AllJoyn Router Service)
			"MSDTC",       // Disables Distributed Transaction Coordinator
			"WpcMonSvc",   // Disables Parental Controls
			"PhoneSvc",    // Disables Phone Service(Manages the telephony state on the device)
			"PrintNotify", // Disables Windows printer notifications and extentions
			"PcaSvc",      // Disables Program Compatibility Assistant Service
			"WPDBusEnum",  // Disables Portable Device Enumerator Service
			//"LicenseManager",							// Disable LicenseManager (Windows store may not work properly)
			"seclogon",   // Disables  Secondary Logon(disables other credentials only password will work)
			"SysMain",    // Disables sysmain
			"lmhosts",    // Disables TCP/IP NetBIOS Helper
			"wisvc",      // Disables Windows Insider program(Windows Insider will not work)
			"FontCache",  // Disables Windows font cache
			"RetailDemo", // Disables RetailDemo whic is often used when showing your device
			"ALG",        // Disables Application Layer Gateway Service(Provides support for 3rd party protocol plug-ins for Internet Connection Sharing)
			//"BFE",									// Disables Base Filtering Engine (BFE) (is a service that manages firewall and Internet Protocol security)
			//"BrokerInfrastructure",					// Disables Windows infrastructure service that controls which background tasks can run on the system.
			"SCardSvr",    // Disables Windows smart card
			"EntAppSvc",   // Disables enterprise application management.
			"BthAvctpSvc", // Disables AVCTP service (if you use  Bluetooth Audio Device or Wireless Headphones. then don't disable this)
			//"FrameServer",							// Disables Windows Camera Frame Server(this allows multiple clients to access video frames from camera devices.)
			"Browser",     // Disables computer browser
			"BthAvctpSvc", // AVCTP service (This is Audio Video Control Transport Protocol service.)
			//"BDESVC",									// Disables bitlocker
			//"iphlpsvc",								// Disables ipv6 but most websites don't use ipv6 they use ipv4     
			"edgeupdate",                    // Disables one of edge update service  
			"MicrosoftEdgeElevationService", // Disables one of edge service 
			"edgeupdatem",                   // Disables another one of update service (disables edgeupdatem)                          
			"SEMgrSvc",                      // Disables Payments and NFC/SE Manager (Manages payments and Near Field Communication (NFC) based secure elements)
			//"PNRPsvc",								// Disables peer Name Resolution Protocol ( some peer-to-peer and collaborative applications, such as Remote Assistance, may not function, Discord will still work)
			//"p2psvc",									// Disables Peer Name Resolution Protocol(nables multi-party communication using Peer-to-Peer Grouping.  If disabled, some applications, such as HomeGroup, may not function. Discord will still work)
			//"p2pimsvc",								// Disables Peer Networking Identity Manager (Peer-to-Peer Grouping services may not function, and some applications, such as HomeGroup and Remote Assistance, may not function correctly.Discord will still work)
			"PerfHost",                    // Disables remote users and 64-bit processes to query performance .
			"BcastDVRUserService_48486de", // Disables GameDVR and Broadcast is used for Game Recordings and Live Broadcasts
			"CaptureService_48486de",      // Disables potential screen capture functionality for applications that call the Windows.Graphics.Capture API.  
			"cbdhsvc_48486de",             // Disables cbdhsvc_48486de (clipboard service it disables)
			//"BluetoothUserService_48486de",			// Disables BluetoothUserService_48486de (The Bluetooth user service supports proper functionality of Bluetooth features relevant to each user session.)
			"WpnService", // Disables WpnService (Push Notifications may not work )
			//"StorSvc",								// Disables StorSvc (usb external hard drive will not be reconised by windows)
			"RtkBtManServ", // Disables Realtek Bluetooth Device Manager Service
			"QWAVE",        // Disables Quality Windows Audio Video Experience (audio and video might sound worse)
			"DPS",
			//Hp services
			"HPAppHelperCap",
			"HPDiagsCap",
			"HPNetworkCap",
			"HPSysInfoCap",
			"HpTouchpointAnalyticsService",
			//hyper-v services
			"HvHost",
			"vmickvpexchange",
			"vmicguestinterface",
			"vmicshutdown",
			"vmicheartbeat",
			"vmicvmsession",
			"vmicrdv",
			"vmictimesync",
			// Services which cannot be disabled
			//"WdNisSvc",
		};
		#endregion

		#region AutoLogger
		private static readonly string AUTO_LOGGER_DIRECTORY = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ), "Microsoft", "Diagnosis", "ETLLogs", "AutoLogger" );
		private static readonly string AUTO_LOGGER_FILE      = Path.Combine( AUTO_LOGGER_DIRECTORY, "AutoLogger-Diagtrack-Listener.etl" );
		#endregion

		private static readonly string[] BLOATWARE_APPS = {
			// Unnecessary Windows 10 AppX Apps
			"Microsoft.3DBuilder",
			"Microsoft.Microsoft3DViewer",
			"Microsoft.AppConnector",
			"Microsoft.BingFinance",
			"Microsoft.BingNews",
			"Microsoft.BingSports",
			"Microsoft.BingTranslator",
			"Microsoft.BingWeather",
			"Microsoft.BingFoodAndDrink",
			"Microsoft.BingHealthAndFitness",
			"Microsoft.BingTravel",
			"Microsoft.MinecraftUWP",
			"Microsoft.GamingServices",
			// "Microsoft.WindowsReadingList",
			"Microsoft.GetHelp",
			"Microsoft.Getstarted",
			"Microsoft.Messaging",
			"Microsoft.Microsoft3DViewer",
			"Microsoft.MicrosoftSolitaireCollection",
			"Microsoft.NetworkSpeedTest",
			"Microsoft.News",
			"Microsoft.Office.Lens",
			"Microsoft.Office.Sway",
			"Microsoft.Office.OneNote",
			"Microsoft.OneConnect",
			"Microsoft.People",
			"Microsoft.Print3D",
			"Microsoft.SkypeApp",
			"Microsoft.Wallet",
			"Microsoft.Whiteboard",
			"Microsoft.WindowsAlarms",
			"microsoft.windowscommunicationsapps",
			"Microsoft.WindowsFeedbackHub",
			"Microsoft.WindowsMaps",
			"Microsoft.WindowsPhone",
			"Microsoft.WindowsSoundRecorder",
			"Microsoft.XboxApp",
			"Microsoft.ConnectivityStore",
			"Microsoft.CommsPhone",
			"Microsoft.ScreenSketch",
			"Microsoft.Xbox.TCUI",
			"Microsoft.XboxGameOverlay",
			"Microsoft.XboxGameCallableUI",
			"Microsoft.XboxSpeechToTextOverlay",
			"Microsoft.MixedReality.Portal",
			"Microsoft.XboxIdentityProvider",
			"Microsoft.ZuneMusic",
			"Microsoft.ZuneVideo",
			"Microsoft.YourPhone",
			"Microsoft.Getstarted",
			"Microsoft.MicrosoftOfficeHub",
			// Sponsored Windows 10 AppX Apps,
			// Add sponsored/featured apps to remove in the "*AppName*" format,
			"*EclipseManager*",
			"*ActiproSoftwareLLC*",
			"*AdobeSystemsIncorporated.AdobePhotoshopExpress*",
			"*Duolingo-LearnLanguagesforFree*",
			"*PandoraMediaInc*",
			"*CandyCrush*",
			"*BubbleWitch3Saga*",
			"*Wunderlist*",
			"*Flipboard*",
			"*Twitter*",
			"*Facebook*",
			"*Royal Revolt*",
			"*Sway*",
			"*Speed Test*",
			"*Dolby*",
			"*Viber*",
			"*ACGMediaPlayer*",
			"*Netflix*",
			"*OneCalendar*",
			"*LinkedInforWindows*",
			"*HiddenCityMysteryofShadows*",
			"*Hulu*",
			"*HiddenCity*",
			"*AdobePhotoshopExpress*",
			"*HotspotShieldFreeVPN*",
			// Optional: Typically not removed but you can if you need to for some reason,
			"*Microsoft.Advertising.Xaml*",
			// "*Microsoft.MSPaint*",
			// "*Microsoft.MicrosoftStickyNotes*",
			// "*Microsoft.Windows.Photos*",
			// "*Microsoft.WindowsCalculator*",
			// "*Microsoft.WindowsStore*",
		};
		// ReSharper restore IdentifierTypo StringLiteralTypo CommentTypo

		static Tweaks() {
			// Makes sure that it is running only on Windows.
			if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
				throw new PlatformNotSupportedException( "This program only works on Windows." );
			}
		}


		/// <summary>
		/// Changes a lot of Windows properties to make it more performant.
		/// To undo, simply call <see cref="UndoEssentialTweaks"/>.
		/// </summary>
		/// <seealso cref="UndoEssentialTweaks"/>
		/// <param name="output">A method to output messages to.</param>
		/// <exception cref="ArgumentNullException">When <paramref name="output"/> is null.</exception>
		[SuppressMessage( "ReSharper", "AsyncVoidLambda" )]
		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		[SuppressMessage( "ReSharper", "IdentifierTypo" )]
		[SuppressMessage( "ReSharper", "CommentTypo" )]
		public static async Task EssentialTweaks( Action<string> output ) {
			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Expected a method to report back to." );
			}

			// Creates a Restory Point.
			output( "Creating a restore point." );

			await Task.Run( () => RestorePointHandler.CreateSystemRestorePoint( "Essential Tweaks" ) );

			// O&O Shutup
			output( "Running O&O Shut Up 10." );

			await ShutUpWindows();

			// Telemetry
			output( $"Disabling Telemetry.\nChanging `HKLM:\\{HKLM_TELEMETRY_LOCATIONS[0]}` and `HKLM:\\{HKLM_TELEMETRY_LOCATIONS[1]}` `AllowTelemetry` to 0" );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_TELEMETRY_LOCATIONS[0], TELEMETRY_KEY );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_TELEMETRY_LOCATIONS[1], TELEMETRY_KEY );

			foreach ( var task in TELEMETRY_TASKS ) {
				output( $"Disabling Telemetry task `{task}`." );
				
				await DisableSchedule( task );
			}

			// WiFi Sense
			output( "Disabling Wi-Fi Sense" );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HOTSPOT_LOCATION, "Value" );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_AUTO_CONNECT_LOCATION, "Value" );

			// Application Suggestions
			output( "Disabling Application Suggestions" );

			foreach ( var regKey in CLOUD_CONTENT_KEYS ) {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_CONTENT_DELIVERY_MANAGER_LOCATION, regKey );
			}

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_CLOUD_CONTENT_POLICY_LOCATION, "DisableWindowsConsumerFeatures", 1 );

			// Active History
			output( "Disabling Active History" );

			foreach ( var regKey in ACTIVE_HISTORY_KEYS ) {
				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ACTIVITY_HISTORY_LOCATION, regKey );
			}

			// Location Tracking
			output( "Disabling Location Tracking." );

			DisableLocationTracking();

			// Map Updates
			output( "Disabling Automatic Maps updates." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKLM_MAP_UPDATE_LOCATION, MAP_UPDATE_KEY );

			// Feedback
			output( "Disabling Feedback." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_FEEDBACK_LOCATION, "NumberOfSIUFInPeriod" );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_FEEDBACK_POLICY, "DoNotShowFeedbackNotification", 1 );

			foreach ( var task in FEEDBACK_TASKS ) {
				await DisableSchedule( task );
			}

			// Tailored Experience
			output( "Disabling Tailored Experience." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_TAILORED_LOCATION, TAILORED_KEY, 1 );

			// Advertising ID
			output( "Disabling Advertising ID." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ADVERTISING_ID, ADVERTISING_ID_KEY, 1 );

			// Error Reporting
			output( "Disabling Error Reporting." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ERROR_REPORTING_LOCATION, "Disabled", 1 );

			await DisableSchedule( ERROR_REPORTING_TASK );

			// Windows Update
			output( "Restricting Windows to local only." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_DELIVERY_OPTIMIZATION_LOCATION, "DODownloadMode", 1 );

			// Diagnostics
			output( "Disabling Diagnostic Tracking Services." );

			await ToggleService( DIAGNOSTIC_TRACKING_SERVICE );

			// WAP
			output( "Killing WAP Push Service" );

			await ToggleService( WAP_PUSH_SERVICE );

			// F8 Boot Menu Option
			output( "Enabling F8 Boot Menu Option." );

			await ProgramHandler.StartProgram( "bcdedit", "/set \"{current}\" bootmenupolicy legacy" );

			// Home Groups
			output( "Disabling Home Groups." );

			await ToggleService( HOME_GROUP_LISTENER_SERVICE );
			await ToggleService( HOME_GROUP_PROVIDER_SERVICE );

			// Remote Assistance
			output( "Disabling Remote Assistance." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_REMOTE_ASSISTANCE_LOCATION, "fAllowToGetHelp" );

			// Storage Sense
			output( "Disabling Storage Sense." );

			try {
				Registry.LocalMachine.DeleteSubKeyTree( HKCU_STORAGE_SENSE );
			} catch ( ArgumentException ) {
				output( "Looks like Storage Sense is already disabled." );
			}

			// Superfetch
			output( "Disabling Superfetch." );

			await ToggleService( SUPER_FETCH_SERVICE );

			// Hibernation
			output( "Disabling Hibernation." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[0], "HibernationEnabled" );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[1], "ShowHibernateOption" );

			// File Operation Details
			output( "Showing File Operations Details." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_FILE_OPERATIONS_LOCATION, FILE_OPERATION_KEY, 1 );

			// Task View
			output( "Hiding Task View." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, SHOW_TASK_VIEW_BUTTON_KEY );

			// People Icon
			output( "Hiding People Icon." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_PEOPLE_ICON_LOCATION, PEOPLE_ICON_KEY );

			// Tray Icons
			output( "Hiding Tray Icons." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, AUTO_TRAY_KEY );

			// Numlock
			output( "Enabling NumLock after startup." );

			try {
				Utilities.ChangeDWordRegistryKey( Registry.Users, HKU_KEYBOARD, INIT_KEYBOARD_KEY, INIT_KEYBOARD_VALUE );
			} catch ( ArgumentException ) {
			}

			// Default Explorer Location
			output( "Changing default Explorer view to This PC." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, LAUNCH_TO_KEY, 1 );

			// 3D Objects
			output( "Hiding 3D Objects icon from This PC." );

			try {
				Registry.LocalMachine.DeleteSubKeyTree( HKLM_3D_FOLDER_ICON_LOCATION );
			} catch ( ArgumentException ) {
			}

			// IRP Stack Size
			output( "Changing IRP Stack Size." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_IRP_STACK_SIZE_LOCATION, IRP_STACK_KEY, 20 );

			// Service Limit
			output( "Grouping svhost.exe processes." );

			GroupServiceHostProcesses();

			// News and Interests
			output( "Disabling News and Interests" );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_WINDOWS_FEEDBACK_LOCATION, ENABLE_FEEDBACK_KEY );
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_WINDOWS_FEEDBACK_LOCATION, FEEDS_TASKBAR_VIEW_MODE_KEY, 2 );

			// Meet Now
			output( "Removing the \"Meet Now\" button." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_POLICY_LOCATION, "HideSCAMeetNow", 1 );

			// AutoLogger
			output( "Removing AutoLogger file and restricting directories." );

			if ( File.Exists( AUTO_LOGGER_FILE ) ) {
				File.Delete( AUTO_LOGGER_FILE );
			}

			await ProgramHandler.StartProgram( "icacls", $"\"{AUTO_LOGGER_DIRECTORY}\" /deny SYSTEM:`(OI`)(CI`)F" );

			// Diagnostic Tracking
			output( "Stopping and disabling Diagnostics Tracking Service." );

			await ToggleService( DIAGNOSTIC_TRACKING_SERVICE );

			// File Extensions
			output( "Showing known file extensions." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, HIDE_FILE_EXT_KEY );

			// Services to Manual
			output( "Setting some services to manual." );

			SetServicesToManual();

			async Task ShutUpWindows() {
				const string SHUT_UP_10     = "https://dl5.oo-software.com/files/ooshutup10/OOSU10.exe";
				const string SHUT_UP_10_CFG = "ooshutup10.cfg";
				var          shutUp10       = await Utilities.DownloadFile( SHUT_UP_10 );
				var          cfgFile        = await Utilities.WriteResourceToFile( SHUT_UP_10_CFG );

				await ProgramHandler.StartProgram( shutUp10, $"\"{cfgFile}\" /quiet" );
			}

			void DisableLocationTracking() {
				var regKey = Registry.LocalMachine.CreateSubKey( HKLM_LOCATION_TRACKING_LOCATIONS[0] );

				regKey!.SetValue( "Value", "Deny", RegistryValueKind.String );

				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_LOCATION_TRACKING_LOCATIONS[1], "SensorPermissionState" );
				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_LOCATION_TRACKING_LOCATIONS[2], "Status" );
			}

			void GroupServiceHostProcesses() {
				var ram = 0L;
				
				

				foreach ( var svhost in Process.GetProcessesByName( "svchost" ) ) {
					ram += svhost.PeakWorkingSet64;
				}

				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control", "SccHostSplitThresholdInKB", ( uint )( ram / 1024 ) );
			}

			void SetServicesToManual() {
				var tasks      = new Task[Environment.ProcessorCount];
				var smallTasks = SERVICES_THAT_DONT_NEED_TO_RUN.Length / tasks.Length;

				for ( var i = 0; i < tasks.Length; ++i ) {
					var subServices = new string[smallTasks];

					Array.Copy( SERVICES_THAT_DONT_NEED_TO_RUN, smallTasks * i, subServices, 0, smallTasks );

					tasks[i] = Task.Run( async () => {
						foreach ( var service in subServices ) {
							lock ( tasks ) {
								output( $"Setting `{service}` to Manual." );
							}

							await SwitchServiceToManual( service );
						}
					} );
				}

				Task.WaitAll( tasks );
			}
		}

		/// <summary>
		/// Undoes most everything that the <see cref="EssentialTweaks"/> did.
		/// </summary>
		/// <seealso cref="EssentialTweaks"/>
		/// <param name="output">A method to output messages to.</param>
		/// <exception cref="ArgumentNullException">When <paramref name="output"/> is null.</exception>
		[SuppressMessage( "ReSharper", "AsyncVoidLambda" )]
		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		[SuppressMessage( "ReSharper", "IdentifierTypo" )]
		[SuppressMessage( "ReSharper", "CommentTypo" )]
		public static async Task UndoEssentialTweaks( Action<string> output ) {
			if ( output == null ) {
				throw new ArgumentNullException( nameof( output ), "Expected a method to report back to." );
			}

			output( "Creating a restore point." );

			RestorePointHandler.CreateSystemRestorePoint( "Undoing Essential Tweaks" );

			#region Telemetry
			output( "Enabling Telemetry." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_TELEMETRY_LOCATIONS[0], TELEMETRY_KEY, 1 );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_TELEMETRY_LOCATIONS[1], TELEMETRY_KEY, 1 );

			foreach ( var task in TELEMETRY_TASKS ) {
				await DisableSchedule( task );
			}
			#endregion

			#region WiFi Sense
			output( "Enabling Wi-Fi Sense" );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HOTSPOT_LOCATION, "Value", 1 );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_AUTO_CONNECT_LOCATION, "Value", 1 );
			#endregion

			#region Application Suggestions
			output( "Enabling Application Suggestions" );

			foreach ( var regKey in CLOUD_CONTENT_KEYS ) {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_CONTENT_DELIVERY_MANAGER_LOCATION, regKey );
			}

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_CLOUD_CONTENT_POLICY_LOCATION, "DisableWindowsConsumerFeatures" );
			#endregion

			#region Active History
			output( "Enabling Active History" );

			foreach ( var regKey in ACTIVE_HISTORY_KEYS ) {
				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ACTIVITY_HISTORY_LOCATION, regKey, 1 );
			}
			#endregion

			#region Location
			output( "Enabling Location Tracking." );

			EnableLocationTracking();
			#endregion

			#region Maps
			output( "Enabling Automatic Maps updates." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKLM_MAP_UPDATE_LOCATION, MAP_UPDATE_KEY, 1 );
			#endregion

			#region Disabling Feedback
			output( "Enabling Feedback." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_FEEDBACK_LOCATION, "NumberOfSIUFInPeriod", 1 );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_FEEDBACK_POLICY, "DoNotShowFeedbackNotification" );
			#endregion

			#region Tailored Experiences
			output( "Enabling Tailored Experience." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKLM_CLOUD_CONTENT_POLICY_LOCATION, TAILORED_KEY );
			#endregion

			#region Ad ID
			output( "Enabling Advertising ID." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ADVERTISING_ID, ADVERTISING_ID_KEY );
			#endregion

			#region Error Reporting
			output( "Enabling Error Reporting." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_ERROR_REPORTING_LOCATION, "Disabled" );
			#endregion

			#region Diagnostics
			output( "Enabling Diagnostic Tracking Services." );

			await ToggleService( DIAGNOSTIC_TRACKING_SERVICE, true );
			#endregion

			#region WAP Push Service
			output( "Enabling WAP Push Service" );

			await ToggleService( WAP_PUSH_SERVICE, true );
			#endregion

			#region Home Group
			output( "Enabling Home Groups." );

			await ToggleService( HOME_GROUP_LISTENER_SERVICE, true );
			await ToggleService( HOME_GROUP_PROVIDER_SERVICE, true );
			#endregion

			#region Remote Assistance
			output( "Enabling Remote Assistance." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_REMOTE_ASSISTANCE_LOCATION, "fAllowToGetHelp", 1 );
			#endregion

			#region Storage Sense
			output( "Enabling Storage Sense." );

			// Registry.LocalMachine.DeleteSubKeyTree( HKCU_STORAGE_SENSE );
			Registry.LocalMachine.CreateSubKey( HKCU_STORAGE_SENSE )!.Dispose();
			#endregion

			#region Superfetch
			output( "Enabling Superfetch." );

			await ToggleService( SUPER_FETCH_SERVICE, true );
			#endregion

			#region Hibernation
			output( "Enabling Hibernation." );

			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[0], "HibernationEnabled", 1 );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[1], "ShowHibernateOption", 1 );
			#endregion

			#region File Operation Details
			output( "Showing File Operations Details." );

			Registry.CurrentUser.DeleteSubKeyTree( HKCU_FILE_OPERATIONS_LOCATION );
			#endregion

			#region Task View
			output( "Showing Task View Button." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, SHOW_TASK_VIEW_BUTTON_KEY, 1 );
			#endregion

			#region Peoples
			output( "Showing People Icon." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_PEOPLE_ICON_LOCATION, PEOPLE_ICON_KEY, 1 );
			#endregion

			#region Tray Icons
			output( "Showing Tray Icons." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, AUTO_TRAY_KEY, 1 );
			#endregion

			#region Default Explorer Location
			output( "Changing default Explorer view to This PC." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, LAUNCH_TO_KEY );
			#endregion

			#region 3D Objects
			output( "Showing 3D Objects icon from This PC." );

			Registry.LocalMachine.CreateSubKey( HKLM_3D_FOLDER_ICON_LOCATION )!.Dispose();
			#endregion

			#region AutoLogger
			output( "Restoring AutoLogger" );

			await ProgramHandler.StartProgram( "icacls", $"\"{AUTO_LOGGER_DIRECTORY}\" /grant:r SYSTEM:`(OI`)(CI`)F" );
			#endregion

			#region Diagnostics Tracking
			output( "Stopping and disabling Diagnostics Tracking Service." );

			await ToggleService( DIAGNOSTIC_TRACKING_SERVICE, true );
			#endregion

			#region File Extensions
			output( "Showing known file extensions." );

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, HIDE_FILE_EXT_KEY, 1 );
			#endregion

			output( "Resetting Group Policies." );

			Directory.Delete( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "System32", "GroupPolicyUsers" ) );
			Directory.Delete( Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "System32", "GroupPolicy" ) );

			await ProgramHandler.StartProgram( "gpupdate.exe", "/force" );

			void EnableLocationTracking() {
				var regKey = Registry.LocalMachine.CreateSubKey( HKLM_LOCATION_TRACKING_LOCATIONS[0] );

				regKey!.SetValue( "Value", "Allow", RegistryValueKind.String );

				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_LOCATION_TRACKING_LOCATIONS[1], "SensorPermissionState", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_LOCATION_TRACKING_LOCATIONS[2], "Status", 1 );
			}
		}

		/// <summary>
		/// Tells Windows to use Universal Coordinated Time.
		/// </summary>
		public static void SetTimeToUniversal() => Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 1 );

		/// <summary>
		/// Toggles background apps.
		/// </summary>
		/// <param name="output">A method to write log data to.</param>
		/// <param name="disable">Whether to enable or disable the background apps.</param>
		public static void ToggleBackgroundApplications( Action<string> output, bool disable = true ) {
			var rootKey = Registry.CurrentUser.CreateSubKey( HKCU_BACKGROUND_ACCESS_APPLICATIONS );

			foreach ( var subKeyName in rootKey!.GetSubKeyNames().Where( name => !name.StartsWith( "Microsoft.Windows.Cortana" ) ) ) {
				var toggleValue = disable ? 1u : 0u;

				output( $"{( disable ? "Disabling" : "Enabling" )} `{subKeyName}`." );

				Utilities.ChangeDWordRegistryKey( rootKey, subKeyName, "Disabled", toggleValue );
				Utilities.ChangeDWordRegistryKey( rootKey, subKeyName, "DisabledByUser", toggleValue );
			}
		}

		/// <summary>
		/// Toggles Windows Search (Cortana) on or off.
		/// </summary>
		/// <param name="disable">Whether to enable or disable Cortana.</param>
		public static void ToggleCortana( bool disable = true ) {
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, @"SOFTWARE\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", disable ? 0u : 1u );
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, @"SOFTWARE\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", disable ? 1u : 0u );
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, @"SOFTWARE\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", disable ? 1u : 0u );
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, @"SOFTWARE\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", disable ? 0u : 1u );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", disable ? 0u : 1u );
		}

		#region Bloatware
		/// <summary>
		/// Removes all of the bloatware from the computer.
		/// </summary>
		/// <returns>A task that is waiting for the removal of all bloatware.</returns>
		public static Task PurgeBloatware() {
			var tasks       = new Task[Environment.ProcessorCount];
			var splitArrays = Utilities.SplitArrayForMultipleThreads( BLOATWARE_APPS );

			for ( var i = 0; i < tasks.Length; ++i ) {
				var subArr = splitArrays[i];

				tasks[i] = Task.Run( async () => {
					const string APPX_REMOVE_COMMAND      = "Get-AppxPackage -Name {0} | Remove-AppxPackage";
					const string APPX_PROVISIONED_COMMAND = "Get-AppxProvisionedPackage -Online | Where-Object DisplayName -like {0} | Remove-AppxProvisionedPackage -Online";

					foreach ( var appx in subArr ) {
						var command0 = string.Format( APPX_REMOVE_COMMAND, appx );
						var command1 = string.Format( APPX_PROVISIONED_COMMAND, appx );

						await ProgramHandler.StartProgram( "powershell.exe", $"-Command {{{command0}}}" );
						await ProgramHandler.StartProgram( "powershell.exe", $"-Command {{{command1}}}" );
					}
				} );
			}

			return Task.Run( () => Task.WaitAll( tasks ) );
		}

		/// <summary>
		/// Installs the bloatware back onto the computer.
		/// </summary>
		/// <returns>A task that waits for the programs to install.</returns>
		public static Task ReinstallBloatware() {
			var tasks       = new Task[Environment.ProcessorCount];
			var splitArrays = Utilities.SplitArrayForMultipleThreads( BLOATWARE_APPS );

			for ( var i = 0; i < tasks.Length; ++i ) {
				var subArr = splitArrays[i];

				tasks[i] = Task.Run( async () => {
					const string APPX_INSTALL_COMMAND = "Add-AppxPackage -DisableDevelopmentMode -Register \"$($(Get-AppxPackage -AllUsers {0}).InstallLocation)\\AppXManifest.xml\"";

					foreach ( var package in subArr ) {
						var command = string.Format( APPX_INSTALL_COMMAND, package );

						await ProgramHandler.StartProgram( "powershell.exe", $"-Command {{{command}}}" );
					}
				} );
			}

			return Task.Run( () => Task.WaitAll( tasks ) );
		}
		#endregion

		/// <summary>
		/// Toggles the enabling or disabling of the action center.
		/// </summary>
		/// <param name="enable">Whether to enable it or disable it.</param>
		public static void ToggleActionCenter( bool enable ) {
			const string PUSH_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications";

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, "DisableNotificationCenter", enable ? 0u : 1u );
			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, PUSH_LOCATION, "ToastEnabled", enable ? 1u : 0u );
		}

		/// <summary>
		/// Used to select either the performance visuals or the fancy visuals.
		/// </summary>
		/// <param name="usePerformance">To use the performance over the fancy visual.</param>
		/// <returns>A background task that will change the visuals.</returns>
		public static Task ToggleVisualEffects( bool usePerformance ) {
			const string DESKTOP                = @"Control Panel\Desktop";
			const string WINDOW_METRIC          = @"Control Panel\Desktop\WindowMetric";
			const string KEYBOARD               = @"Control Panel\Keyboard";
			const string EXPLORER_VFX           = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
			const string DESKTOP_WINDOW_MANAGER = @"Software\Microsoft\Windows\DWM";

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, EXPLORER_VFX, "VisualFXSetting", 3 );

			return Task.Run( () => {
				if ( usePerformance ) {
					Performance();
				} else {
					Appearance();
				}
			} );

			void Performance() {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP, "DragFullWindows" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP, "MenuShowDelay", 200 );

				// ReSharper disable once ConvertToUsingDeclaration
				using ( var desktopReg = Registry.CurrentUser.CreateSubKey( DESKTOP ) ) {
					desktopReg?.SetValue( "UserPrefencesMask", new byte[] { 144, 18, 3, 128, 16, 0, 0, 0 }, RegistryValueKind.Binary );
				}

				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, WINDOW_METRIC, "MinAnimate" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, KEYBOARD, "KeyboardDelay" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "ListviewAlphaSelect" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "ListviewShadow" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "TaskbarAnimations" );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP_WINDOW_MANAGER, "EnableAeroPeek" );
			}

			void Appearance() {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP, "DragFullWindows", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP, "MenuShowDelay", 400 );

				// ReSharper disable once ConvertToUsingDeclaration
				using ( var desktopReg = Registry.CurrentUser.CreateSubKey( DESKTOP ) ) {
					desktopReg?.SetValue( "UserPrefencesMask", new byte[] { 158, 30, 7, 128, 18, 0, 0, 0 }, RegistryValueKind.Binary );
				}

				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, WINDOW_METRIC, "MinAnimate", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, KEYBOARD, "KeyboardDelay", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "ListviewAlphaSelect", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "ListviewShadow", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_ADVANCE_LOCATION, "TaskbarAnimations", 1 );
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, DESKTOP_WINDOW_MANAGER, "EnableAeroPeek", 1 );
			}
		}

		/// <summary>
		/// Will either remove or install OneDrive to the computer.
		/// </summary>
		/// <param name="enabled">Whether to install or remove OneDrive.</param>
		/// <returns>A background task that will either remove or add OneDrive.</returns>
		[SuppressMessage( "ReSharper", "IdentifierTypo" )]
		[SuppressMessage( "ReSharper", "StringLiteralTypo" )]
		public static Task ToggleOneDrive( bool enabled ) {
			const string REG_ONEDRIVE_LOC    = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive";
			const string ONEDRIVE_ID         = @"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}";
			const string ONEDRIVE_SYSWOW_ID  = @"Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}";
			var          windowsOneDrive     = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "SysWOW64", "OneDriveSetup.exe" );
			var          userOneDrive        = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), "OneDrive" );
			var          localAppOneDrive    = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Microsoft", "OneDrive" );
			var          oneDriveTemp        = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.System ), "OneDriveTemp" );
			var          programDataOneDrive = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData ), "Microsoft OneDrive" );

			if ( enabled ) {
				Registry.LocalMachine.CreateSubKey( REG_ONEDRIVE_LOC )?.DeleteValue( "DisableFileSyncNGSC" );

				using var oneDriveInstaller = new Process() {
					StartInfo = {
						FileName = windowsOneDrive,
					}
				};

				return Task.CompletedTask;
			} else {
				return Disable();
			}

			async Task Disable() {
				Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, REG_ONEDRIVE_LOC, "DisableFileSyncNGSC", 1 );

				foreach ( var onedrive in Process.GetProcessesByName( "onedrive" ) ) {
					onedrive.Kill();
				}

				if ( File.Exists( windowsOneDrive ) ) {
					using var oneDriveUninstallProc = new Process() {
						StartInfo = {
							FileName  = windowsOneDrive,
							Arguments = "/uninstall"
						}
					};

					oneDriveUninstallProc.Start();

					await oneDriveUninstallProc.WaitForExitAsync();
				}

				foreach ( var explorer in Process.GetProcessesByName( "explorer" ) ) {
					explorer.Kill();
				}

				if ( Directory.Exists( userOneDrive ) ) {
					Directory.Delete( userOneDrive, true );
				}

				if ( Directory.Exists( localAppOneDrive ) ) {
					Directory.Delete( localAppOneDrive, true );
				}

				if ( Directory.Exists( oneDriveTemp ) ) {
					Directory.Delete( oneDriveTemp, true );
				}

				if ( Directory.Exists( programDataOneDrive ) ) {
					Directory.Delete( programDataOneDrive );
				}

				Registry.ClassesRoot.DeleteSubKeyTree( ONEDRIVE_ID );
				Registry.ClassesRoot.DeleteSubKeyTree( ONEDRIVE_SYSWOW_ID );
			}
		}

		/// <summary>
		/// Used to switch between Dark Mode or Light Mode.
		/// </summary>
		/// <param name="darkTheme">Whether to use Dark Mode or Light Mode.</param>
		public static void ToggleThemeMode( bool darkTheme ) {
			const string THEME_REG_LOCATION = @"SOFTWARE\Microsoft\CurrentVersion\Themes\Personalize";
			const string KEY                = "AppsUseLightTheme";

			if ( darkTheme ) {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, THEME_REG_LOCATION, KEY, 1 );
			} else {
				Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, THEME_REG_LOCATION, KEY );
			}
		}

		/// <summary>
		/// Used to either show all tray icons or none at all.
		/// </summary>
		/// <param name="hide">Whether to show or hide tray icons.</param>
		public static void ToggleTrayIcons( bool hide ) {
			const string TRAY_KEY = "EnableAutoTray";

			Utilities.ChangeDWordRegistryKey( Registry.CurrentUser, HKCU_EXPLORER_LOCATION, TRAY_KEY, hide ? 1u : 0u );
		}

		/// <summary>
		/// Used to enable Clipboard History.
		/// </summary>
		public static void EnableClipboardHistory() {
			using var msClipboard     = Registry.CurrentUser.CreateSubKey( @"SOFTWARE\Microsfot\Clipboard" );
			using var systemClipboard = Registry.LocalMachine.CreateSubKey( @"SOFTWARE\Policies\Microsoft\Windows\System" );

			msClipboard?.DeleteValue( "EnableClipboardHistory" );
			systemClipboard?.DeleteValue( "AllowClipboardHistory" );
		}

		/// <summary>
		/// Used to re-enable location tracking.
		/// </summary>
		[SuppressMessage( "ReSharper", "IdentifierTypo" )]
		[SuppressMessage( "ReSharper", "CommentTypo" )]
		public static void EnableLocation() {
			using var locAndSensReg   = Registry.LocalMachine.CreateSubKey( @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" );
			using var sensorDriver    = Registry.LocalMachine.CreateSubKey( HKLM_LOCATION_TRACKING_LOCATIONS[1] );
			using var deviceAccess    = Registry.CurrentUser.CreateSubKey( @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeviceAccess\Global\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}" );
			using var consentLocation = Registry.LocalMachine.CreateSubKey( HKLM_LOCATION_TRACKING_LOCATIONS[0] );
			using var appPrivacy      = Registry.LocalMachine.CreateSubKey( @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" );
			using var lfsvcService    = Registry.LocalMachine.CreateSubKey( HKLM_LOCATION_TRACKING_LOCATIONS[2] );

			// Location and Sensor
			locAndSensReg?.DeleteValue( "DisableWindowsLocationProvider" );
			locAndSensReg?.DeleteValue( "DisableLocationScripting" );
			locAndSensReg?.DeleteValue( "DisableLocation" );

			// Sensor Driver
			sensorDriver?.DeleteValue( "SensorPermissionState" );

			// Device Access
			deviceAccess?.SetValue( "Value", "Allow", RegistryValueKind.String );

			// Consent Location
			consentLocation?.SetValue( "Value", "Allow", RegistryValueKind.String );

			// Privacy
			appPrivacy?.DeleteValue( "LetAppsAccessLocation" );
			appPrivacy?.DeleteValue( "LetAppsAccessLocation_UserInControlOfTheseApps" );
			appPrivacy?.DeleteValue( "LetAppsAccessLocation_ForceAllowTheseApps" );
			appPrivacy?.DeleteValue( "LetAppsAccessLocation_ForceDenyTheseApps" );

			// lfsvc Service
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_LOCATION_TRACKING_LOCATIONS[2], "Status", 1 );
		}

		/// <summary>
		/// Used to re-enable hibernation.
		/// </summary>
		public static void EnableHibernation() {
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[0], "HibernateEnabled", 1 );
			Utilities.ChangeDWordRegistryKey( Registry.LocalMachine, HKLM_HIBERNATION_LOCATIONS[1], "ShowHibernateOption", 1 );
		}

		// Wrapper method that calls another program to manage tasks.
		private static Task DisableSchedule( string name ) => ProgramHandler.StartProgram( "schtasks", $"change /tn \"{name}\" /DISABLE" );

		// A helper method to either enable or disable a service. 
		private static Task ToggleService( string name, bool manual = false ) {
			const string POWERSHELL_SCRIPT_DISABLE = "Stop-Service \"{0}\" -WarningAction SilentlyContinue\nSet-Service \"{0}\" -StartupType Disabled";
			const string POWERSHELL_SCRIPT_ENABLE  = "Stop-Service \"{0}\" -WarningAction SilentlyContinue\nSet-Service \"{0}\" -StartupType Manual";
			var          script                    = manual ? string.Format( POWERSHELL_SCRIPT_ENABLE, name ) : string.Format( POWERSHELL_SCRIPT_DISABLE, name );

			return ProgramHandler.StartProgram( "powershell.exe", $"-Command {{{script}}} -WindowStyle Hidden" );
		}

		// A method to switch a service to manual.
		private static Task SwitchServiceToManual( string name ) {
			const string POWERSHELL_SCRIPT = "Set-Service \"{0}\" -StartupType Manual";

			return ProgramHandler.StartProgram( "powershell.exe", $"-Command {string.Format( POWERSHELL_SCRIPT, name )}" );
		}
	}
}
