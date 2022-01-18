using CTTDebloatNET.Models;

namespace CTTDebloatNET.ViewModels {
	public class ProgramInstallOptionViewModel : ViewModelBase {

		public string ProgramName => Program.DisplayName;

		public ProgramInfo Program {
			get;
		}

		public ProgramInstallOptionViewModel( ProgramInfo info ) {
			Program = info;
		}
		
	}
}
