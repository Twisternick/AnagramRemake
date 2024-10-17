using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;

namespace Tehelee
{
	public class ScriptModificationProcessor : UnityEditor.AssetModificationProcessor
	{
		private static readonly string NamespacePrefix = ""; // Replaces the default, which is the PascalCase of the project name.

		private static readonly string PacketsFolder = "Assets/Scripts/Networking/Packets";

		private static readonly string PacketRegistry = "Assets/Scripts/Networking/PacketRegistry.cs";

		public static void OnWillCreateAsset( string path )
		{
			////////////////////////////////

			path = path.Replace( ".meta", "" );
			int index = path.LastIndexOf( "." );

			if( index < 0 )
				return;

			////////////////////////////////

			string extension = path.Substring( index );
			if( extension != ".cs" && extension != ".js" && extension != ".boo" ) return;

			////////////////////////////////

			string localAssetPath = path.Substring( 0, index );

			index = Application.dataPath.LastIndexOf( "Assets" );
			path = Application.dataPath.Substring( 0, index ) + path;

			string fileText = System.IO.File.ReadAllText( path );

			fileText = fileText.Replace( "\r\n", "\n" ).Replace( '\r', '\n' );

			string templatePath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( EditorApplication.applicationPath ), "Data/Resources/ScriptTemplates/81-C# Script-NewBehaviourScript.cs.txt" );
			if( System.IO.File.Exists( templatePath ) )
			{
				string templateText = System.IO.File.ReadAllText( templatePath );
				templateText = templateText.Replace( "#SCRIPTNAME#", Path.GetFileNameWithoutExtension( path ) );
				templateText = templateText.Replace( "#NOTRIM#", string.Empty );

				if( !string.IsNullOrWhiteSpace( templateText ) && !templateText.Equals( fileText ) )
				{
					Debug.LogWarningFormat( "ScriptModificationProcessor: A new script was found without the native unity template content; skipping...\nPath: {0}", path );
					return;
				}
			}

			////////////////////////////////

			string[] folderSplit = localAssetPath.Split( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );

			////////////////////////////////

			string fullRewrite = null;

			string rewriteGlobal = null;
			string rewriteClass = null;
			string rewriteBody = null;

			string prefixGlobal = string.Empty;
			string prefixClass = string.Empty;
			string prefixBody = string.Empty;

			string walkDirectory = Path.GetDirectoryName( localAssetPath );

			for( int i = folderSplit.Length - 2; i > 0; i-- )
			{
				walkDirectory = walkDirectory.Substring( 0, walkDirectory.Length - ( folderSplit[ i ].Length + 1 ) );

				string fullRewritePath = Path.Combine( walkDirectory, string.Format( ".{0}.script", folderSplit[ i ] ) );
				bool hasFullRewrite = ( fullRewrite == null ) && File.Exists( fullRewritePath );

				if( hasFullRewrite )
				{
					string text = File.ReadAllText( fullRewritePath );

					if( text != null && text.Length > 0 )
						fullRewrite = text;
				}
				else
				{
					string[] templates = Directory.GetFiles( walkDirectory, string.Format( ".{0}.*.*.script", folderSplit[ i ] ), SearchOption.TopDirectoryOnly );

					for( int t = 0, c = templates.Length; t < c; t++ )
					{
						string name = Path.GetFileNameWithoutExtension( templates[ t ] ).Substring( 1 );

						string[] split = name.Split( '.' );

						string operand = split[ 1 ];
						string space = split[ 2 ];

						string file = File.ReadAllText( templates[ t ] );

						if( file.Replace( "\n", "" ).Replace( " ", "" ).Replace( "\t", "" ).Length == 0 )
							continue;

						if( operand.Equals( "override" ) )
						{
							if( space.Equals( "namespace" ) )
							{
								if( rewriteGlobal == null )
									rewriteGlobal = file;
							}
							else if( space.Equals( "class" ) )
							{
								if( rewriteClass == null )
									rewriteClass = file;
							}
							else if( space.Equals( "body" ) )
							{
								if( rewriteBody == null )
									rewriteBody = file;
							}
						}
						else if( operand.Equals( "append" ) )
						{
							if( space.Equals( "namespace" ) )
							{
								prefixGlobal = string.Format( "{0}\n{1}", file, prefixGlobal );
							}
							else if( space.Equals( "class" ) )
							{
								prefixClass = string.Format( "{0}\n{1}", file, prefixClass );
							}
							else if( space.Equals( "body" ) )
							{
								prefixBody = string.Format( "{0}\n{1}", file, prefixBody );
							}
						}
					}
				}
			}

			if( fullRewrite != null )
				fileText = fullRewrite;

			fileText = fileText.Replace( "\r\n", "\n" ).Replace( '\r', '\n' );

			int indexNamespace = 0, indexClass = 0, indexEndGlobal = 0;

			System.Action calculateIndicies = () =>
			{
				indexNamespace = fileText.IndexOf( "namespace" );
				if( indexNamespace != -1 )
					indexNamespace = fileText.LastIndexOf( '\n', indexNamespace, indexNamespace );

				indexClass = fileText.IndexOf( "class" );
				if( indexClass != -1 )
					indexClass = fileText.LastIndexOf( '\n', indexClass, indexClass );

				indexEndGlobal = Mathf.Min( indexNamespace, indexClass );
			};

			calculateIndicies();

			if( rewriteGlobal != null )
			{
				rewriteGlobal = string.Format( "\n{0}", rewriteGlobal.Replace( "\r\n", "\n" ).Replace( '\r', '\n' ) );

				fileText = string.Format( "{0}\n{1}", rewriteGlobal.Substring( 1 ), fileText.Substring( indexEndGlobal ) );

				calculateIndicies();

				indexEndGlobal = Mathf.Min( indexNamespace, indexClass );
			}

			if( rewriteClass != null )
			{
				rewriteClass = string.Format( "\n{0}", rewriteClass.Replace( "\r\n", "\n" ).Replace( '\r', '\n' ) );

				if( indexNamespace != -1 )
				{
					int parseIndex = 0;
					while( ( parseIndex = rewriteClass.IndexOf( '\n', parseIndex ) ) != -1 )
					{
						rewriteClass = rewriteClass.Insert( ++parseIndex, "\t" );
					}

					int beginClass = fileText.IndexOf( '{', indexNamespace ) + 2;

					int endClass = fileText.LastIndexOf( '}', fileText.Length - 1, fileText.Length - 1 ) - 1;

					fileText = string.Format( "{0}{1}{2}", fileText.Substring( 0, beginClass ), rewriteClass.Substring( 1 ), fileText.Substring( endClass ) );

					calculateIndicies();
				}
				else
				{
					fileText = string.Format( "{0}{1}", fileText.Substring( 0, indexClass ), rewriteClass.Substring( 1 ) );

					calculateIndicies();
				}
			}

			if( rewriteBody != null )
			{
				rewriteBody = string.Format( "\n{0}", rewriteBody.Replace( "\r\n", "\n" ).Replace( '\r', '\n' ) );

				if( indexClass != -1 )
				{
					int parseIndex = 0;
					while( ( parseIndex = rewriteBody.IndexOf( '\n', parseIndex ) ) != -1 )
					{
						rewriteBody = rewriteBody.Insert( ++parseIndex, indexNamespace != -1 ? "\t\t" : "\t" );
					}

					int beginBody = fileText.IndexOf( '{', indexClass ) + 2;

					int endBody = fileText.LastIndexOf( '}', fileText.Length - 1, fileText.Length - 1 ) - 1;
					if( indexNamespace != -1 )
						endBody = fileText.LastIndexOf( '}', endBody, endBody ) - 1;

					fileText = string.Format( "{0}{1}\n{2}", fileText.Substring( 0, beginBody ), rewriteBody.Substring( 1 ), fileText.Substring( endBody ) );
				}
			}

			if( prefixGlobal.Length > 0 )
			{
				prefixGlobal = string.Format( "\n{0}", prefixGlobal.Replace( '\r', '\n' ) );

				fileText = string.Format( "{0}{1}\n{2}", fileText.Substring( 0, indexEndGlobal ), prefixGlobal.Substring( 1 ), fileText.Substring( indexEndGlobal ) );

				calculateIndicies();
			}

			if( prefixClass.Length > 0 )
			{
				prefixClass = string.Format( "\n{0}", prefixClass.Replace( '\r', '\n' ) );

				if( indexNamespace != -1 )
				{
					int parseIndex = 0;
					while( ( parseIndex = prefixClass.IndexOf( '\n', parseIndex ) ) != -1 )
					{
						prefixClass = prefixClass.Insert( ++parseIndex, "\t" );
					}

					int beginClass = fileText.IndexOf( '{', indexNamespace ) + 2;

					fileText = string.Format( "{0}{1}\n{2}", fileText.Substring( 0, beginClass ), prefixClass.Substring( 1 ), fileText.Substring( beginClass ) );

					calculateIndicies();
				}
				else
				{
					fileText = string.Format( "{0}{1}\n{2}", fileText.Substring( 0, indexClass ), prefixClass.Substring( 1 ), fileText.Substring( indexClass ) );

					calculateIndicies();
				}
			}

			if( prefixBody.Length > 0 )
			{
				prefixBody = string.Format( "\n{0}", prefixBody.Replace( '\r', '\n' ) );

				if( indexClass != -1 )
				{
					int parseIndex = 0;
					while( ( parseIndex = prefixBody.IndexOf( '\n', parseIndex ) ) != -1 )
					{
						prefixBody = prefixBody.Insert( ++parseIndex, indexNamespace != -1 ? "\t\t" : "\t" );
					}

					int beginBody = fileText.IndexOf( '{', indexClass ) + 2;

					fileText = string.Format( ( indexNamespace != -1 ? "{0}{1}\n{2}" : "{0}{1}\n{2}" ), fileText.Substring( 0, beginBody ), prefixBody.Substring( 1 ), fileText.Substring( beginBody ) );
				}
			}

			////////////////////////////////

			int inScriptsFolder = -1;
			int inScenesFolder = -1;

			List<string> scriptFolders = new List<string>();
			for( int i = folderSplit.Length - 2; i > 0; i-- )
			{
				if( folderSplit[ i ].Equals( "Scripts" ) )
				{
					inScriptsFolder = i;
					continue;
				}

				if( folderSplit[ i ].Equals( "Scenes" ) )
				{
					inScenesFolder = i;
					continue;
				}

				if( folderSplit[ i ].Equals( "Editor" ) )
					continue;

				scriptFolders.Add( folderSplit[ i ] );
			}

			string namespacePrefix = !string.IsNullOrEmpty( NamespacePrefix ) ? NamespacePrefix : ToPascalCase( PlayerSettings.productName );

			string folderSpace = "";
			if( inScriptsFolder != -1 )
			{
				scriptFolders.Add( inScenesFolder != -1 ? string.Format( "{0}.scenes", namespacePrefix ) : namespacePrefix );

				scriptFolders.Reverse();

				foreach( string folder in scriptFolders )
				{
					folderSpace += string.Format( "{0}.", folder );
				}

				folderSpace = folderSpace.Substring( 0, folderSpace.Length - 1 ).ToLower();
			}

			////////////////////////////////

			fileText = fileText.Replace( "#FOLDERSPACE#", inScriptsFolder != -1 ? folderSpace : namespacePrefix );

			fileText = fileText.Replace( "#SCRIPTNAME#", Path.GetFileNameWithoutExtension( path ) );

			////////////////////////////////
			
			if( localAssetPath.StartsWith( PacketsFolder ) )
			{
				string packetName = Path.GetFileNameWithoutExtension( path ).Replace( ".", "" );

				string prefix = localAssetPath.Length > ( PacketsFolder.Length + packetName.Length + 1 ) ? localAssetPath.Substring( PacketsFolder.Length + 1, localAssetPath.Length - packetName.Length - PacketsFolder.Length - 2 ) : string.Empty;

				if( prefix.Length > 0 )
				{
					string[] prefixFolders = prefix.Split( '/' );
					for( int i = prefixFolders.Length - 1; i >= 0; i-- )
					{
						packetName = string.Format( "{0}_{1}", prefixFolders[ i ], packetName );
					}
				}

				string registryPath = PacketRegistry;
				Asset assetRegistry = new Asset( registryPath );

				registryPath = string.Format( "{0}/{1}", Application.dataPath, registryPath.Substring( 7 ) );

				string registryText = File.ReadAllText( registryPath ).Replace( "\r\n", "\n" ).Replace( '\r', '\n' );

				if( registryText.Contains( string.Format( "\t\t{0},", packetName ) ) )
				{
					Debug.LogWarningFormat( "Packet Registry already contains the packet type '{0}' for the new packet class: {1}.\nRegistry File Path: {2}", packetName, Path.GetFileNameWithoutExtension( path ), registryPath );

					fileText = fileText.Replace( "#PACKETID#", packetName );
				}
				else
				{
					int enumEnd = registryText.LastIndexOf( '}', registryText.Length - 1, registryText.Length - 1 );
					enumEnd = registryText.LastIndexOf( '}', enumEnd, enumEnd );
					enumEnd = registryText.LastIndexOf( '\n', enumEnd, enumEnd ) - 2;

					registryText = registryText.Insert( enumEnd, string.Format( "\t\t{0},\n", packetName ) );

					if( Provider.CheckoutIsValid( assetRegistry ) )
					{
						Provider.Checkout( assetRegistry, CheckoutMode.Asset );
					}

					try
					{
						File.WriteAllText( registryPath, registryText );

						fileText = fileText.Replace( "#PACKETID#", packetName );

						Debug.LogFormat( "Packet Registry has been appended to include the new packet type '{0}' for the packet class: {1}.\nRegistry File Path: {2}", packetName, Path.GetFileNameWithoutExtension( path ), registryPath );
					}
					catch( System.Exception )
					{
						Debug.LogErrorFormat( "Unable to modify Packet Registry to append new packet class; please add manually.\nRegistry File Path: {0}", registryPath );

						fileText = fileText.Replace( "#PACKETID#", "Invalid" );
					}
				}
			}

			////////////////////////////////
			
			fileText = fileText.Replace( "\r\n", "\n" ).Replace( '\r', '\n' );

			System.IO.File.WriteAllText( path, fileText );
			AssetDatabase.Refresh();
		}

		public static AssetDeleteResult OnWillDeleteAsset( string path, RemoveAssetOptions removeOptions )
		{
			////////////////////////////////

			path = path.Replace( ".meta", "" );
			int index = path.LastIndexOf( "." );

			if( index < 0 )
				return AssetDeleteResult.DidNotDelete;

			////////////////////////////////

			string extension = path.Substring( index );
			if( extension != ".cs" && extension != ".js" && extension != ".boo" ) return AssetDeleteResult.DidNotDelete;

			////////////////////////////////
			
			string localAssetPath = path.Substring( 0, index );

			if( localAssetPath.StartsWith( PacketsFolder ) )
			{
				string packetName = Path.GetFileNameWithoutExtension( path ).Replace( ".", "" );

				string prefix = localAssetPath.Length > ( PacketsFolder.Length + packetName.Length + 1 ) ? localAssetPath.Substring( PacketsFolder.Length + 1, localAssetPath.Length - packetName.Length - PacketsFolder.Length - 2 ) : string.Empty;

				if( prefix.Length > 0 )
				{
					string[] prefixFolders = prefix.Split( '/' );
					for( int i = prefixFolders.Length - 1; i >= 0; i-- )
					{
						packetName = string.Format( "{0}_{1}", prefixFolders[ i ], packetName );
					}
				}

				string registryPath = PacketRegistry;
				Asset assetRegistry = new Asset( registryPath );

				registryPath = string.Format( "{0}/{1}", Application.dataPath, registryPath.Substring( 7 ) );

				string registryText = File.ReadAllText( registryPath ).Replace( "\r\n", "\n" ).Replace( '\r', '\n' );

				string registryLookup = string.Format( "\t\t{0},\n", packetName );
				int registryIndex = registryText.IndexOf( registryLookup );

				if( registryIndex != -1 )
				{
					registryText = string.Format( "{0}{1}", registryText.Substring( 0, registryIndex ), registryText.Substring( registryIndex + registryLookup.Length ) );

					if( Provider.CheckoutIsValid( assetRegistry ) )
					{
						Provider.Checkout( assetRegistry, CheckoutMode.Asset );
					}

					try
					{
						File.WriteAllText( registryPath, registryText );

						Debug.LogFormat( "Packet Registry has been modified to remove the depreciated packet type '{0}' for the deleted packet class: {1}.\nRegistry File Path: {2}", packetName, Path.GetFileNameWithoutExtension( path ), registryPath );
					}
					catch( System.Exception )
					{
						Debug.LogErrorFormat( "Unable to modify Packet Registry to remove packet type '{0}' for the deleted class: {1}; please remove manually.\nRegistry File Path: {2}", packetName, Path.GetFileNameWithoutExtension( path ), registryPath );
					}
				}
			}

			////////////////////////////////

			return AssetDeleteResult.DidNotDelete;
		}

		private static char[] splitSpace = new char[] { ' ' };

		private static string ToPascalCase( string input )
		{
			if( string.IsNullOrEmpty( input ) )
				return string.Empty;

			string output = "";
			string[] split = input.Split( splitSpace, System.StringSplitOptions.RemoveEmptyEntries );

			foreach( string s in split )
			{
				output += char.ToUpper( s[ 0 ] ) + s.Substring( 1 ).ToLower();
			}

			return output;
		}
	}
}