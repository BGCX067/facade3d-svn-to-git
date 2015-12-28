// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;
using GameCommon;
using System.Xml;
using GeneralMeshUtils;

namespace ColladaMeshFormat
{
	[CustomMeshFormatLoaderExtensions( new string[] { "dae" } )]
	public class ColladaMeshFormatLoader : CustomMeshFormatLoader
	{
		string currentFileName;
		Mesh currentMesh;

		float globalScale = 1;
		bool yAxisUp = true;
		ColladaExportingTools colladaExportingTool = ColladaExportingTools.Unknown;
		//parsed material info
		Dictionary<string, string> generatedEffects;//<effectId, materialId>
		Dictionary<string, string> generatedBindedMaterials;//<materialId, originalMaterialName>
		Dictionary<string, MySceneMaterial> generatedMaterials;//<material name, material>
		Dictionary<string, string> generatedImages;//<imageId, textureFilePath>
		Dictionary<string, GeometryItem> generatedGeometries;
		List<MySceneSubMesh> generatedSubMeshes;

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MySceneMaterial : ISceneMaterial
		{
			string name;
			ColorValue diffuseColor = new ColorValue( 1, 1, 1 );
			string diffuse1Map = "";
			string diffuse2Map = "";
			string diffuse3Map = "";
			string diffuse4Map = "";
			string diffuse1TexCoord = "";
			string diffuse2TexCoord = "";
			string diffuse3TexCoord = "";
			string diffuse4TexCoord = "";
			string diffuse2MapBlending = "";
			string diffuse3MapBlending = "";
			string diffuse4MapBlending = "";
			string specularMap = "";
			string specularTexCoord;
			ColorValue specularColor;
			string emissionMap = "";
			string emissionTexCoord;
			ColorValue emissionColor;
			float shininess;
			float transparency;
			ColorValue transparentColor;
			bool opaque;
			bool culling = true;

			//

			public MySceneMaterial( string name )
			{
				this.name = name;
			}

			public string Name
			{
				get { return name; }
				set { name = value; }
			}

			public ColorValue DiffuseColor
			{
				get { return diffuseColor; }
				set { diffuseColor = value; }
			}

			public string Diffuse1Map
			{
				get { return diffuse1Map; }
				set { diffuse1Map = value; }
			}

			public string Diffuse2Map
			{
				get { return diffuse2Map; }
				set { diffuse2Map = value; }
			}

			public string Diffuse3Map
			{
				get { return diffuse3Map; }
				set { diffuse3Map = value; }
			}

			public string Diffuse4Map
			{
				get { return diffuse4Map; }
				set { diffuse4Map = value; }
			}

			public string Diffuse1TexCoord
			{
				get { return diffuse1TexCoord; }
				set { diffuse1TexCoord = value; }
			}

			public string Diffuse2TexCoord
			{
				get { return diffuse2TexCoord; }
				set { diffuse2TexCoord = value; }
			}

			public string Diffuse3TexCoord
			{
				get { return diffuse3TexCoord; }
				set { diffuse3TexCoord = value; }
			}

			public string Diffuse4TexCoord
			{
				get { return diffuse4TexCoord; }
				set { diffuse4TexCoord = value; }
			}

			public string Diffuse2MapBlending
			{
				get { return diffuse2MapBlending; }
				set { diffuse2MapBlending = value; }
			}

			public string Diffuse3MapBlending
			{
				get { return diffuse3MapBlending; }
				set { diffuse3MapBlending = value; }
			}

			public string Diffuse4MapBlending
			{
				get { return diffuse4MapBlending; }
				set { diffuse4MapBlending = value; }
			}

			public float Transparency
			{
				get { return transparency; }
				set { transparency = value; }
			}

			public ColorValue TransparentColor
			{
				get { return transparentColor; }
				set { transparentColor = value; }
			}

			public bool Opaque
			{
				get { return opaque; }
				set { opaque = value; }
			}

			public float GetAlpha()
			{
				if( opaque )
					return transparency;
				if( transparentColor.Red > 0 || transparentColor.Green > 0 || transparentColor.Blue > 0 )
					return 1.0f - transparency;
				return 1;
			}

			public string SpecularMap
			{
				get { return specularMap; }
				set { specularMap = value; }
			}

			public string SpecularTexCoord
			{
				get { return specularTexCoord; }
				set { specularTexCoord = value; }
			}

			public ColorValue SpecularColor
			{
				get { return specularColor; }
				set { specularColor = value; }
			}

			public float Shininess
			{
				get { return shininess; }
				set { shininess = value; }
			}

			public string EmissionMap
			{
				get { return emissionMap; }
				set { emissionMap = value; }
			}

			public string EmissionTexCoord
			{
				get { return emissionTexCoord; }
				set { emissionTexCoord = value; }
			}

			public ColorValue EmissionColor
			{
				get { return emissionColor; }
				set { emissionColor = value; }
			}

			public bool Culling
			{
				get { return culling; }
				set { culling = value; }
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class GeometryItem
		{
			string id;
			SubMesh[] subMeshes;

			//

			public class SubMesh
			{
				SubMeshVertex[] vertices;
				int[] indices;
				int textureCoordCount;
				bool vertexColors;
				MySceneMaterial material;

				//

				public SubMesh( SubMeshVertex[] vertices, int[] indices, int textureCoordCount,
					bool vertexColors, MySceneMaterial material )
				{
					this.vertices = vertices;
					this.indices = indices;
					this.textureCoordCount = textureCoordCount;
					this.vertexColors = vertexColors;
					this.material = material;
				}

				public SubMeshVertex[] Vertices
				{
					get { return vertices; }
				}

				public int[] Indices
				{
					get { return indices; }
				}

				public int TextureCoordCount
				{
					get { return textureCoordCount; }
				}

				public bool VertexColors
				{
					get { return vertexColors; }
				}

				public MySceneMaterial Material
				{
					get { return material; }
				}
			}

			//

			public GeometryItem( string id, SubMesh[] subMeshes )
			{
				this.id = id;
				this.subMeshes = subMeshes;
			}

			public string Id
			{
				get { return id; }
			}

			public SubMesh[] SubMeshes
			{
				get { return subMeshes; }
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MySceneSubMesh : ISceneSubMesh
		{
			SubMeshVertex[] vertices;
			int[] indices;
			int textureCoordCount;
			bool vertexColors;
			MySceneMaterial material;

			//

			public MySceneSubMesh( SubMeshVertex[] vertices, int[] indices, int textureCoordCount,
				bool vertexColors, MySceneMaterial material )
			{
				this.vertices = vertices;
				this.indices = indices;
				this.textureCoordCount = textureCoordCount;
				this.vertexColors = vertexColors;
				this.material = material;
			}

			public void GetGeometry( out SubMeshVertex[] vertices, out int[] indices )
			{
				vertices = this.vertices;
				indices = this.indices;
			}

			public int GetTextureCoordCount()
			{
				return textureCoordCount;
			}

			public bool VertexColors
			{
				get { return vertexColors; }
			}

			public ISceneMaterial Material
			{
				get { return material; }
			}

			public SceneBoneAssignmentItem[] GetVertexBoneAssignment( int vertexIndex )
			{
				return null;
			}

			public void GetVerticesByTime( float timeFrame, out Vec3[] vertices, bool skeletonOn )
			{
				vertices = null;
				skeletonOn = false;
			}

			public bool HasPoses()
			{
				return false;
			}

			public void GetPoses( out PoseInfo[] poses )
			{
				poses = null;
			}

			public void GetPoseReferenceByTime( float timeFrame, out PoseReference[] poseReferences )
			{
				poseReferences = null;
			}

			public bool AllowCollision
			{
				get { return true; }
			}

			public UVUnwrapChannels UVUnwrapChannel
			{
				get { return UVUnwrapChannels.None; }
			}

			public bool HasVertexColors()
			{
				return vertexColors;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MyMeshSceneObject : IMeshSceneObject
		{
			MySceneSubMesh[] subMeshes;

			//

			public MyMeshSceneObject( MySceneSubMesh[] subMeshes )
			{
				this.subMeshes = subMeshes;
			}


			public ISceneSubMesh[] SubMeshes
			{
				get { return subMeshes; }
			}

			public IList<ISceneBone> SkinBones
			{
				get
				{
					return new ISceneBone[ 0 ];
				}
			}

			public int AnimationFrameRate
			{
				get
				{
					return 0;
				}
			}

			public void GetBoundsByTime( float timeFrame, out Bounds bounds, out float radius )
			{
				Log.Fatal( "GetBoundsByTime" );
				bounds = Bounds.Cleared;
				radius = -1;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class SourceItem
		{
			string id;
			float[] data;
			int offset;
			int stride;

			//

			public SourceItem( string id, float[] data, int offset, int stride )
			{
				this.id = id;
				this.data = data;
				this.offset = offset;
				this.stride = stride;
			}

			public string Id
			{
				get { return id; }
			}

			public float[] Data
			{
				get { return data; }
			}

			public int Offset
			{
				get { return offset; }
			}

			public int Stride
			{
				get { return stride; }
			}

			public Vec2 GetItemVec2( int index )
			{
				int index2 = offset + index * stride;
				return new Vec2(
					data[ index2 + 0 ],
					data[ index2 + 1 ] );
			}

			public Vec3 GetItemVec3( int index )
			{
				int index2 = offset + index * stride;
				return new Vec3(
					data[ index2 + 0 ],
					data[ index2 + 1 ],
					data[ index2 + 2 ] );
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		enum ChannelTypes
		{
			Unknown,

			POSITION,
			NORMAL,
			COLOR,
			TEXCOORD0,
			TEXCOORD1,
			TEXCOORD2,
			TEXCOORD3,
			TANGENT,
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		enum ColladaExportingTools
		{
			Unknown,

			FBX,
			XSI,
			Blender,
			Unwrap3d,
			NeoAxis,
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		void Error( string format, params object[] args )
		{
			Log.Warning(
				string.Format( "ColladaMeshFormatLoader: Cannot load file \"{0}\". ", currentFileName ) +
				string.Format( format, args ) );
		}

		static float[] ConvertStringToFloatArray( string str )
		{
			if( string.IsNullOrEmpty( str ) )
				return new float[ 0 ];

			string fixedStr = str.Replace( ',', '.' );

			string[] strings = fixedStr.Split( new char[] { ' ', '\n', '\t', '\r' },
				StringSplitOptions.RemoveEmptyEntries );

			float[] array = new float[ strings.Length ];

			for( int n = 0; n < strings.Length; n++ )
			{
				float value;
				if( !float.TryParse( strings[ n ], out value ) )
					return null;

				array[ n ] = value;
			}

			return array;
		}

		static ColorValue ConvertStringToColorValue( string str )
		{
			if( string.IsNullOrEmpty( str ) )
				return new ColorValue();

			string fixedStr = str.Replace( ',', '.' );

			string[] strings = fixedStr.Split( new char[] { ' ', '\n', '\t', '\r' },
				 StringSplitOptions.RemoveEmptyEntries );

			ColorValue color = new ColorValue();

			if( strings.Length >= 3 )
			{
				float red;
				if( !float.TryParse( strings[ 0 ], out red ) )
					return color;

				float green;
				if( !float.TryParse( strings[ 1 ], out green ) )
					return color;

				float blue;
				if( !float.TryParse( strings[ 2 ], out blue ) )
					return color;

				color.Red = red;
				color.Green = green;
				color.Blue = blue;

				if( strings.Length == 4 )
				{
					float alpha;
					if( !float.TryParse( strings[ 3 ], out alpha ) )
						return color;
					color.Alpha = alpha;
				}
			}

			return color;
		}

		static int[] ConvertStringToIntArray( string str )
		{
			if( string.IsNullOrEmpty( str ) )
				return new int[ 0 ];

			string fixedStr = str.Replace( ',', '.' );

			string[] strings = fixedStr.Split( new char[] { ' ', '\n', '\t', '\r' },
				StringSplitOptions.RemoveEmptyEntries );

			int[] array = new int[ strings.Length ];

			for( int n = 0; n < strings.Length; n++ )
			{
				int value;
				if( !int.TryParse( strings[ n ], out value ) )
					return null;

				array[ n ] = value;
			}

			return array;
		}

		static Vec2 ConvertColladaTexCoordToNeoAxisTexCoord( Vec2 colladaTexCoord )
		{
			return new Vec2( colladaTexCoord.X, 1 - colladaTexCoord.Y );
		}

		ColladaExportingTools GetColladaExportingTool( string authoringTool )
		{
			if( authoringTool.Contains( "FBX" ) )
				return ColladaExportingTools.FBX;

			if( authoringTool.Contains( "Softimage" ) )
				return ColladaExportingTools.XSI;

			if( authoringTool.Contains( "Blender" ) )
				return ColladaExportingTools.Blender;

			if( authoringTool.Contains( "Unwrap3D" ) )
				return ColladaExportingTools.Unwrap3d;

			if( authoringTool.Contains( "NeoAxis" ) )
				return ColladaExportingTools.NeoAxis;

			return ColladaExportingTools.Unknown;
		}

		string GetVirtualPath( string colladaFileDirectory, string path )
		{
			if( path.IndexOf( "file://" ) >= 0 )
				path = path.Remove( 0, 7 );
			if( path.Length > 0 && ( path[ 0 ] == '/' || path[ 0 ] == '\\' ) )
				path = path.Remove( 0, 1 );

			if( Path.IsPathRooted( path ) )
			{
				string virtualPath = VirtualFileSystem.GetVirtualPathByReal( path );
				if( string.IsNullOrEmpty( virtualPath ) )
					virtualPath = Path.Combine( colladaFileDirectory, Path.GetFileName( path ) );
				return virtualPath;
			}
			else
			{
				if( path.Length > 1 && path[ 0 ] == '.' && ( path[ 1 ] == '/' || path[ 1 ] == '\\' ) )
					path = path.Remove( 0, 2 );
				return Path.Combine( colladaFileDirectory, path );
			}
		}

		ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes GetMapBlendingType( string blendingMode )
		{
			if( blendingMode == "ADD" )
				return ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes.Add;

			if( blendingMode == "BLEND" )
				return ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes.AlphaBlend;

			return ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes.Modulate;
		}

		ShaderBaseMaterial.TexCoordIndexes GetNeoAxisTexCoordByColladaTexCoord( string texCoord )
		{
			if( texCoord == "CHANNEL1" )
				return ShaderBaseMaterial.TexCoordIndexes.TexCoord1;
			if( texCoord == "CHANNEL2" )
				return ShaderBaseMaterial.TexCoordIndexes.TexCoord2;
			if( texCoord == "CHANNEL3" )
				return ShaderBaseMaterial.TexCoordIndexes.TexCoord3;

			return ShaderBaseMaterial.TexCoordIndexes.TexCoord0;
		}

		MySceneMaterial GetOrCreateMaterial( string materialName )
		{
			MySceneMaterial material;
			if( !generatedMaterials.TryGetValue( materialName, out material ) )
			{
				material = new MySceneMaterial( materialName );
				generatedMaterials.Add( material.Name, material );
			}
			return material;
		}

		bool ParseSource( XmlNode sourceNode, out SourceItem source )
		{
			source = null;

			string id = XmlUtils.GetAttribute( sourceNode, "id" );

			XmlNode floatArrayNode = XmlUtils.FindChildNode( sourceNode, "float_array" );
			if( floatArrayNode == null )
			{
				Error( "\"float_array\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			int floatsCount;
			if( !int.TryParse( XmlUtils.GetAttribute( floatArrayNode, "count" ), out floatsCount ) )
			{
				Error( "Invalid \"count\" attribute of floats array. Source: \"{0}\".", id );
				return false;
			}

			float[] data = ConvertStringToFloatArray( floatArrayNode.InnerText );
			if( data == null )
			{
				Error( "Cannot read array with name \"{0}\".", XmlUtils.GetAttribute( floatArrayNode, "id" ) );
				return false;
			}

			if( data.Length != floatsCount )
			{
				Error( "Invalid amount of items in \"float_array\". Required amount: \"{0}\". " +
					"Real amount: \"{1}\". Source: \"{2}\".", floatsCount, data.Length, id );
				return false;
			}

			XmlNode techniqueCommonNode = XmlUtils.FindChildNode( sourceNode, "technique_common" );
			if( techniqueCommonNode == null )
			{
				Error( "\"technique_common\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			XmlNode accessorNode = XmlUtils.FindChildNode( techniqueCommonNode, "accessor" );
			if( accessorNode == null )
			{
				Error( "\"accessor\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			int offset = 0;
			{
				string offsetAttribute = XmlUtils.GetAttribute( accessorNode, "offset" );
				if( !string.IsNullOrEmpty( offsetAttribute ) )
				{
					if( !int.TryParse( offsetAttribute, out offset ) )
					{
						Error( "Invalid \"offset\" attribute of accessor. Source: \"{0}\".", id );
						return false;
					}
				}
			}

			int stride = 1;
			{
				string strideAttribute = XmlUtils.GetAttribute( accessorNode, "stride" );
				if( !string.IsNullOrEmpty( strideAttribute ) )
				{
					if( !int.TryParse( strideAttribute, out stride ) )
					{
						Error( "Invalid \"stride\" attribute of accessor. Source: \"{0}\".", id );
						return false;
					}
				}
			}

			int count;
			if( !int.TryParse( XmlUtils.GetAttribute( accessorNode, "count" ), out count ) )
			{
				Error( "Invalid \"count\" attribute of accessor. Source: \"{0}\".", id );
				return false;
			}

			source = new SourceItem( id, data, offset, stride );
			return true;
		}

		bool ParseMeshSources( XmlNode meshNode, out List<SourceItem> sources )
		{
			sources = new List<SourceItem>();

			foreach( XmlNode childNode in meshNode.ChildNodes )
			{
				if( childNode.Name != "source" )
					continue;

				SourceItem source;
				if( !ParseSource( childNode, out source ) )
					return false;

				sources.Add( source );
			}

			return true;
		}

		string GetIdFromURL( string url )
		{
			if( string.IsNullOrEmpty( url ) )
				return "";
			if( url[ 0 ] != '#' )
				return "";
			return url.Substring( 1 );
		}

		bool ParseInputNode( string geometryId, Dictionary<string, string> vertexSemanticDictionary,
			XmlNode inputNode, out int offset, out string sourceId, out ChannelTypes channelType )
		{
			offset = 0;
			sourceId = null;
			channelType = ChannelTypes.Unknown;

			//offset
			if( !int.TryParse( XmlUtils.GetAttribute( inputNode, "offset" ), out offset ) )
			{
				Error( "Invalid \"offset\" attribute. Geometry: \"{0}\".", geometryId );
				return false;
			}

			string semantic = XmlUtils.GetAttribute( inputNode, "semantic" );

			//channelType
			{
				int set = 0;
				{
					string setAsString = XmlUtils.GetAttribute( inputNode, "set" );
					if( !string.IsNullOrEmpty( setAsString ) )
					{
						if( !int.TryParse( setAsString, out set ) )
						{
							Error( "Invalid \"set\" attribute. Geometry: \"{0}\".", geometryId );
							return false;
						}
					}
				}

				if( semantic == "VERTEX" )
					channelType = ChannelTypes.POSITION;
				else if( semantic == "POSITION" )
					channelType = ChannelTypes.POSITION;
				else if( semantic == "NORMAL" )
					channelType = ChannelTypes.NORMAL;
				else if( semantic == "COLOR" )
					channelType = ChannelTypes.COLOR;
				else if( semantic == "TEXCOORD" )
				{
					switch( set )
					{
					case 0: channelType = ChannelTypes.TEXCOORD0; break;
					case 1: channelType = ChannelTypes.TEXCOORD1; break;
					case 2: channelType = ChannelTypes.TEXCOORD2; break;
					case 3: channelType = ChannelTypes.TEXCOORD3; break;
					default: channelType = ChannelTypes.Unknown; break;
					}
				}
				else if( semantic == "TANGENT" )
					channelType = ChannelTypes.TANGENT;
				else
				{
					channelType = ChannelTypes.Unknown;
				}
			}

			//sourceId
			{
				string sourceUrl = XmlUtils.GetAttribute( inputNode, "source" );

				sourceId = GetIdFromURL( sourceUrl );
				if( string.IsNullOrEmpty( sourceId ) )
				{
					Error( "Invalid \"source\" attribute for input node. Source url: \"{0}\". Geometry: \"{1}\".",
						sourceUrl, geometryId );
					return false;
				}

				if( semantic == "VERTEX" )
				{
					string newSourceId;
					if( !vertexSemanticDictionary.TryGetValue( sourceId, out newSourceId ) )
					{
						Error( "Cannot find vertices node with \"{0}\". Geometry: \"{1}\".",
							sourceId, geometryId );
						return false;
					}
					sourceId = newSourceId;
				}
			}

			return true;
		}

		bool ParseInputs( string geometryId, Dictionary<string, SourceItem> sourceDictionary,
			Dictionary<string, string> vertexSemanticDictionary, XmlNode primitiveElementsNode,
			out Pair<ChannelTypes, SourceItem>[] inputs )
		{
			inputs = null;

			List<Pair<ChannelTypes, SourceItem>> inputList = new List<Pair<ChannelTypes, SourceItem>>();

			foreach( XmlNode inputNode in primitiveElementsNode.ChildNodes )
			{
				if( inputNode.Name != "input" )
					continue;

				int offset;
				string sourceId;
				ChannelTypes channelType;

				if( !ParseInputNode( geometryId, vertexSemanticDictionary, inputNode, out offset, out sourceId,
					out channelType ) )
				{
					return false;
				}

				while( inputList.Count < offset + 1 )
					inputList.Add( new Pair<ChannelTypes, SourceItem>( ChannelTypes.Unknown, null ) );

				if( channelType != ChannelTypes.Unknown )
				{
					if( inputList[ offset ].Second != null )
					{
						Error( "Input with offset \"{0}\" is already defined.", offset );
						return false;
					}

					SourceItem source;
					if( !sourceDictionary.TryGetValue( sourceId, out source ) )
					{
						Error( "Source with id \"{0}\" is not exists.", sourceId );
						return false;
					}

					inputList[ offset ] = new Pair<ChannelTypes, SourceItem>( channelType, source );
				}
			}

			inputs = inputList.ToArray();

			return true;
		}

		SubMeshVertex[] GenerateSubMeshVertices( Pair<ChannelTypes, SourceItem>[] inputs, int vertexCount,
			int[] indices, int startIndex )
		{
			SubMeshVertex[] itemVertices = new SubMeshVertex[ vertexCount ];

			int currentIndex = startIndex;

			for( int nVertex = 0; nVertex < itemVertices.Length; nVertex++ )
			{
				SubMeshVertex vertex = new SubMeshVertex();

				foreach( Pair<ChannelTypes, SourceItem> input in inputs )
				{
					ChannelTypes channelType = input.First;
					SourceItem source = input.Second;

					int indexValue = indices[ currentIndex ];
					currentIndex++;

					switch( channelType )
					{
					case ChannelTypes.POSITION:
						vertex.position = source.GetItemVec3( indexValue );
						break;

					case ChannelTypes.NORMAL:
						vertex.normal = source.GetItemVec3( indexValue );
						break;

					case ChannelTypes.TEXCOORD0:
						vertex.texCoord0 = ConvertColladaTexCoordToNeoAxisTexCoord(
							source.GetItemVec2( indexValue ) );
						break;

					case ChannelTypes.TEXCOORD1:
						vertex.texCoord1 = ConvertColladaTexCoordToNeoAxisTexCoord(
							source.GetItemVec2( indexValue ) );
						break;

					case ChannelTypes.TEXCOORD2:
						vertex.texCoord2 = ConvertColladaTexCoordToNeoAxisTexCoord(
							source.GetItemVec2( indexValue ) );
						break;

					case ChannelTypes.TEXCOORD3:
						vertex.texCoord3 = ConvertColladaTexCoordToNeoAxisTexCoord(
							source.GetItemVec2( indexValue ) );
						break;

					case ChannelTypes.COLOR:
						{
							Vec3 c = source.GetItemVec3( indexValue ); ;
							vertex.color = new ColorValue( c.X, c.Y, c.Z, 1 );
						}
						break;

					//maybe need use "TEXTANGENT".
					//case ChannelTypes.TANGENT:
					//   vertex.tangent = source.GetItemVec3( indexValue );
					//   break;

					}
				}

				itemVertices[ nVertex ] = vertex;
			}

			return itemVertices;
		}

		bool ParseMeshNode( string geometryId, XmlNode meshNode )
		{
			List<SourceItem> sources;
			if( !ParseMeshSources( meshNode, out sources ) )
				return false;

			Dictionary<string, SourceItem> sourceDictionary = new Dictionary<string, SourceItem>();
			foreach( SourceItem source in sources )
				sourceDictionary.Add( source.Id, source );

			//vertexSemanticDictionary
			Dictionary<string, string> vertexSemanticDictionary = new Dictionary<string, string>();
			{
				foreach( XmlNode verticesNode in meshNode.ChildNodes )
				{
					if( verticesNode.Name != "vertices" )
						continue;

					string id = XmlUtils.GetAttribute( verticesNode, "id" );

					XmlNode inputNode = XmlUtils.FindChildNode( verticesNode, "input" );
					if( inputNode == null )
					{
						Error( "\"input\" node is not defined for vertices node \"{0}\". Geometry: \"{1}\".",
							id, geometryId );
						return false;
					}

					string sourceUrl = XmlUtils.GetAttribute( inputNode, "source" );

					string sourceId = GetIdFromURL( sourceUrl );
					if( string.IsNullOrEmpty( sourceId ) )
					{
						Error( "Invalid \"source\" attribute for vertices node \"{0}\". Source url: \"{1}\". " +
							"Geometry: \"{2}\".", id, sourceUrl, geometryId );
						return false;
					}

					vertexSemanticDictionary.Add( id, sourceId );
				}
			}

			List<GeometryItem.SubMesh> geometrySubMeshes = new List<GeometryItem.SubMesh>();

			//polygons, triangles, ...
			foreach( XmlNode primitiveElementsNode in meshNode.ChildNodes )
			{
				bool lines = primitiveElementsNode.Name == "lines";
				bool linestrips = primitiveElementsNode.Name == "linestrips";
				bool polygons = primitiveElementsNode.Name == "polygons";
				bool polylist = primitiveElementsNode.Name == "polylist";
				bool triangles = primitiveElementsNode.Name == "triangles";
				bool trifans = primitiveElementsNode.Name == "trifans";
				bool tristrips = primitiveElementsNode.Name == "tristrips";

				if( lines || linestrips || trifans || tristrips )
				{
					Log.Warning( "\"{0}\" primitive element is not supported. Geometry: \"{1}\". " +
						"\n\nElement was skipped.", primitiveElementsNode.Name, geometryId );
					continue;
				}

				if( polygons || triangles || polylist )
				{
					int itemCount;
					if( !int.TryParse( XmlUtils.GetAttribute( primitiveElementsNode, "count" ), out itemCount ) )
					{
						Error( "Invalid \"count\" attribute of \"{0}\". Geometry: \"{1}\".",
							primitiveElementsNode.Name, geometryId );
						return false;
					}

					Pair<ChannelTypes, SourceItem>[] inputs;
					if( !ParseInputs( geometryId, sourceDictionary, vertexSemanticDictionary,
						primitiveElementsNode, out inputs ) )
					{
						return false;
					}

					int textureCoordCount = 0;
					bool vertexColors = false;
					{
						foreach( Pair<ChannelTypes, SourceItem> input in inputs )
						{
							ChannelTypes channelType = input.First;

							if( channelType >= ChannelTypes.TEXCOORD0 && channelType <= ChannelTypes.TEXCOORD3 )
							{
								int v = channelType - ChannelTypes.TEXCOORD0;
								if( ( v + 1 ) > textureCoordCount )
									textureCoordCount = v + 1;
							}

							if( channelType == ChannelTypes.COLOR )
								vertexColors = true;
						}
					}

					List<SubMeshVertex> vertices = new List<SubMeshVertex>( itemCount );

					if( polygons )
					{
						foreach( XmlNode pNode in primitiveElementsNode.ChildNodes )
						{
							if( pNode.Name != "p" )
								continue;

							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read index array of geometry \"{0}\".", geometryId );
								return false;
							}

							int vertexCount = indexValues.Length / inputs.Length;
							SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs,
								vertexCount, indexValues, 0 );

							//generate triangles
							for( int n = 1; n < itemVertices.Length - 1; n++ )
							{
								vertices.Add( itemVertices[ 0 ] );
								vertices.Add( itemVertices[ n ] );
								vertices.Add( itemVertices[ n + 1 ] );
							}
						}
					}

					if( triangles )
					{
						XmlNode pNode = XmlUtils.FindChildNode( primitiveElementsNode, "p" );

						if( pNode != null )
						{
							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int vertexCount = indexValues.Length / inputs.Length;

							if( itemCount != vertexCount / 3 )
							{
								Error( "Invalid item amount of \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs,
								vertexCount, indexValues, 0 );

							//generate triangles
							for( int n = 0; n < vertexCount; n++ )
								vertices.Add( itemVertices[ n ] );
						}
					}

					if( polylist )
					{
						XmlNode vCountNode = XmlUtils.FindChildNode( primitiveElementsNode, "vcount" );
						XmlNode pNode = XmlUtils.FindChildNode( primitiveElementsNode, "p" );

						if( vCountNode != null && pNode != null )
						{
							int[] vCount = ConvertStringToIntArray( vCountNode.InnerText );
							if( vCount == null )
							{
								Error( "Cannot read \"vcount\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							if( vCount.Length != itemCount )
							{
								Error( "Invalid item amount of \"vcount\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int currentIndex = 0;

							foreach( int polyCount in vCount )
							{
								SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs, polyCount,
									indexValues, currentIndex );
								currentIndex += polyCount * inputs.Length;

								//generate triangles
								for( int n = 1; n < itemVertices.Length - 1; n++ )
								{
									vertices.Add( itemVertices[ 0 ] );
									vertices.Add( itemVertices[ n ] );
									vertices.Add( itemVertices[ n + 1 ] );
								}

							}

							if( currentIndex != indexValues.Length )
							{
								Error( "Invalid indices of geometry \"{0}\".", geometryId );
								return false;
							}
						}
					}

					if( vertices.Count != 0 )
					{
						int[] indices = new int[ vertices.Count ];
						for( int n = 0; n < indices.Length; n++ )
							indices[ n ] = n;

						MySceneMaterial material = null;
						{
							string materialName = XmlUtils.GetAttribute( primitiveElementsNode, "material" );
							if( !string.IsNullOrEmpty( materialName ) )
								material = GetOrCreateMaterial( materialName );
						}

						//add to geometrySubMeshes
						GeometryItem.SubMesh geometrySubMesh = new GeometryItem.SubMesh(
							vertices.ToArray(), indices, textureCoordCount, vertexColors, material );
						geometrySubMeshes.Add( geometrySubMesh );
					}

					continue;
				}
			}

			//add to generatedGeometries
			{
				GeometryItem geometry = new GeometryItem( geometryId, geometrySubMeshes.ToArray() );
				generatedGeometries.Add( geometry.Id, geometry );
			}

			return true;
		}

		bool ParseGeometry( XmlNode geometryNode )
		{
			string geometryId = XmlUtils.GetAttribute( geometryNode, "id" );

			//find mesh node
			XmlNode meshNode = XmlUtils.FindChildNode( geometryNode, "mesh" );
			if( meshNode == null )
			{
				Error( "Mesh node is not exists for geometry \"{0}\".", geometryId );
				return false;
			}

			if( !ParseMeshNode( geometryId, meshNode ) )
				return false;

			return true;
		}

		bool ParseGeometries( XmlNode colladaNode )
		{
			foreach( XmlNode libraryGeometriesNode in colladaNode.ChildNodes )
			{
				if( libraryGeometriesNode.Name != "library_geometries" )
					continue;

				foreach( XmlNode geometryNode in libraryGeometriesNode.ChildNodes )
				{
					if( geometryNode.Name != "geometry" )
						continue;

					if( !ParseGeometry( geometryNode ) )
						return false;
				}
			}

			return true;
		}

		bool ParseNodeInstanceGeometry( Mat4 nodeTransform, XmlNode instanceGeometry )
		{
			string url = XmlUtils.GetAttribute( instanceGeometry, "url" );

			bool nodeTransformIdentity = nodeTransform.Equals( Mat4.Identity, .0001f );

			string geometryId = GetIdFromURL( url );
			if( string.IsNullOrEmpty( geometryId ) )
			{
				Error( "Invalid \"url\" attribute specified for \"instance_geometry\". Url: \"{0}\".", url );
				return false;
			}

			GeometryItem geometry;
			if( !generatedGeometries.TryGetValue( geometryId, out geometry ) )
			{
				Error( "Geometry with id \"{0}\" is not exists.", geometryId );
				return false;
			}

			foreach( GeometryItem.SubMesh geometrySubMesh in geometry.SubMeshes )
			{
				SubMeshVertex[] newVertices = new SubMeshVertex[ geometrySubMesh.Vertices.Length ];

				for( int n = 0; n < newVertices.Length; n++ )
				{
					SubMeshVertex vertex = geometrySubMesh.Vertices[ n ];

					if( !nodeTransformIdentity )
					{
						Vec3 oldPosition = vertex.position;
						vertex.position = nodeTransform * vertex.position;
						Vec3 p2 = nodeTransform * ( oldPosition + vertex.normal );
						vertex.normal = ( p2 - vertex.position ).GetNormalize();
					}

					newVertices[ n ] = vertex;
				}

				MySceneSubMesh sceneSubMesh = new MySceneSubMesh( newVertices, geometrySubMesh.Indices,
					geometrySubMesh.TextureCoordCount, geometrySubMesh.VertexColors, geometrySubMesh.Material );
				generatedSubMeshes.Add( sceneSubMesh );
			}

			XmlNode bindMaterialNode = XmlUtils.FindChildNode( instanceGeometry, "bind_material" );
			if( bindMaterialNode != null )
			{
				XmlNode techniqueCommonNode = XmlUtils.FindChildNode( bindMaterialNode, "technique_common" );
				if( techniqueCommonNode == null )
				{
					Error( "Technique_common node is not exists for geometry \"{0}\".", geometryId );
					return false;
				}

				foreach( XmlNode instanceMaterialNode in techniqueCommonNode.ChildNodes )
				{
					if( instanceMaterialNode.Name == "instance_material" )
					{
						string target = GetIdFromURL( XmlUtils.GetAttribute( instanceMaterialNode, "target" ) );
						string symbol = XmlUtils.GetAttribute( instanceMaterialNode, "symbol" );
						generatedBindedMaterials.Add( target, symbol );
					}
				}
			}

			return true;
		}

		bool ParseNode( Mat4 nodeTransform, XmlNode node )
		{
			string nodeId = XmlUtils.GetAttribute( node, "id" );

			Mat4 currentTransform = nodeTransform;

			foreach( XmlNode childNode in node.ChildNodes )
			{
				if( childNode.Name == "matrix" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 16 )
					{
						Error( "Invalid format of \"matrix\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Mat4 matrix = new Mat4( values );
					currentTransform *= matrix;
					continue;
				}

				if( childNode.Name == "translate" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 3 )
					{
						Error( "Invalid format of \"translate\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Vec3 translate = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					currentTransform *= Mat4.FromTranslate( translate );
					continue;
				}

				if( childNode.Name == "rotate" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 4 )
					{
						Error( "Invalid format of \"rotate\" node. Node \"{0}\".", nodeId );
						return false;
					}

					Vec3 axis = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					Radian angle = new Degree( values[ 3 ] ).InRadians();

					if( axis != Vec3.Zero )
					{
						axis.Normalize();
						float halfAngle = .5f * angle;
						float sin = MathFunctions.Sin( halfAngle );
						float cos = MathFunctions.Cos( halfAngle );
						Quat r = new Quat( axis * sin, cos );
						r.Normalize();

						currentTransform *= r.ToMat3().ToMat4();
					}

					continue;
				}

				if( childNode.Name == "scale" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 3 )
					{
						Error( "Invalid format of \"scale\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Vec3 scale = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					currentTransform *= Mat3.FromScale( scale ).ToMat4();
					continue;
				}

				if( childNode.Name == "node" )
				{
					if( !ParseNode( currentTransform, childNode ) )
						return false;

					continue;
				}

				if( childNode.Name == "instance_geometry" )
				{
					if( !ParseNodeInstanceGeometry( currentTransform, childNode ) )
						return false;

					continue;
				}
			}

			return true;
		}

		bool ParseImage( XmlNode imageNode )
		{
			string imageId = XmlUtils.GetAttribute( imageNode, "id" );

			//find profile node
			XmlNode initFromNode = XmlUtils.FindChildNode( imageNode, "init_from" );
			if( initFromNode == null )
			{
				Error( "Init_from node is not exists for image \"{0}\".", imageId );
				return false;
			}

			generatedImages.Add( imageId, initFromNode.InnerText );
			return true;
		}

		bool ParseImages( XmlNode colladaNode )
		{
			foreach( XmlNode libraryImagesNode in colladaNode.ChildNodes )
			{
				if( libraryImagesNode.Name != "library_images" )
					continue;

				foreach( XmlNode imageNode in libraryImagesNode.ChildNodes )
				{
					if( imageNode.Name != "image" )
						continue;

					if( !ParseImage( imageNode ) )
						return false;
				}
			}

			return true;
		}

		bool ParseMaterial( XmlNode materialNode )
		{
			string materialId = XmlUtils.GetAttribute( materialNode, "id" );
			string materialName = XmlUtils.GetAttribute( materialNode, "name" );

			//find profile node
			XmlNode instanceEffectNode = XmlUtils.FindChildNode( materialNode, "instance_effect" );
			if( instanceEffectNode == null )
			{
				Error( "Instance_effect node is not exists for material \"{0}\".", materialId );
				return false;
			}

			string effectId = GetIdFromURL( XmlUtils.GetAttribute( instanceEffectNode, "url" ) );

			generatedEffects.Add( effectId, materialId );
			return true;
		}

		bool ParseMaterials( XmlNode colladaNode )
		{
			foreach( XmlNode libraryMaterialsNode in colladaNode.ChildNodes )
			{
				if( libraryMaterialsNode.Name != "library_materials" )
					continue;

				foreach( XmlNode materialNode in libraryMaterialsNode.ChildNodes )
				{
					if( materialNode.Name != "material" )
						continue;

					if( !ParseMaterial( materialNode ) )
						return false;
				}
			}

			return true;
		}

		bool ParseTextureBlendingMode( XmlNode textureNode, out string blendingMode,
			 string materialAttributeName )
		{
			blendingMode = "";

			XmlNode extraNode = XmlUtils.FindChildNode( textureNode, "extra" );
			if( extraNode == null )
			{
				return true;
			}

			XmlNode techniqueNode = XmlUtils.FindChildNode( extraNode, "technique" );
			if( techniqueNode == null )
			{
				Error( "Technique node is not exists for texture node of matterial attribute \"{0}\".",
					 materialAttributeName );
				return false;
			}

			XmlNode blendMode = XmlUtils.FindChildNode( techniqueNode, "blend_mode" );
			if( techniqueNode == null )
			{
				Error( "Blend_mode node is not exists for technique node of matterial attribute \"{0}\".",
					 materialAttributeName );
				return false;
			}

			blendingMode = blendMode.InnerText;

			return true;
		}

		bool ParseEffectAttribute( XmlNode materialAttributeNode, ref ColorValue color,
				ref string textureId1, ref string textureCoord1, ref string textureId2,
				ref string textureCoord2, ref string blendingMode2, ref string textureId3,
				ref string textureCoord3, ref string blendingMode3, ref string textureId4,
				ref string textureCoord4, ref string blendingMode4 )
		{
			if( materialAttributeNode.ChildNodes.Count == 0 )
			{
				Error( "Parameter is not exists for matterial attribute \"{0}\".",
					 materialAttributeNode.Name );
				return false;
			}

			int texturesCount = 0;

			foreach( XmlNode parameterNode in materialAttributeNode.ChildNodes )
			{
				if( parameterNode.Name == "texture" )
				{
					string texCoord = XmlUtils.GetAttribute( parameterNode, "texcoord" );
					string textureId = XmlUtils.GetAttribute( parameterNode, "texture" );
					string blendingMode;
					if( !ParseTextureBlendingMode( parameterNode, out blendingMode,
						 materialAttributeNode.Name ) )
						return false;

					switch( texturesCount )
					{
					case 0:
						textureId1 = textureId;
						textureCoord1 = texCoord;
						break;
					case 1:
						textureId2 = textureId;
						textureCoord2 = texCoord;
						blendingMode2 = blendingMode;
						break;
					case 2:
						textureId3 = textureId;
						textureCoord3 = texCoord;
						blendingMode3 = blendingMode;
						break;
					case 3:
						textureId4 = textureId;
						textureCoord4 = texCoord;
						blendingMode4 = blendingMode;
						break;
					}

					texturesCount++;
				}

				if( parameterNode.Name == "color" )
					color = ConvertStringToColorValue( parameterNode.InnerText );
			}

			return true;
		}

		string GetTextureFileName( XmlNode profileNode, string imageId )
		{
			string textureFileName;

			//XSI specified
			foreach( XmlNode newParamNode in profileNode )
			{
				if( newParamNode.Name != "newparam" )
					continue;

				if( XmlUtils.GetAttribute( newParamNode, "sid" ) == imageId )
				{
					XmlNode sampler2D = XmlUtils.FindChildNode( newParamNode, "sampler2D" );
					if( sampler2D == null )
						continue;

					XmlNode source = XmlUtils.FindChildNode( sampler2D, "source" );
					if( source == null )
						continue;

					string surfaceId = source.InnerText;

					foreach( XmlNode newParamSurfaceNode in profileNode )
					{
						if( newParamNode.Name != "newparam" )
							continue;

						if( XmlUtils.GetAttribute( newParamSurfaceNode, "sid" ) == surfaceId )
						{
							XmlNode surface = XmlUtils.FindChildNode( newParamSurfaceNode, "surface" );
							if( surface == null )
								continue;

							XmlNode initFrom = XmlUtils.FindChildNode( surface, "init_from" );
							if( initFrom == null )
								continue;

							if( generatedImages.TryGetValue( initFrom.InnerText, out textureFileName ) )
								return textureFileName;
						}
					}
				}
			}

			//Max\Maya specified
			if( generatedImages.TryGetValue( imageId, out textureFileName ) )
				return textureFileName;

			return "";
		}

		bool ParseEffectNode( XmlNode profileNode, XmlNode effectNode, ref MySceneMaterial material )
		{
			foreach( XmlNode materialAttributeNode in effectNode.ChildNodes )
			{
				if( materialAttributeNode.Name == "diffuse" )
				{
					ColorValue diffuseColor = new ColorValue( 1, 1, 1 );
					string textureId1 = "";
					string textureId2 = "";
					string textureId3 = "";
					string textureId4 = "";
					string textureCoord1 = "";
					string textureCoord2 = "";
					string textureCoord3 = "";
					string textureCoord4 = "";
					string blendingMode2 = "";
					string blendingMode3 = "";
					string blendingMode4 = "";

					if( !ParseEffectAttribute( materialAttributeNode, ref diffuseColor, ref textureId1,
								ref textureCoord1, ref textureId2, ref textureCoord2, ref blendingMode2,
								ref textureId3, ref textureCoord3, ref blendingMode3, ref textureId4,
								ref textureCoord4, ref blendingMode4 ) )
						return false;

					material.DiffuseColor = diffuseColor;

					if( !string.IsNullOrEmpty( textureId1 ) )
					{
						material.Diffuse1Map = GetTextureFileName( profileNode, textureId1 );
						material.Diffuse1TexCoord = textureCoord1;
					}
					if( !string.IsNullOrEmpty( textureId2 ) )
					{
						material.Diffuse2Map = GetTextureFileName( profileNode, textureId2 );
						material.Diffuse2TexCoord = textureCoord2;
						material.Diffuse2MapBlending = blendingMode2;
					}
					if( !string.IsNullOrEmpty( textureId3 ) )
					{
						material.Diffuse3Map = GetTextureFileName( profileNode, textureId3 );
						material.Diffuse3TexCoord = textureCoord3;
						material.Diffuse3MapBlending = blendingMode3;
					}
					if( !string.IsNullOrEmpty( textureId4 ) )
					{
						material.Diffuse4Map = GetTextureFileName( profileNode, textureId4 );
						material.Diffuse4TexCoord = textureCoord4;
						material.Diffuse4MapBlending = blendingMode4;
					}
				}

				if( materialAttributeNode.Name == "specular" )
				{
					ColorValue specularColor = new ColorValue( 0, 0, 0 );
					string textureId1 = "";
					string textureId2 = "";
					string textureId3 = "";
					string textureId4 = "";
					string textureCoord1 = "";
					string textureCoord2 = "";
					string textureCoord3 = "";
					string textureCoord4 = "";
					string blendingMode2 = "";
					string blendingMode3 = "";
					string blendingMode4 = "";

					if( !ParseEffectAttribute( materialAttributeNode, ref specularColor, ref textureId1,
								ref textureCoord1, ref textureId2, ref textureCoord2, ref blendingMode2,
								ref textureId3, ref textureCoord3, ref blendingMode3, ref textureId4,
								ref textureCoord4, ref blendingMode4 ) )
						return false;

					material.SpecularColor = specularColor;

					if( !string.IsNullOrEmpty( textureId1 ) )
					{
						material.SpecularMap = GetTextureFileName( profileNode, textureId1 );
						material.SpecularTexCoord = textureCoord1;
					}
				}

				if( materialAttributeNode.Name == "shininess" )
				{
					XmlNode floatNode = XmlUtils.FindChildNode( materialAttributeNode, "float" );

					if( floatNode == null )
					{
						Error( "\"float\" node is not exists for material attribute \"{0}\".",
							 materialAttributeNode.Name );
						return false;
					}

					float shininess;
					if( !float.TryParse( floatNode.InnerText, out shininess ) )
					{
						Error( "Invalid \"float\" attribute of \"shininess\" node." );
						return false;
					}
					material.Shininess = shininess;
				}

				if( materialAttributeNode.Name == "emission" )
				{
					ColorValue emissionColor = new ColorValue( 0, 0, 0 );
					string textureId1 = "";
					string textureId2 = "";
					string textureId3 = "";
					string textureId4 = "";
					string textureCoord1 = "";
					string textureCoord2 = "";
					string textureCoord3 = "";
					string textureCoord4 = "";
					string blendingMode2 = "";
					string blendingMode3 = "";
					string blendingMode4 = "";

					if( !ParseEffectAttribute( materialAttributeNode, ref emissionColor, ref textureId1,
								ref textureCoord1, ref textureId2, ref textureCoord2, ref blendingMode2,
								ref textureId3, ref textureCoord3, ref blendingMode3, ref textureId4,
								ref textureCoord4, ref blendingMode4 ) )
						return false;

					material.EmissionColor = emissionColor;

					if( !string.IsNullOrEmpty( textureId1 ) )
					{
						material.EmissionMap = GetTextureFileName( profileNode, textureId1 );
						material.EmissionTexCoord = textureCoord1;
					}
				}

				if( materialAttributeNode.Name == "transparent" )
				{
					string opaque = XmlUtils.GetAttribute( materialAttributeNode, "opaque" );
					if( opaque == "RGB_ZERO" )
					{
						material.Opaque = true;
					}

					XmlNode colorNode = XmlUtils.FindChildNode( materialAttributeNode, "color" );
					if( colorNode == null )
					{
						material.TransparentColor = new ColorValue( 1, 1, 1 );
					}
					else
					{
						material.TransparentColor = ConvertStringToColorValue( colorNode.InnerText );
					}
				}

				if( materialAttributeNode.Name == "transparency" )
				{
					XmlNode floatNode = XmlUtils.FindChildNode( materialAttributeNode, "float" );

					if( floatNode == null )
					{
						Error( "\"float\" node is not exists for material attribute \"{0}\".",
							 materialAttributeNode.Name );
						return false;
					}

					float transparency;
					if( !float.TryParse( floatNode.InnerText, out transparency ) )
					{
						Error( "Invalid \"float\" attribute of \"transparency\" node." );
						return false;
					}
					material.Transparency = transparency;
				}
			}

			return true;
		}

		bool ParseEffectExtraNode( XmlNode extraNode, string effectId, ref MySceneMaterial material )
		{
			foreach( XmlNode extraAttributeNode in extraNode.ChildNodes )
			{
				if( extraAttributeNode.Name == "technique" )
				{
					string profile = XmlUtils.GetAttribute( extraAttributeNode, "profile" );
					if( profile == "MAX3D" )
					{
						foreach( XmlNode techniqueAtttributeNode in extraAttributeNode.ChildNodes )
						{
							if( techniqueAtttributeNode.Name == "double_sided" )
							{
								int doubleSided;

								if( !int.TryParse( techniqueAtttributeNode.InnerText, out doubleSided ) )
								{
									Error( "Invalid \"double_sided\" attribute of \"technique\" node " +
										 "for effect {0}.", effectId );
									return false;
								}

								if( doubleSided == 1 )
									material.Culling = false;
							}
						}
					}

				}
			}

			return true;
		}

		bool ParseEffect( XmlNode effectNode )
		{
			string effectId = XmlUtils.GetAttribute( effectNode, "id" );

			MySceneMaterial material;

			string effectName;
			if( !generatedEffects.TryGetValue( effectId, out effectName ) )
			{
				Error( "Generated effect is not exists for effect \"{0}\".", effectId );
				return false;
			}

			string materialName;
			if( !generatedBindedMaterials.TryGetValue( effectName, out materialName ) )
			{
				//Effect exist in library_effects, but not used in visual scenes
				return true;
			}

			if( !generatedMaterials.TryGetValue( materialName, out material ) )
			{
				//Effect exist in library_effects, but not used in geometries
				return true;
			}

			//find profile node
			XmlNode profileNode = XmlUtils.FindChildNode( effectNode, "profile_COMMON" );
			if( profileNode == null )
			{
				Error( "Profile node is not exists for effect \"{0}\".", effectId );
				return false;
			}

			//find technique node
			XmlNode techniqueNode = XmlUtils.FindChildNode( profileNode, "technique" );
			if( techniqueNode == null )
			{
				Error( "Technique node is not exists for effect \"{0}\".", effectId );
				return false;
			}

			if( techniqueNode.ChildNodes.Count == 0 )
			{
				Error( "Material node is not exists for effect \"{0}\".", effectId );
				return false;
			}

			foreach( XmlNode effectMaterialNode in techniqueNode.ChildNodes )
			{
				if( effectMaterialNode.Name == "blinn" || effectMaterialNode.Name == "lambert" ||
					 effectMaterialNode.Name == "phong" )
				{
					if( !ParseEffectNode( profileNode, techniqueNode.ChildNodes[ 0 ], ref material ) )
						return false;

					XmlNode extraNode = XmlUtils.FindChildNode( effectNode, "extra" );
					if( extraNode != null )
					{
						if( !ParseEffectExtraNode( extraNode, effectId, ref material ) )
							return false;
					}

					generatedMaterials[ materialName ] = material;

					return true;
				}
			}

			Error( "Material node is not exists for effect \"{0}\".", effectId );
			return false;
		}

		bool ParseEffects( XmlNode colladaNode )
		{
			foreach( XmlNode libraryEffectsNode in colladaNode.ChildNodes )
			{
				if( libraryEffectsNode.Name != "library_effects" )
					continue;

				foreach( XmlNode effectNode in libraryEffectsNode.ChildNodes )
				{
					if( effectNode.Name != "effect" )
						continue;

					if( !ParseEffect( effectNode ) )
						return false;
				}
			}

			return true;
		}

		bool ParseVisualScenes( XmlNode colladaNode )
		{
			foreach( XmlNode visualScenesNode in colladaNode.ChildNodes )
			{
				if( visualScenesNode.Name != "library_visual_scenes" )
					continue;

				foreach( XmlNode visualSceneNode in visualScenesNode.ChildNodes )
				{
					if( visualSceneNode.Name != "visual_scene" )
						continue;

					foreach( XmlNode nodeNode in visualSceneNode.ChildNodes )
					{
						if( nodeNode.Name != "node" )
							continue;

						Mat4 currentTransform = Mat4.Identity;

						if( globalScale != 1 )
						{
							Mat4 m = Mat3.FromScale( new Vec3( globalScale, globalScale, globalScale ) ).ToMat4();
							currentTransform *= m;
						}

						if( yAxisUp )
						{
							//good for xsi, blender
							Mat4 rotationMatrix = Mat3.FromRotateByX( new Degree( -90 ) ).ToMat4();
							//good for 3dsmax
							//Mat4 rotationMatrix = Mat3.FromRotateByX( new Degree( 90 ) ).ToMat4();

							currentTransform *= rotationMatrix;
						}

						if( !ParseNode( currentTransform, nodeNode ) )
							return false;
					}
				}
			}

			return true;
		}

		bool ParseColladaNode( XmlNode colladaNode )
		{
			//colladaExportingTool, globalScale, yAxisUp
			{
				XmlNode assetNode = XmlUtils.FindChildNode( colladaNode, "asset" );
				if( assetNode != null )
				{
					XmlNode contributorNode = XmlUtils.FindChildNode( assetNode, "contributor" );
					if( contributorNode != null )
					{
						XmlNode authoringToolNode = XmlUtils.FindChildNode( contributorNode,
							 "authoring_tool" );
						if( authoringToolNode != null )
						{
							colladaExportingTool = GetColladaExportingTool( authoringToolNode.InnerText );
						}
					}

					XmlNode upAxisNode = XmlUtils.FindChildNode( assetNode, "up_axis" );
					if( upAxisNode != null )
					{
						if( upAxisNode.InnerText == "Z_UP" )
							yAxisUp = false;
						else if( upAxisNode.InnerText == "X_UP" )
						{
							Error( "X up axis is not supported." );
							return false;
						}
					}

					XmlNode unitNode = XmlUtils.FindChildNode( assetNode, "unit" );
					if( unitNode != null )
					{
						string meterStr = XmlUtils.GetAttribute( unitNode, "meter" );
						if( !string.IsNullOrEmpty( meterStr ) )
						{
							string fixedStr = meterStr.Replace( ',', '.' );

							if( !float.TryParse( fixedStr, out globalScale ) )
							{
								Error( "Invalid \"meter\" attribute of \"unit\" node." );
								return false;
							}
						}
					}
				}
			}

			//library_geometries
			if( !ParseGeometries( colladaNode ) )
				return false;

			//library_visual_scenes
			if( !ParseVisualScenes( colladaNode ) )
				return false;

			//library_images
			if( !ParseImages( colladaNode ) )
				return false;

			//library_materials
			if( !ParseMaterials( colladaNode ) )
				return false;

			//library_effects
			if( !ParseEffects( colladaNode ) )
				return false;

			return true;
		}

		void ClearTemporaryFields()
		{
			currentFileName = null;
			currentMesh = null;
			globalScale = 1;
			yAxisUp = true;
			colladaExportingTool = ColladaExportingTools.Unknown;
			generatedMaterials = null;
			generatedGeometries = null;
			generatedSubMeshes = null;
			generatedImages = null;
			generatedEffects = null;
			generatedBindedMaterials = null;
		}

		string GetUniqueMaterialName( string prefix )
		{
			int counter = 1;
			while( true )
			{
				string name = prefix;
				if( counter != 1 )
					name += counter.ToString();

				if( MaterialManager.Instance.GetByName( name ) == null &&
					!generatedMaterials.ContainsKey( name ) )
				{
					return name;
				}

				counter++;
			}
		}

		void MakeUniqueNamesForMaterials()
		{
			List<MySceneMaterial> oldList = new List<MySceneMaterial>( generatedMaterials.Values );

			generatedMaterials = new Dictionary<string, MySceneMaterial>();
			foreach( MySceneMaterial material in oldList )
			{
				string uniqueMaterialName = GetUniqueMaterialName( material.Name );

				material.Name = uniqueMaterialName;
				generatedMaterials[ uniqueMaterialName ] = material;
			}
		}

		bool DoExportMaterials()
		{
			string colladaFileDirectory = Path.GetDirectoryName( currentFileName );

			foreach( MySceneMaterial material in generatedMaterials.Values )
			{
				ShaderBaseMaterial shaderBaseMaterial = (ShaderBaseMaterial)
					 HighLevelMaterialManager.Instance.CreateMaterial( material.Name, "ShaderBaseMaterial" );
				if( shaderBaseMaterial == null )
				{
					Error( "Unable to create ShaderBaseMaterial material with name \"{0}\".", material.Name );
					return false;
				}

				currentMesh.AttachDependentMaterial( shaderBaseMaterial );

				//blending
				float alpha = material.GetAlpha();
				if( alpha < 1 )
					shaderBaseMaterial.Blending = ShaderBaseMaterial.MaterialBlendingTypes.AlphaBlend;

				//culling
				if( !material.Culling )
					shaderBaseMaterial.Culling = false;

				//diffuseColor
				if( material.DiffuseColor.Red < 1 || material.DiffuseColor.Green < 1 ||
					  material.DiffuseColor.Blue < 1 || alpha < 1 )
				{
					Vec3 color = new Vec3( material.DiffuseColor.Red, material.DiffuseColor.Green,
						 material.DiffuseColor.Blue );
					float power = Math.Max( Math.Max( color.X, color.Y ), color.Z );
					if( power > 1 )
						color /= power;

					if( alpha < 1 )
						shaderBaseMaterial.DiffuseColor = new ColorValue( color.X, color.Y, color.Z, alpha );
					else
						shaderBaseMaterial.DiffuseColor = new ColorValue( color.X, color.Y, color.Z );

					if( power > 1 )
						shaderBaseMaterial.DiffusePower = power;
				}

				//diffuseMap1
				if( !string.IsNullOrEmpty( material.Diffuse1Map ) )
				{
					shaderBaseMaterial.Diffuse1Map.Texture =
						 GetVirtualPath( colladaFileDirectory, material.Diffuse1Map );
					shaderBaseMaterial.Diffuse1Map.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
						 material.Diffuse1TexCoord );
				}

				//diffuseMap2
				if( !string.IsNullOrEmpty( material.Diffuse2Map ) )
				{
					shaderBaseMaterial.Diffuse2Map.Texture =
						 GetVirtualPath( colladaFileDirectory, material.Diffuse2Map );
					shaderBaseMaterial.Diffuse2Map.Blending = GetMapBlendingType(
						 material.Diffuse2MapBlending );
					shaderBaseMaterial.Diffuse2Map.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
						 material.Diffuse2TexCoord );
				}

				//diffuseMap3
				if( !string.IsNullOrEmpty( material.Diffuse3Map ) )
				{
					shaderBaseMaterial.Diffuse3Map.Texture =
						 GetVirtualPath( colladaFileDirectory, material.Diffuse3Map );
					shaderBaseMaterial.Diffuse3Map.Blending = GetMapBlendingType(
						 material.Diffuse3MapBlending );
					shaderBaseMaterial.Diffuse3Map.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
						 material.Diffuse3TexCoord );
				}

				//diffuseMap4
				if( !string.IsNullOrEmpty( material.Diffuse4Map ) )
				{
					shaderBaseMaterial.Diffuse4Map.Texture =
						 GetVirtualPath( colladaFileDirectory, material.Diffuse4Map );
					shaderBaseMaterial.Diffuse4Map.Blending = GetMapBlendingType(
						 material.Diffuse4MapBlending );
					shaderBaseMaterial.Diffuse4Map.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
						 material.Diffuse4TexCoord );
				}

				//specularMap
				if( !string.IsNullOrEmpty( material.SpecularMap ) )
				{
					shaderBaseMaterial.SpecularMap.Texture =
						 GetVirtualPath( colladaFileDirectory, material.SpecularMap );
					shaderBaseMaterial.SpecularMap.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
								material.SpecularTexCoord );
				}

				//specularColor
				if( material.SpecularColor.Red > 0 || material.SpecularColor.Green > 0 ||
					 material.SpecularColor.Blue > 0 )
				{
					shaderBaseMaterial.SpecularColor = material.SpecularColor;
				}

				//specularShininess
				if( !string.IsNullOrEmpty( material.SpecularMap ) || material.SpecularColor.Red > 0 ||
					material.SpecularColor.Green > 0 || material.SpecularColor.Blue > 0 )
				{
					//conversion can be configured better.

					switch( colladaExportingTool )
					{
					case ColladaExportingTools.FBX:
					case ColladaExportingTools.NeoAxis:
						shaderBaseMaterial.SpecularShininess = material.Shininess * 3;
						break;
					default:
						shaderBaseMaterial.SpecularShininess = material.Shininess;
						break;
					}
				}

				//emissionMap
				if( !string.IsNullOrEmpty( material.EmissionMap ) )
				{
					shaderBaseMaterial.EmissionMap.Texture =
						 GetVirtualPath( colladaFileDirectory, material.EmissionMap );
					shaderBaseMaterial.EmissionMap.TexCoord = GetNeoAxisTexCoordByColladaTexCoord(
								material.EmissionTexCoord );
				}

				//emissionColor
				if( material.EmissionColor.Red > 0 || material.EmissionColor.Green > 0 ||
					 material.EmissionColor.Blue > 0 )
				{
					shaderBaseMaterial.EmissionColor = material.EmissionColor;
				}

				shaderBaseMaterial.UpdateBaseMaterial();
			}

			return true;
		}

		protected override bool Load( string virtualFileName, Mesh mesh )
		{
			currentFileName = virtualFileName;
			currentMesh = mesh;
			globalScale = 1;
			yAxisUp = true;
			colladaExportingTool = ColladaExportingTools.Unknown;
			generatedMaterials = new Dictionary<string, MySceneMaterial>();
			generatedGeometries = new Dictionary<string, GeometryItem>();
			generatedSubMeshes = new List<MySceneSubMesh>();
			generatedImages = new Dictionary<string, string>();
			generatedEffects = new Dictionary<string, string>();
			generatedBindedMaterials = new Dictionary<string, string>();

			//load file
			XmlNode colladaNode;
			{
				try
				{
					using( Stream stream = VirtualFile.Open( virtualFileName ) )
					{
						string fileText = new StreamReader( stream ).ReadToEnd();
						colladaNode = XmlUtils.LoadFromText( fileText );
					}
				}
				catch( Exception ex )
				{
					Error( ex.Message );
					return false;
				}
			}

			//parse
			if( !ParseColladaNode( colladaNode ) )
			{
				ClearTemporaryFields();
				return false;
			}

			MakeUniqueNamesForMaterials();

			if( !DoExportMaterials() )
			{
				ClearTemporaryFields();
				return false;
			}

			//generate mesh
			const bool tangents = true;
			const bool edgeList = true;
			const bool allowMergeSubMeshes = true;
			MeshConstructor meshConstructor = new MeshConstructor( tangents, edgeList, allowMergeSubMeshes );

			MyMeshSceneObject meshSceneObject = new MyMeshSceneObject( generatedSubMeshes.ToArray() );

			if( !meshConstructor.DoExport( meshSceneObject, mesh ) )
			{
				ClearTemporaryFields();
				return false;
			}

			ClearTemporaryFields();

			return true;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////

		float[] ConvertVertexBufferInfoToFloatArray( VertexBufferBinding bufferBinding,
			 VertexElement element, int elementsCount, int valuesPerElement )
		{
			float[] floatArray = new float[ elementsCount * valuesPerElement ];

			HardwareVertexBuffer buffer = bufferBinding.GetBuffer( element.Source );
			unsafe
			{
				byte* pointer = (byte*)buffer.Lock( HardwareBuffer.LockOptions.Normal ).ToPointer();
				byte* p = pointer + element.Offset;
				for( int n = 0; n < elementsCount; n++ )
				{
					for( int i = 0; i < valuesPerElement; i++ )
					{
						floatArray[ n * valuesPerElement + i ] = *(float*)( p + sizeof( float ) * i );
					}
					p += buffer.VertexSizeInBytes;
				}
			}
			buffer.Unlock();

			return floatArray;
		}

		void ConvertMeshVerticesInfoToFloatArrays( Mesh mesh, out float[] floatVerticesArray,
			 out float[] floatNormalsArray, out float[] floatUV0Array, out float[] floatUV1Array,
			 out float[] floatUV2Array, out float[] floatUV3Array, out int trianglesCount )
		{
			floatUV0Array = null;
			floatUV1Array = null;
			floatUV2Array = null;
			floatUV3Array = null;

			List<float> floatVerticesList = new List<float>();
			List<float> floatNormalsList = new List<float>();
			List<float> floatUV0List = new List<float>();
			List<float> floatUV1List = new List<float>();
			List<float> floatUV2List = new List<float>();
			List<float> floatUV3List = new List<float>();
			trianglesCount = 0;

			foreach( SubMesh subMesh in mesh.SubMeshes )
			{
				VertexData vertexData = subMesh.UseSharedVertices ?
					 mesh.SharedVertexData : subMesh.VertexData;
				IndexData indexData = subMesh.IndexData;
				trianglesCount += indexData.IndexCount;
				int vertexCount = vertexData.VertexCount;

				VertexDeclaration declaration = vertexData.VertexDeclaration;
				VertexBufferBinding bufferBinding = vertexData.VertexBufferBinding;

				//positions
				{
					int positionIndex = declaration.FindElementBySemantic( VertexElementSemantic.Position );
					VertexElement positionElement = declaration.Elements[ positionIndex ];

					float[] positions = ConvertVertexBufferInfoToFloatArray( bufferBinding, positionElement,
						 vertexCount, 3 );

					foreach( float position in positions )
					{
						floatVerticesList.Add( position );
					}
				}

				//normals
				{
					int normalIndex = declaration.FindElementBySemantic( VertexElementSemantic.Normal );
					VertexElement normalElement = declaration.Elements[ normalIndex ];

					float[] normals = ConvertVertexBufferInfoToFloatArray( bufferBinding, normalElement,
						 vertexCount, 3 );

					foreach( float normal in normals )
					{
						floatNormalsList.Add( normal );
					}
				}

				//UVs
				{
					foreach( VertexElement element in declaration.Elements )
					{
						if( element.Semantic == VertexElementSemantic.TextureCoordinates )
						{
							float[] floatUVArray = ConvertVertexBufferInfoToFloatArray( bufferBinding,
											element, vertexCount, 2 );
							switch( element.Index )
							{
							case 0:
								for( int i = 0; i < floatUVArray.Length; i += 2 )
								{
									floatUV0List.Add( floatUVArray[ i ] );
									floatUV0List.Add( 1 - floatUVArray[ i + 1 ] );
								}
								break;
							case 1:
								for( int i = 0; i < floatUVArray.Length; i += 2 )
								{
									floatUV0List.Add( floatUVArray[ i ] );
									floatUV0List.Add( 1 - floatUVArray[ i + 1 ] );
								}
								break;
							case 2:
								for( int i = 0; i < floatUVArray.Length; i += 2 )
								{
									floatUV0List.Add( floatUVArray[ i ] );
									floatUV0List.Add( 1 - floatUVArray[ i + 1 ] );
								}
								break;
							case 3:
								for( int i = 0; i < floatUVArray.Length; i += 2 )
								{
									floatUV0List.Add( floatUVArray[ i ] );
									floatUV0List.Add( 1 - floatUVArray[ i + 1 ] );
								}
								break;
							}
						}
					}
				}
			}

			floatVerticesArray = floatVerticesList.ToArray();
			floatNormalsArray = floatNormalsList.ToArray();

			if( floatUV0List.Count > 0 )
				floatUV0Array = floatUV0List.ToArray();
			if( floatUV1List.Count > 0 )
				floatUV1Array = floatUV1List.ToArray();
			if( floatUV2List.Count > 0 )
				floatUV2Array = floatUV2List.ToArray();
			if( floatUV2List.Count > 0 )
				floatUV2Array = floatUV2List.ToArray();
		}

		List<string> GetMeshMaterialNamesList( Mesh mesh )
		{
			List<string> materialNamesList = new List<string>();

			foreach( SubMesh subMesh in mesh.SubMeshes )
			{
				if( !string.IsNullOrEmpty( subMesh.MaterialName ) )
					materialNamesList.Add( subMesh.MaterialName );
			}

			return materialNamesList;
		}

		Dictionary<string, string> GetGeneratedImages( Mesh mesh )
		{
			Dictionary<string, string> generatedImages = new Dictionary<string, string>();

			int filesCount = 0;

			foreach( SubMesh subMesh in mesh.SubMeshes )
			{
				string materialName = subMesh.MaterialName;
				if( !string.IsNullOrEmpty( materialName ) )
				{
					ShaderBaseMaterial material = 
						HighLevelMaterialManager.Instance.GetMaterialByName( materialName ) as ShaderBaseMaterial;

					if( material != null )
					{
						if( !string.IsNullOrEmpty( material.EmissionMap.Texture ) )
						{
							string fileName = material.EmissionMap.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}

						if( !string.IsNullOrEmpty( material.Diffuse1Map.Texture ) )
						{
							string fileName = material.Diffuse1Map.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}

						if( !string.IsNullOrEmpty( material.Diffuse2Map.Texture ) )
						{
							string fileName = material.Diffuse2Map.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}

						if( !string.IsNullOrEmpty( material.Diffuse3Map.Texture ) )
						{
							string fileName = material.Diffuse3Map.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}

						if( !string.IsNullOrEmpty( material.Diffuse4Map.Texture ) )
						{
							string fileName = material.Diffuse4Map.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}

						if( !string.IsNullOrEmpty( material.SpecularMap.Texture ) )
						{
							string fileName = material.SpecularMap.Texture;

							if( !generatedImages.ContainsKey( fileName ) )
							{
								string imageName = "file" + filesCount.ToString();
								generatedImages.Add( fileName, imageName );
								filesCount++;
							}
						}
					}
				}
			}

			return generatedImages;
		}

		string ConvertFloatArrayToString( float[] floatArray )
		{
			string result = "";

			if( floatArray.Length > 0 )
				result = floatArray[ 0 ].ToString();

			for( int i = 1; i < floatArray.Length; i++ )
				result += " " + floatArray[ i ].ToString( "f6" );

			return result;
		}

		string ConvertIntArrayToString( int[] intArray )
		{
			string result = "";

			if( intArray.Length > 0 )
				result = intArray[ 0 ].ToString();

			for( int i = 1; i < intArray.Length; i++ )
				result += " " + intArray[ i ].ToString();

			return result;
		}

		bool IsEqualFloats( float float1, float float2 )
		{
			return Math.Abs( float1 - float2 ) < 0.0000001f;
		}

		bool IsEqualElements( float[] floatArray, int arrayStartIndex, List<float> floatList,
			 int listStartIndex, int stride )
		{
			bool isEqual = true;

			for( int i = 0; isEqual && i < stride; i++ )
			{
				isEqual = IsEqualFloats( floatArray[ arrayStartIndex * stride + i ],
					 floatList[ listStartIndex * stride + i ] );
			}

			return isEqual;
		}

		void CompressFloatArray( float[] floatArray, int stride, out float[] compressedFloatArray,
			 out int[] floatArrayToCompressedFloatArrayIndices )
		{
			int arrayLength = floatArray.Length;
			int elementsCount = arrayLength / stride;

			List<float> compressedFloatList = new List<float>();
			floatArrayToCompressedFloatArrayIndices = new int[ arrayLength ];

			for( int i = 0; i < elementsCount; i++ )
			{
				int foundedIndex = -1;
				for( int j = 0; j < compressedFloatList.Count / stride; j++ )
				{
					if( IsEqualElements( floatArray, i, compressedFloatList, j, stride ) )
					{
						foundedIndex = j;
						break;
					}
				}

				if( foundedIndex > -1 )
				{
					floatArrayToCompressedFloatArrayIndices[ i ] = foundedIndex;
				}
				else
				{
					floatArrayToCompressedFloatArrayIndices[ i ] = compressedFloatList.Count / stride;

					for( int j = 0; j < stride; j++ )
						compressedFloatList.Add( floatArray[ i * stride + j ] );
				}
			}

			compressedFloatArray = compressedFloatList.ToArray();
		}

		string GetMapBlendingType( ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes blendingMode )
		{
			if( blendingMode == ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes.Add )
				return "ADD";

			if( blendingMode == ShaderBaseMaterial.DiffuseMapItem.MapBlendingTypes.AlphaBlend )
				return "BLEND";

			return "MODULATE";
		}

		string GetTexCoordByIndex( ShaderBaseMaterial.TexCoordIndexes texCoord )
		{
			switch( texCoord )
			{
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord0:
				return "CHANNEL0";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord1:
				return "CHANNEL1";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord2:
				return "CHANNEL2";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord3:
				return "CHANNEL3";
			}

			return "CHANNEL0";
		}

		string GetTexCoordNumberByIndex( ShaderBaseMaterial.TexCoordIndexes texCoord )
		{
			switch( texCoord )
			{
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord0:
				return "0";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord1:
				return "1";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord2:
				return "2";
			case ShaderBaseMaterial.TexCoordIndexes.TexCoord3:
				return "3";
			}

			return "0";
		}

		void WriteMatrix( XmlTextWriter xmlTextWriter, string sid, Mat4 matrix )
		{
			xmlTextWriter.WriteStartElement( "matrix" );
			xmlTextWriter.WriteAttributeString( "sid", sid );
			xmlTextWriter.WriteString( matrix.ToString() );
			xmlTextWriter.WriteEndElement();
		}

		void WriteFloatSource( XmlTextWriter xmlTextWriter, float[] floatArray, string name,
			 int count, string stride, string[] parameters )
		{
			xmlTextWriter.WriteStartElement( "source" );
			xmlTextWriter.WriteAttributeString( "id", name );

			//float_array
			{
				xmlTextWriter.WriteStartElement( "float_array" );
				xmlTextWriter.WriteAttributeString( "id", name + "-array" );
				xmlTextWriter.WriteAttributeString( "count", floatArray.Length.ToString() );
				xmlTextWriter.WriteString( ConvertFloatArrayToString( floatArray ) );
				xmlTextWriter.WriteEndElement();
			}

			//technique_common
			{
				xmlTextWriter.WriteStartElement( "technique_common" );

				//accessor
				{
					xmlTextWriter.WriteStartElement( "accessor" );
					xmlTextWriter.WriteAttributeString( "source", "#" + name + "-array" );
					xmlTextWriter.WriteAttributeString( "count", count.ToString() );
					if( !string.IsNullOrEmpty( stride ) )
						xmlTextWriter.WriteAttributeString( "stride", stride );

					for( int i = 0; i < parameters.Length; i++ )
					{
						//param
						{
							xmlTextWriter.WriteStartElement( "param" );
							xmlTextWriter.WriteAttributeString( "name", parameters[ i ] );
							xmlTextWriter.WriteAttributeString( "type", "float" );
							xmlTextWriter.WriteEndElement();
						}
					}

					xmlTextWriter.WriteEndElement();//accessor
				}

				xmlTextWriter.WriteEndElement();//technique_common
			}

			xmlTextWriter.WriteEndElement();//source
		}

		void WriteInput( XmlTextWriter xmlTextWriter, string semantic, string offset, string source )
		{
			xmlTextWriter.WriteStartElement( "input" );
			xmlTextWriter.WriteAttributeString( "semantic", semantic );
			if( !string.IsNullOrEmpty( offset ) )
				xmlTextWriter.WriteAttributeString( "offset", offset );
			xmlTextWriter.WriteAttributeString( "source", source );
			xmlTextWriter.WriteEndElement();
		}

		bool WriteAsset( XmlTextWriter xmlTextWriter )
		{
			//asset
			{
				xmlTextWriter.WriteStartElement( "asset" );

				//contributor
				xmlTextWriter.WriteStartElement( "contributor" );
				xmlTextWriter.WriteElementString( "authoring_tool",
					"NeoAxis Engine " + EngineVersionInformation.Version );
				xmlTextWriter.WriteEndElement();

				//created
				xmlTextWriter.WriteElementString( "created",
					DateTime.UtcNow.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'" ) );

				//modified
				xmlTextWriter.WriteElementString( "modified",
					DateTime.UtcNow.ToString( "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'" ) );

				//revision
				xmlTextWriter.WriteElementString( "revision", "1.4.1" );

				//unit
				xmlTextWriter.WriteStartElement( "unit" );
				xmlTextWriter.WriteAttributeString( "meter", "1" );
				xmlTextWriter.WriteAttributeString( "name", "meter" );
				xmlTextWriter.WriteEndElement();

				//up_axis
				xmlTextWriter.WriteElementString( "up_axis", "Z_UP" );

				xmlTextWriter.WriteEndElement();//asset
			}

			return true;
		}

		bool WriteLibraryImages( XmlTextWriter xmlTextWriter, Dictionary<string, string> generatedImages )
		{
			//library_images
			{
				xmlTextWriter.WriteStartElement( "library_images" );

				foreach( string fileName in generatedImages.Keys )
				{
					string imageName = generatedImages[ fileName ];

					//image
					{
						xmlTextWriter.WriteStartElement( "image" );
						xmlTextWriter.WriteAttributeString( "id", imageName + "-image" );
						xmlTextWriter.WriteAttributeString( "name", imageName );

						//init_from
						xmlTextWriter.WriteElementString( "init_from", "file://" +
							 VirtualFileSystem.GetRealPathByVirtual( fileName ) );

						xmlTextWriter.WriteEndElement();
					}
				}

				xmlTextWriter.WriteEndElement();//library_images
			}

			return true;
		}

		bool WriteLibraryMaterials( XmlTextWriter xmlTextWriter, List<string> meshMaterialNamesList )
		{
			//library_materials
			{
				xmlTextWriter.WriteStartElement( "library_materials" );

				foreach( string materialName in meshMaterialNamesList )
				{
					//material
					{
						xmlTextWriter.WriteStartElement( "material" );
						xmlTextWriter.WriteAttributeString( "id", materialName );
						xmlTextWriter.WriteAttributeString( "name", materialName );

						//instance_effect
						{
							xmlTextWriter.WriteStartElement( "instance_effect" );
							xmlTextWriter.WriteAttributeString( "url", "#" + materialName + "-fx" );
							xmlTextWriter.WriteEndElement();
						}

						xmlTextWriter.WriteEndElement();
					}
				}

				xmlTextWriter.WriteEndElement();//library_materials
			}

			return true;
		}

		void WriteFloatNode( XmlTextWriter xmlTextWriter, float value, string sid )
		{
			string floatString = value.ToString( "f6" );

			//float
			xmlTextWriter.WriteStartElement( "float" );
			xmlTextWriter.WriteAttributeString( "sid", sid );
			xmlTextWriter.WriteString( floatString );
			xmlTextWriter.WriteEndElement();
		}

		void WriteColorNode( XmlTextWriter xmlTextWriter, ColorValue color, string sid )
		{
			string colorString = string.Format( "{0:f6} {1:f6} {2:f6} {3:f6}", color.Red, color.Green,
				 color.Blue, color.Alpha );

			//color
			xmlTextWriter.WriteStartElement( "color" );
			xmlTextWriter.WriteAttributeString( "sid", sid );
			xmlTextWriter.WriteString( colorString );
			xmlTextWriter.WriteEndElement();
		}

		void WriteTextureAttribute( XmlTextWriter xmlTextWriter, string imageName, string texCoord,
			 string texCoordNumber, string blendMode )
		{
			//texture
			{
				xmlTextWriter.WriteStartElement( "texture" );
				xmlTextWriter.WriteAttributeString( "texture", imageName + "-image" );
				xmlTextWriter.WriteAttributeString( "texcoord", texCoord );

				//extra
				{
					xmlTextWriter.WriteStartElement( "extra" );

					//technique
					{
						xmlTextWriter.WriteStartElement( "technique" );
						xmlTextWriter.WriteAttributeString( "profile", "MAYA" );

						//wrapU
						{
							xmlTextWriter.WriteStartElement( "wrapU" );
							xmlTextWriter.WriteAttributeString( "sid", "wrapU" + texCoordNumber );
							xmlTextWriter.WriteString( "TRUE" );
							xmlTextWriter.WriteEndElement();
						}

						//wrapV
						{
							xmlTextWriter.WriteStartElement( "wrapV" );
							xmlTextWriter.WriteAttributeString( "sid", "wrapV" + texCoordNumber );
							xmlTextWriter.WriteString( "TRUE" );
							xmlTextWriter.WriteEndElement();
						}

						//blend_mode
						xmlTextWriter.WriteElementString( "blend_mode", blendMode );

						xmlTextWriter.WriteEndElement();//technique
					}

					xmlTextWriter.WriteEndElement();//extra
				}

				xmlTextWriter.WriteEndElement();//texture
			}
		}

		bool WriteEffectAttribute( XmlTextWriter xmlTextWriter, ShaderBaseMaterial.MapItem map,
			 ColorValue color, string sid, Dictionary<string, string> generatedImages )
		{
			xmlTextWriter.WriteStartElement( sid );

			if( !string.IsNullOrEmpty( map.Texture ) )
			{
				string imageName;
				if( !generatedImages.TryGetValue( map.Texture, out imageName ) )
				{
					Error( "Image name wasn't generated for texture \"{0}\"", map.Texture );
					return false;
				}

				//texture
				WriteTextureAttribute( xmlTextWriter, imageName, GetTexCoordByIndex( map.TexCoord ),
					 GetTexCoordNumberByIndex( map.TexCoord ), "ADD" );
			}
			else
			{
				WriteColorNode( xmlTextWriter, color, sid );
			}

			xmlTextWriter.WriteEndElement();


			return true;
		}

		bool WriteDiffuseEffectAttribute( XmlTextWriter xmlTextWriter, ShaderBaseMaterial.MapItem mapItem0,
			 ShaderBaseMaterial.DiffuseMapItem mapItem1, ShaderBaseMaterial.DiffuseMapItem mapItem2,
			 ShaderBaseMaterial.DiffuseMapItem mapItem3, ColorValue color, string sid,
			 Dictionary<string, string> generatedImages )
		{
			bool isTextureUsed = false;

			xmlTextWriter.WriteStartElement( sid );

			//Diffuse1Map
			if( !string.IsNullOrEmpty( mapItem0.Texture ) )
			{
				string imageName;
				if( !generatedImages.TryGetValue( mapItem0.Texture, out imageName ) )
				{
					Error( "Image name wasn't generated for texture \"{0}\"", mapItem0.Texture );
					return false;
				}

				//texture
				WriteTextureAttribute( xmlTextWriter, imageName, GetTexCoordByIndex( mapItem0.TexCoord ),
					 GetTexCoordNumberByIndex( mapItem0.TexCoord ), "ADD" );

				isTextureUsed = true;
			}

			//Diffuse2Map
			if( !string.IsNullOrEmpty( mapItem1.Texture ) )
			{
				string imageName;
				if( !generatedImages.TryGetValue( mapItem1.Texture, out imageName ) )
				{
					Error( "Image name wasn't generated for texture \"{0}\"", mapItem1.Texture );
					return false;
				}

				//texture
				WriteTextureAttribute( xmlTextWriter, imageName, GetTexCoordByIndex( mapItem1.TexCoord ),
					 GetTexCoordNumberByIndex( mapItem1.TexCoord ), GetMapBlendingType( mapItem1.Blending ) );

				isTextureUsed = true;
			}

			//Diffuse3Map
			if( !string.IsNullOrEmpty( mapItem2.Texture ) )
			{
				string imageName;
				if( !generatedImages.TryGetValue( mapItem2.Texture, out imageName ) )
				{
					Error( "Image name wasn't generated for texture \"{0}\"", mapItem2.Texture );
					return false;
				}

				//texture
				WriteTextureAttribute( xmlTextWriter, imageName, GetTexCoordByIndex( mapItem2.TexCoord ),
					 GetTexCoordNumberByIndex( mapItem2.TexCoord ), GetMapBlendingType( mapItem2.Blending ) );

				isTextureUsed = true;
			}

			//Diffuse4Map
			if( !string.IsNullOrEmpty( mapItem3.Texture ) )
			{
				string imageName;
				if( !generatedImages.TryGetValue( mapItem3.Texture, out imageName ) )
				{
					Error( "Image name wasn't generated for texture \"{0}\"", mapItem3.Texture );
					return false;
				}

				//texture
				WriteTextureAttribute( xmlTextWriter, imageName, GetTexCoordByIndex( mapItem3.TexCoord ),
					 GetTexCoordNumberByIndex( mapItem3.TexCoord ), GetMapBlendingType( mapItem3.Blending ) );

				isTextureUsed = true;
			}

			if( !isTextureUsed )
				WriteColorNode( xmlTextWriter, color, sid );

			xmlTextWriter.WriteEndElement();

			return true;
		}

		bool WriteEffectInfo( XmlTextWriter xmlTextWriter, string materialName,
			 Dictionary<string, string> generatedImages )
		{
			ShaderBaseMaterial material = null;
			HighLevelMaterial highLevelMaterial =
				 HighLevelMaterialManager.Instance.GetMaterialByName( materialName );
			if( highLevelMaterial != null )
				material = highLevelMaterial as ShaderBaseMaterial;

			//phong
			{
				xmlTextWriter.WriteStartElement( "phong" );

				if( material != null )
				{
					//emission
					if( !WriteEffectAttribute( xmlTextWriter, material.EmissionMap, material.EmissionColor,
						"emission", generatedImages ) )
					{
						return false;
					}

					//ambient
					xmlTextWriter.WriteStartElement( "ambient" );
					WriteColorNode( xmlTextWriter, new ColorValue( 0, 0, 0, 1 ), "ambient" );
					xmlTextWriter.WriteEndElement();

					//diffuse
					if( !WriteDiffuseEffectAttribute( xmlTextWriter, material.Diffuse1Map,
						 material.Diffuse2Map, material.Diffuse3Map, material.Diffuse3Map,
						 material.DiffuseColor, "diffuse", generatedImages ) )
					{
						return false;
					}

					//specular
					if( !WriteEffectAttribute( xmlTextWriter, material.SpecularMap, material.SpecularColor,
						 "specular", generatedImages ) )
					{
						return false;
					}

					//shininess
					xmlTextWriter.WriteStartElement( "shininess" );
					WriteFloatNode( xmlTextWriter, material.SpecularShininess / 3, "shininess" );
					xmlTextWriter.WriteEndElement();

					//reflective
					xmlTextWriter.WriteStartElement( "reflective" );
					WriteColorNode( xmlTextWriter, new ColorValue( 0, 0, 0, 1 ), "reflective" );
					xmlTextWriter.WriteEndElement();

					//reflectivity
					xmlTextWriter.WriteStartElement( "reflectivity" );
					WriteFloatNode( xmlTextWriter, 1, "reflectivity" );
					xmlTextWriter.WriteEndElement();

					//transparent
					xmlTextWriter.WriteStartElement( "transparent" );
					WriteColorNode( xmlTextWriter, new ColorValue( 1, 1, 1, 1 ), "transparent" );
					xmlTextWriter.WriteEndElement();

					//transparency
					xmlTextWriter.WriteStartElement( "transparency" );
					WriteFloatNode( xmlTextWriter, 1 - material.DiffuseColor.Alpha, "transparency" );
					xmlTextWriter.WriteEndElement();
				}

				xmlTextWriter.WriteEndElement();//phong
			}

			return true;
		}

		bool WriteLibraryEffects( XmlTextWriter xmlTextWriter, List<string> meshMaterialNamesList,
			 Dictionary<string, string> generatedImages )
		{
			//library_effects
			{
				xmlTextWriter.WriteStartElement( "library_effects" );

				foreach( string materialName in meshMaterialNamesList )
				{
					//effect
					{
						xmlTextWriter.WriteStartElement( "effect" );
						xmlTextWriter.WriteAttributeString( "id", materialName + "-fx" );
						xmlTextWriter.WriteAttributeString( "name", materialName );

						//profile_COMMON
						{
							xmlTextWriter.WriteStartElement( "profile_COMMON" );

							//technique
							{
								xmlTextWriter.WriteStartElement( "technique" );
								xmlTextWriter.WriteAttributeString( "sid", "standard" );

								//phong
								if( !WriteEffectInfo( xmlTextWriter, materialName, generatedImages ) )
									return false;

								xmlTextWriter.WriteEndElement();//technique
							}

							xmlTextWriter.WriteEndElement();//profile_COMMON
						}

						xmlTextWriter.WriteEndElement();//effect
					}
				}

				xmlTextWriter.WriteEndElement();//library_effects
			}

			return true;
		}

		bool WriteLibraryGeometries( XmlTextWriter xmlTextWriter, Mesh mesh )
		{
			//library_geometries
			{
				xmlTextWriter.WriteStartElement( "library_geometries" );

				//geometry
				{
					string meshName = Path.GetFileNameWithoutExtension( mesh.Name );

					xmlTextWriter.WriteStartElement( "geometry" );
					xmlTextWriter.WriteAttributeString( "id", meshName + "-lib" );
					xmlTextWriter.WriteAttributeString( "name", meshName + "Mesh" );

					//mesh
					{
						xmlTextWriter.WriteStartElement( "mesh" );

						float[] floatPositionsArray;
						float[] floatNormalsArray;
						float[] floatUV0Array;
						float[] floatUV1Array;
						float[] floatUV2Array;
						float[] floatUV3Array;
						int trianglesCount;
						ConvertMeshVerticesInfoToFloatArrays( mesh, out floatPositionsArray,
							 out floatNormalsArray, out floatUV0Array, out floatUV1Array, out floatUV2Array,
							  out floatUV3Array, out trianglesCount );

						float[] compressedPositionsArray;
						float[] compressedNormalsArray;
						float[] compressedUV0Array = null;
						float[] compressedUV1Array = null;
						float[] compressedUV2Array = null;
						float[] compressedUV3Array = null;

						int[] positionsArrayToCompressedPositionsArrayIndices;
						int[] normalsArrayToCompressedNormalsArrayIndices;
						int[] UV0ArrayToCompressedUV0ArrayIndices = null;
						int[] UV1ArrayToCompressedUV1ArrayIndices = null;
						int[] UV2ArrayToCompressedUV2ArrayIndices = null;
						int[] UV3ArrayToCompressedUV3ArrayIndices = null;

						CompressFloatArray( floatPositionsArray, 3, out compressedPositionsArray,
							 out positionsArrayToCompressedPositionsArrayIndices );

						CompressFloatArray( floatNormalsArray, 3, out compressedNormalsArray,
							 out normalsArrayToCompressedNormalsArrayIndices );

						//source (positions)
						WriteFloatSource( xmlTextWriter, compressedPositionsArray, meshName + "-lib-Position",
							 compressedPositionsArray.Length / 3, "3", new string[ 3 ] { "X", "Y", "Z" } );

						//source (normals)
						WriteFloatSource( xmlTextWriter, compressedNormalsArray, meshName + "-lib-Normal0",
							 compressedNormalsArray.Length / 3, "3", new string[ 3 ] { "X", "Y", "Z" } );

						if( floatUV0Array != null )
						{
							CompressFloatArray( floatUV0Array, 2, out compressedUV0Array,
								 out UV0ArrayToCompressedUV0ArrayIndices );

							//source (UV0)
							WriteFloatSource( xmlTextWriter, compressedUV0Array,
								 meshName + "-lib-UV0", compressedUV0Array.Length / 2,
								 "2", new string[ 2 ] { "S", "T" } );
						}

						if( floatUV1Array != null )
						{
							CompressFloatArray( floatUV1Array, 2, out compressedUV1Array,
								 out UV1ArrayToCompressedUV1ArrayIndices );

							//source (UV1)
							WriteFloatSource( xmlTextWriter, compressedUV1Array,
								 meshName + "-lib-UV1", compressedUV1Array.Length / 2,
								 "2", new string[ 2 ] { "S", "T" } );
						}

						if( floatUV2Array != null )
						{
							CompressFloatArray( floatUV2Array, 2, out compressedUV2Array,
								 out UV2ArrayToCompressedUV2ArrayIndices );

							//source (UV2)
							WriteFloatSource( xmlTextWriter, compressedUV2Array,
								 meshName + "-lib-UV2", compressedUV2Array.Length / 2,
								 "2", new string[ 2 ] { "S", "T" } );
						}

						if( floatUV3Array != null )
						{
							CompressFloatArray( floatUV3Array, 2, out compressedUV3Array,
								 out UV3ArrayToCompressedUV3ArrayIndices );

							//source (UV3)
							WriteFloatSource( xmlTextWriter, compressedUV3Array,
								 meshName + "-lib-UV3", compressedUV3Array.Length / 2,
								 "2", new string[ 2 ] { "S", "T" } );
						}


						//vertices
						{
							xmlTextWriter.WriteStartElement( "vertices" );
							xmlTextWriter.WriteAttributeString( "id", meshName + "-lib-Vertex" );

							//input
							WriteInput( xmlTextWriter, "POSITION", null, "#" + meshName + "-lib-Position" );

							xmlTextWriter.WriteEndElement();
						}

						int vertexStartIndex = 0;

						foreach( SubMesh subMesh in mesh.SubMeshes )
						{
							int subMeshTrianglesCount = subMesh.IndexData.IndexCount / 3;
							int[] indices = subMesh.IndexData.GetIndices();

							//polygons
							{
								xmlTextWriter.WriteStartElement( "polygons" );
								if( !string.IsNullOrEmpty( subMesh.MaterialName ) )
									xmlTextWriter.WriteAttributeString( "material", subMesh.MaterialName );
								xmlTextWriter.WriteAttributeString( "count",
									 subMeshTrianglesCount.ToString() );

								//input (VERTEX)
								WriteInput( xmlTextWriter, "VERTEX", "0", "#" + meshName + "-lib-Vertex" );

								//input (NORMAL)
								WriteInput( xmlTextWriter, "NORMAL", "1", "#" + meshName + "-lib-Normal0" );

								int uvLayersCount = 0;

								if( compressedUV0Array != null )
								{
									int offset = 2 + uvLayersCount;
									uvLayersCount++;

									//input (UV0)
									WriteInput( xmlTextWriter, "TEXCOORD", offset.ToString(), "#" + meshName + "-lib-UV0" );
								}

								if( compressedUV1Array != null )
								{
									int offset = 2 + uvLayersCount;
									uvLayersCount++;

									//input (UV1)
									WriteInput( xmlTextWriter, "TEXCOORD", offset.ToString(), "#" + meshName + "-lib-UV1" );
								}

								if( compressedUV2Array != null )
								{
									int offset = 2 + uvLayersCount;
									uvLayersCount++;

									//input (UV2)
									WriteInput( xmlTextWriter, "TEXCOORD", offset.ToString(), "#" + meshName + "-lib-UV2" );
								}

								if( compressedUV3Array != null )
								{
									int offset = 2 + uvLayersCount;
									uvLayersCount++;

									//input (UV3)
									WriteInput( xmlTextWriter, "TEXCOORD", offset.ToString(), "#" + meshName + "-lib-UV3" );
								}

								for( int triangleIndex = 0; triangleIndex < subMeshTrianglesCount;
									 triangleIndex++ )
								{
									int indicesPerVertexCount = uvLayersCount + 2;
									int[] polygonIndices = new int[ indicesPerVertexCount * 3 ];

									for( int triangleVertexIndex = 0; triangleVertexIndex < 3;
										 triangleVertexIndex++ )
									{
										int vertexIndex = vertexStartIndex + indices[ triangleIndex * 3 +
											 triangleVertexIndex ];

										int positionIndex = positionsArrayToCompressedPositionsArrayIndices[
											 vertexIndex ];
										int normalIndex = normalsArrayToCompressedNormalsArrayIndices[
											 vertexIndex ];

										polygonIndices[ indicesPerVertexCount * triangleVertexIndex + 0 ] =
											 positionIndex;
										polygonIndices[ indicesPerVertexCount * triangleVertexIndex + 1 ] =
											 normalIndex;

										int uvIndex = 0;

										if( compressedUV0Array != null )
										{
											polygonIndices[ indicesPerVertexCount * triangleVertexIndex +
												 uvIndex + 2 ] =
												 UV0ArrayToCompressedUV0ArrayIndices[ vertexIndex ];
											uvIndex++;
										}

										if( compressedUV1Array != null )
										{
											polygonIndices[ indicesPerVertexCount * triangleVertexIndex +
												 uvIndex + 2 ] =
												 UV1ArrayToCompressedUV1ArrayIndices[ vertexIndex ];
											uvIndex++;
										}

										if( compressedUV2Array != null )
										{
											polygonIndices[ indicesPerVertexCount * triangleVertexIndex +
												 uvIndex + 2 ] =
												 UV2ArrayToCompressedUV2ArrayIndices[ vertexIndex ];
											uvIndex++;
										}

										if( compressedUV3Array != null )
										{
											polygonIndices[ indicesPerVertexCount * triangleVertexIndex +
												 uvIndex + 2 ] =
												 UV3ArrayToCompressedUV3ArrayIndices[ vertexIndex ];
											uvIndex++;
										}
									}

									string polygonString = ConvertIntArrayToString( polygonIndices );

									//p
									{
										xmlTextWriter.WriteStartElement( "p" );
										xmlTextWriter.WriteString( polygonString );
										xmlTextWriter.WriteEndElement();
									}
								}

								xmlTextWriter.WriteEndElement();//polygons
							}

							vertexStartIndex += subMesh.VertexData.VertexCount;
						}

						xmlTextWriter.WriteEndElement();//mesh
					}

					xmlTextWriter.WriteEndElement();//geometry
				}

				xmlTextWriter.WriteEndElement();//library_geometries
			}

			return true;
		}

		bool WriteLibraryVisualScenes( XmlTextWriter xmlTextWriter, Mesh mesh,
			 List<string> meshMaterialNamesList )
		{
			//library_visual_scenes
			{
				xmlTextWriter.WriteStartElement( "library_visual_scenes" );

				//visual_scene
				{
					xmlTextWriter.WriteStartElement( "visual_scene" );
					xmlTextWriter.WriteAttributeString( "id", "RootNode" );
					xmlTextWriter.WriteAttributeString( "name", "RootNode" );

					//node
					{
						xmlTextWriter.WriteStartElement( "node" );

						string meshName = Path.GetFileNameWithoutExtension( mesh.Name );
						xmlTextWriter.WriteAttributeString( "id", meshName );
						xmlTextWriter.WriteAttributeString( "name", meshName );

						//matrix
						WriteMatrix( xmlTextWriter, "matrix", Mat4.Identity );

						//instance_geometry
						{
							xmlTextWriter.WriteStartElement( "instance_geometry" );
							xmlTextWriter.WriteAttributeString( "url", "#" + meshName + "-lib" );

							if( meshMaterialNamesList.Count > 0 )
							{
								//bind_material
								{
									xmlTextWriter.WriteStartElement( "bind_material" );
									//technique_common
									{
										xmlTextWriter.WriteStartElement( "technique_common" );

										foreach( string materialName in meshMaterialNamesList )
										{
											//instance_material
											{
												xmlTextWriter.WriteStartElement( "instance_material" );
												xmlTextWriter.WriteAttributeString( "symbol", materialName );
												xmlTextWriter.WriteAttributeString( "target",
													 "#" + materialName );
												xmlTextWriter.WriteEndElement();
											}
										}

										xmlTextWriter.WriteEndElement(); //technique_common
									}
									xmlTextWriter.WriteEndElement();//bind_material
								}
							}

							xmlTextWriter.WriteEndElement();//instance_geometry
						}

						xmlTextWriter.WriteEndElement();//node
					}

					xmlTextWriter.WriteEndElement();//visual_scene
				}

				xmlTextWriter.WriteEndElement();//library_visual_scenes
			}

			return true;
		}

		protected override bool Save( Mesh mesh, string realFileName )
		{
			Dictionary<string, string> generatedImages = GetGeneratedImages( mesh );
			List<string> meshMaterialNamesList = GetMeshMaterialNamesList( mesh );

			XmlTextWriter xmlTextWriter = new XmlTextWriter( realFileName, System.Text.Encoding.UTF8 );
			{
				xmlTextWriter.Formatting = Formatting.Indented;
				xmlTextWriter.Indentation = 1;
				xmlTextWriter.IndentChar = '\t';

				xmlTextWriter.WriteStartDocument();
				xmlTextWriter.WriteStartElement( "COLLADA" );
				xmlTextWriter.WriteAttributeString( "xmlns", "http://www.collada.org/2005/11/COLLADASchema" );
				xmlTextWriter.WriteAttributeString( "version", "1.4.1" );

				if( !WriteAsset( xmlTextWriter ) )
					return false;

				if( !WriteLibraryImages( xmlTextWriter, generatedImages ) )
					return false;

				if( !WriteLibraryMaterials( xmlTextWriter, meshMaterialNamesList ) )
					return false;

				if( !WriteLibraryEffects( xmlTextWriter, meshMaterialNamesList, generatedImages ) )
					return false;

				if( !WriteLibraryGeometries( xmlTextWriter, mesh ) )
					return false;

				if( !WriteLibraryVisualScenes( xmlTextWriter, mesh, meshMaterialNamesList ) )
					return false;

				//scene
				{
					xmlTextWriter.WriteStartElement( "scene" );

					//instance_visual_scene
					xmlTextWriter.WriteStartElement( "instance_visual_scene" );
					xmlTextWriter.WriteAttributeString( "url", "#RootNode" );
					xmlTextWriter.WriteEndElement();

					xmlTextWriter.WriteEndElement();
				}

				xmlTextWriter.WriteEndElement();
				xmlTextWriter.WriteEndDocument();

				try
				{
					xmlTextWriter.Flush();
				}
				catch( Exception ex )
				{
					Error( ex.Message );
					return false;
				}

				xmlTextWriter.Close();
			}

			return true;
		}

	}
}
