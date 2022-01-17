using System;
using System.Text;
using Newtonsoft.Json;

namespace CTTDebloatNET.Models {
	/// <summary>
	/// Represents a program that can be installed.
	/// </summary>
	public readonly struct ProgramInfo {
		/// <summary>
		/// The display name of the program.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// The IDs required to install the program.
		/// </summary>
		public string[] IDs { get; }

		[JsonConstructor]
		public ProgramInfo( [JsonProperty("displayName")]string name, [JsonProperty("id")]string[] ids ) {
			if ( name == null ) {
				throw new ArgumentNullException( nameof( name ), "Expected a name, but got null." );
			}

			if ( string.IsNullOrWhiteSpace( name ) ) {
				throw new ArgumentException( "There must be a name associated with the ids.", nameof( name ) );
			}

			if ( ids == null ) {
				throw new ArgumentNullException( nameof( ids ), "Expected a list of IDs." );
			}

			if ( ids.Length == 0 ) {
				throw new ArgumentOutOfRangeException( nameof( ids ), "There must be at least 1 ID to associate with the program." );
			}

			DisplayName = name;
			IDs         = ids;
		}

		public override string ToString() {
			var builder = new StringBuilder( "Name:\t" ).AppendLine( DisplayName );

			for ( var i = 0; i < IDs.Length; ++i ) {
				builder.Append( "ID [" ).Append( i + 1 ).Append( "]:\t" ).AppendLine( IDs[i] );
			}

			return builder.ToString();
		}
	}
}
