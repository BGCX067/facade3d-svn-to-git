// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.SoundSystem;
using Engine.Networking;

namespace GameCommon
{
	/// <summary>
	/// Defines a engine profiler window class.
	/// </summary>
	public class EngineProfilerWindow : Control
	{
		static EngineProfilerWindow instance;

		[Config( "EngineProfiler", "background" )]
		static bool background;

		[Config( "EngineProfiler", "lastPageSelectionIndex" )]
		static int lastPageSelectionIndex;

		Control window;
		CheckBox backgroundCheckBox;
		ComboBox pageSelectionComboBox;
		Control pageAreaControl;

		List<Page> pages = new List<Page>();
		Page activePage;

		///////////////////////////////////////////

		public static EngineProfilerWindow Instance
		{
			get { return instance; }
		}

		public bool Background
		{
			get { return background; }
		}

		public bool IsActivePostEffectsPage()
		{
			return activePage as PostEffectsPage != null;
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			EngineApp.Instance.Config.RegisterClassParameters( GetType() );

			instance = this;

			window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\EngineProfiler\\EngineProfilerWindow.gui" );
			Controls.Add( window );

			backgroundCheckBox = (CheckBox)window.Controls[ "Background" ];
			backgroundCheckBox.Checked = background;
			backgroundCheckBox.CheckedChange += Background_CheckedChange;

			Button closeButton = (Button)window.Controls[ "Close" ];
			closeButton.Click += delegate( Button sender )
			{
				SetShouldDetach();
			};
			if( PlatformInfo.Platform == PlatformInfo.Platforms.MacOSX )
				closeButton.Text = closeButton.Text.Replace( "F11", "Fn+F5" );

			pageSelectionComboBox = (ComboBox)window.Controls[ "PageSelection" ];
			pageSelectionComboBox.SelectedIndexChange += pageSelectionComboBox_SelectedIndexChange;

			pageAreaControl = window.Controls[ "PageArea" ];

			//add pages
			pages.Add( new GeneralInformationPage( "General Information", "GeneralInformationPage.gui" ) );
			pages.Add( new GeneralOptionsPage( "General Options", "GeneralOptionsPage.gui" ) );
			pages.Add( new RenderingSystemPage( "Rendering System", "RenderingSystemPage.gui" ) );
			pages.Add( new PhysicsSystemPage( "Physics System", "PhysicsSystemPage.gui" ) );
			pages.Add( new SoundSystemPage( "Sound System", "SoundSystemPage.gui" ) );
			pages.Add( new EntitySystemPage( "Entity System", "EntitySystemPage.gui" ) );
			pages.Add( new MemoryManagementPage( "Memory Management", "MemoryManagementPage.gui" ) );
			pages.Add( new DLLPage( "Assemblies and DLLs", "DLLPage.gui" ) );
			pages.Add( new NetworkingPage( "Networking", "NetworkingPage.gui" ) );
			pages.Add( new JoysticksPage( "Joysticks", "JoysticksPage.gui" ) );
			pages.Add( new TimePage( "Time", "TimePage.gui" ) );
			pages.Add( new PostEffectsPage( "Post Effects", "PostEffectsPage.gui" ) );
			pages.Add( new ProjectSpecificPage( "Project Specific", "ProjectSpecificPage.gui" ) );

			//update pageSelectionComboBox
			foreach( Page page in pages )
				pageSelectionComboBox.Items.Add( page.Caption );
			if( lastPageSelectionIndex >= 0 && lastPageSelectionIndex < pages.Count )
				pageSelectionComboBox.SelectedIndex = lastPageSelectionIndex;
			else
				pageSelectionComboBox.SelectedIndex = 0;

			UpdateColorMultiplier();
		}

		protected override void OnDetach()
		{
			foreach( Page page in pages )
				page.OnDestroy();
			pages.Clear();

			instance = null;

			base.OnDetach();
		}

		void Background_CheckedChange( CheckBox sender )
		{
			background = backgroundCheckBox.Checked;
			UpdateColorMultiplier();
		}

		void ChangePage( Page page )
		{
			if( activePage == page )
				return;

			//hide old page
			if( activePage != null )
			{
				if( activePage.PageControl != null )
					activePage.PageControl.Visible = false;
			}

			activePage = page;

			//load .gui file
			if( activePage.PageControl == null )
			{
				string path = Path.Combine( "Gui\\EngineProfiler", activePage.FileName );
				Control control = ControlDeclarationManager.Instance.CreateControl( path );
				activePage.PageControl = control;
				pageAreaControl.Controls.Add( control );
				activePage.OnInit();
			}

			//show page
			if( activePage.PageControl != null )
				activePage.PageControl.Visible = true;
		}

		void pageSelectionComboBox_SelectedIndexChange( ComboBox sender )
		{
			if( sender.SelectedIndex != -1 )
			{
				ChangePage( pages[ sender.SelectedIndex ] );
				lastPageSelectionIndex = sender.SelectedIndex;
			}
		}

		void UpdateColorMultiplier()
		{
			if( background )
				ColorMultiplier = new ColorValue( 1, 1, 1, .5f );
			else
				ColorMultiplier = new ColorValue( 1, 1, 1 );
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.B )
			{
				backgroundCheckBox.Checked = !backgroundCheckBox.Checked;
				return true;
			}

			if( e.Key == EKeys.Escape )
			{
				if( !Background )
				{
					SetShouldDetach();
					return true;
				}
			}

			return false;
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( activePage != null )
				activePage.OnUpdate();
		}

		///////////////////////////////////////////

		abstract class Page
		{
			string caption;
			string fileName;
			Control pageControl;

			public Page( string caption, string fileName )
			{
				this.caption = caption;
				this.fileName = fileName;
			}

			public string Caption
			{
				get { return caption; }
			}

			public string FileName
			{
				get { return fileName; }
			}

			public Control PageControl
			{
				get { return pageControl; }
				set { pageControl = value; }
			}

			public virtual void OnInit() { }
			public virtual void OnUpdate() { }
			public virtual void OnDestroy() { }
		}

		///////////////////////////////////////////

		class GeneralInformationPage : Page
		{
			public GeneralInformationPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				int totalTriangles;
				int totalBatches;
				int totalLights;
				IList<RenderStatisticsInfo.CameraInfo> cameras;
				RendererWorld.Instance.Statistics.GetLastFrameStatistics(
					out totalTriangles, out totalBatches, out totalLights, out cameras );

				PageControl.Controls[ "Triangles" ].Text = totalTriangles.ToString( "N0" );
				PageControl.Controls[ "Batches" ].Text = totalBatches.ToString( "N0" );
				PageControl.Controls[ "Lights" ].Text = totalLights.ToString( "N0" );

				//performance counter
				{
					float otherTime = 0;

					foreach( PerformanceCounter.Counter counter in PerformanceCounter.Counters )
					{
						PerformanceCounter.TimeCounter timeCounter = counter as PerformanceCounter.TimeCounter;

						if( timeCounter != null )
						{
							string counterNameWithoutSpaces = counter.Name.Replace( " ", "" );

							Control timeControl = PageControl.Controls[ counterNameWithoutSpaces + "Time" ];
							Control fpsControl = PageControl.Controls[ counterNameWithoutSpaces + "FPS" ];

							if( timeControl != null )
							{
								timeControl.Text = ( timeCounter.CalculatedValue * 1000.0f ).
									ToString( "F2" );
							}
							if( fpsControl != null )
								fpsControl.Text = ( 1.0f / timeCounter.CalculatedValue ).ToString( "F1" );

							if( !counter.InnerCounter )
							{
								if( counter == PerformanceCounter.TotalTimeCounter )
									otherTime += timeCounter.CalculatedValue;
								else
									otherTime -= timeCounter.CalculatedValue;
							}
						}
					}

					{
						TextBox timeControl = PageControl.Controls[ "OtherTime" ] as TextBox;
						TextBox fpsControl = PageControl.Controls[ "OtherFPS" ] as TextBox;

						if( timeControl != null )
							timeControl.Text = ( otherTime * 1000.0f ).ToString( "F2" );
						if( fpsControl != null )
							fpsControl.Text = ( 1.0f / otherTime ).ToString( "F1" );
					}
				}

				//cameras
				{
					ListBox camerasListBox = (ListBox)PageControl.Controls[ "Cameras" ];

					//update cameras list
					{
						List<string> newList = new List<string>();
						{
							foreach( RenderStatisticsInfo.CameraInfo cameraInfo in cameras )
							{
								string cameraInformation = string.Format( "{0} \"{1}\"",
									cameraInfo.CameraPurpose, cameraInfo.CameraName );
								newList.Add( cameraInformation );
							}

							newList.Add( "Total" );
						}

						bool needUpdate = false;
						{
							if( camerasListBox.Items.Count == newList.Count )
							{
								for( int n = 0; n < camerasListBox.Items.Count; n++ )
								{
									if( (string)camerasListBox.Items[ n ] != newList[ n ] )
									{
										needUpdate = true;
										break;
									}
								}
							}
							else
								needUpdate = true;
						}

						if( needUpdate )
						{
							string lastSelectedString = "";
							int lastSelectedEqualIndex = 0;
							if( camerasListBox.SelectedIndex != -1 )
							{
								lastSelectedString = (string)camerasListBox.SelectedItem;

								lastSelectedEqualIndex = -1;
								for( int n = 0; n <= camerasListBox.SelectedIndex; n++ )
								{
									if( (string)camerasListBox.Items[ n ] == lastSelectedString )
										lastSelectedEqualIndex++;
								}
							}

							camerasListBox.Items.Clear();
							foreach( string item in newList )
								camerasListBox.Items.Add( item );

							if( lastSelectedString != "" )
							{
								int skipCounter = 0;
								for( int n = 0; n < camerasListBox.Items.Count; n++ )
								{
									if( (string)camerasListBox.Items[ n ] == lastSelectedString )
									{
										if( lastSelectedEqualIndex == skipCounter )
										{
											camerasListBox.SelectedIndex = n;
											break;
										}
										skipCounter++;
									}
								}
							}

							if( camerasListBox.SelectedIndex == -1 )
								camerasListBox.SelectedIndex = camerasListBox.Items.Count - 1;
						}
					}

					//update camera info
					if( camerasListBox.SelectedIndex == camerasListBox.Items.Count - 1 )
					{
						//total statistics

						int staticMeshObjects = 0;
						int sceneNodes = 0;
						int guiRenderers = 0;
						int guiBatches = 0;
						int triangles = 0;
						int batches = 0;
						int lights = 0;

						foreach( RenderStatisticsInfo.CameraInfo cameraInfo in cameras )
						{
							staticMeshObjects += cameraInfo.StaticMeshObjects;
							sceneNodes += cameraInfo.SceneNodes;
							guiRenderers += cameraInfo.GuiRenderers;
							guiBatches += cameraInfo.GuiBatches;
							triangles += cameraInfo.Triangles;
							batches += cameraInfo.Batches;
							lights += cameraInfo.Lights;
						}

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							staticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text = sceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text = guiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text = guiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text = triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text = batches.ToString( "N0" );
						PageControl.Controls[ "CameraLights" ].Text = lights.ToString( "N0" );

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "";
					}
					else if( camerasListBox.SelectedIndex != -1 )
					{
						//selected camera statistics

						RenderStatisticsInfo.CameraInfo activeCameraInfo = cameras[ camerasListBox.SelectedIndex ];

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text =
							activeCameraInfo.StaticMeshObjects.ToString( "N0" );
						PageControl.Controls[ "CameraSceneNodes" ].Text =
							activeCameraInfo.SceneNodes.ToString( "N0" );
						PageControl.Controls[ "CameraGuiRenderers" ].Text =
							activeCameraInfo.GuiRenderers.ToString( "N0" );
						PageControl.Controls[ "CameraGuiBatches" ].Text =
							activeCameraInfo.GuiBatches.ToString( "N0" );
						PageControl.Controls[ "CameraTriangles" ].Text =
							activeCameraInfo.Triangles.ToString( "N0" );
						PageControl.Controls[ "CameraBatches" ].Text =
							activeCameraInfo.Batches.ToString( "N0" );
						PageControl.Controls[ "CameraLights" ].Text =
							activeCameraInfo.Lights.ToString( "N0" );

						if( activeCameraInfo.PortalSystemEnabled )
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text =
								activeCameraInfo.PortalSystemOutdoorWalks.ToString( "N0" );
							PageControl.Controls[ "CameraPortalsPassed" ].Text =
								activeCameraInfo.PortalSystemPortalsPassed.ToString( "N0" );
							PageControl.Controls[ "CameraZonesPassed" ].Text =
								activeCameraInfo.PortalSystemZonesPassed.ToString( "N0" );
						}
						else
						{
							PageControl.Controls[ "CameraOutdoorWalks" ].Text = "No zones";
							PageControl.Controls[ "CameraPortalsPassed" ].Text = "No zones";
							PageControl.Controls[ "CameraZonesPassed" ].Text = "No zones";
						}
					}
					else
					{
						//no camera selected

						PageControl.Controls[ "CameraStaticMeshObjects" ].Text = "";
						PageControl.Controls[ "CameraSceneNodes" ].Text = "";
						PageControl.Controls[ "CameraGuiRenderers" ].Text = "";
						PageControl.Controls[ "CameraGuiBatches" ].Text = "";
						PageControl.Controls[ "CameraTriangles" ].Text = "";
						PageControl.Controls[ "CameraBatches" ].Text = "";
						PageControl.Controls[ "CameraLights" ].Text = "";

						PageControl.Controls[ "CameraOutdoorWalks" ].Text = "";
						PageControl.Controls[ "CameraPortalsPassed" ].Text = "";
						PageControl.Controls[ "CameraZonesPassed" ].Text = "";
					}
				}
			}
		}

		///////////////////////////////////////////

		class RenderingSystemPage : Page
		{
			public RenderingSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					RenderSystem.Instance.Name, RenderSystem.Instance.DllFileName );

				string gpuSyntaxes = "";
				foreach( string gpuSyntax in GpuProgramManager.Instance.SupportedSyntaxes )
				{
					if( gpuSyntaxes != "" )
						gpuSyntaxes += " ";
					gpuSyntaxes += gpuSyntax;
				}
				PageControl.Controls[ "GPUSyntaxes" ].Text = gpuSyntaxes;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				//Textures
				{
					uint totalCount;
					uint totalSize;
					uint loadedCount;
					uint loadedSize;
					uint compressedLoadedCount;
					uint compressedLoadedSize;
					uint uncompressedLoadedCount;
					uint uncompressedLoadedSize;
					uint manuallyCreatedCount;
					uint manuallyCreatedSize;
					uint renderTargetCount;
					uint renderTargetSize;

					TextureManager.Instance.GetStatistics(
						out totalCount, out totalSize,
						out loadedCount, out loadedSize,
						out compressedLoadedCount, out compressedLoadedSize,
						out uncompressedLoadedCount, out uncompressedLoadedSize,
						out manuallyCreatedCount, out manuallyCreatedSize,
						out renderTargetCount, out renderTargetSize );

					PageControl.Controls[ "TexturesTotalCount" ].Text = totalCount.ToString();
					PageControl.Controls[ "TexturesTotalSize" ].Text =
						( (double)totalSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesLoadedCount" ].Text = loadedCount.ToString();
					PageControl.Controls[ "TexturesLoadedSize" ].Text =
						( (double)loadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesCompressedLoadedCount" ].Text =
						compressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesCompressedLoadedSize" ].Text =
						( (double)compressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesUncompressedLoadedCount" ].Text =
						uncompressedLoadedCount.ToString();
					PageControl.Controls[ "TexturesUncompressedLoadedSize" ].Text =
						( (double)uncompressedLoadedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesManuallyCreatedCount" ].Text =
						manuallyCreatedCount.ToString();
					PageControl.Controls[ "TexturesManuallyCreatedSize" ].Text =
						( (double)manuallyCreatedSize / 1024 / 1024 ).ToString( "F2" );
					PageControl.Controls[ "TexturesRenderTargetCount" ].Text =
						renderTargetCount.ToString();
					PageControl.Controls[ "TexturesRenderTargetSize" ].Text =
						( (double)renderTargetSize / 1024 / 1024 ).ToString( "F2" );
				}

				//Meshes
				{
					uint count;
					uint size;

					MeshManager.Instance.GetStatistics( out count, out size );

					PageControl.Controls[ "MeshesCount" ].Text = count.ToString();
					PageControl.Controls[ "MeshesSize" ].Text =
						( (double)size / 1024 / 1024 ).ToString( "F2" );
				}

				//GPU programs
				{
					PageControl.Controls[ "GPUPrograms" ].Text =
						GpuProgramManager.Instance.Programs.Count.ToString();

					int generatedAtRuntime = 0;
					int loadedFromCache = 0;
					foreach( GpuProgram gpuProgram in GpuProgramManager.Instance.Programs )
					{
						if( gpuProgram.HasLoadedFromShaderCache() )
							loadedFromCache++;
						else
							generatedAtRuntime++;
					}

					PageControl.Controls[ "GPUProgramsGeneratedAtRuntime" ].Text =
						generatedAtRuntime.ToString( "N0" );
					PageControl.Controls[ "GPUProgramsLoadedFromCache" ].Text =
						loadedFromCache.ToString( "N0" );

					if( RenderSystem.Instance.HasShaderModel2() && loadedFromCache == 0 )
					{
						PageControl.Controls[ "GPUProgramsLoadedFromCache" ].ColorMultiplier =
							new ColorValue( 1, 0, 0 );
					}
				}

				PageControl.Controls[ "HighLevelMaterials" ].Text =
					HighLevelMaterialManager.Instance.Materials.Count.ToString();

				PageControl.Controls[ "SceneGraph" ].Text =
					SceneManager.Instance._SceneGraph.Type.ToString();
				PageControl.Controls[ "SceneNodes" ].Text =
					SceneManager.Instance.SceneNodes.Count.ToString( "N0" );
				PageControl.Controls[ "StaticMeshObjects" ].Text =
					SceneManager.Instance.StaticMeshObjects.Count.ToString( "N0" );

				//Shader cache
				{
					OnlyForLoadShaderCacheManager manager = RendererWorld.ShaderCacheManager as
						OnlyForLoadShaderCacheManager;
					if( manager != null )
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text =
							Path.GetFileName( manager.UsedFileName );
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text =
							manager.ProgramCount.ToString( "N0" );
					}
					else
					{
						PageControl.Controls[ "ShaderCacheFileName" ].Text = "";
						PageControl.Controls[ "ShaderCacheProgramCount" ].Text = "";
					}
				}
			}
		}

		/////////////////////////////////////////////

		class PhysicsSystemPage : Page
		{
			public PhysicsSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				PageControl.Controls[ "Library" ].Text = string.Format( "{0} ({1})",
					PhysicsWorld.Instance.DriverName, PhysicsWorld.Instance.DriverAssemblyFileName );

				CheckBox hardwareAccelecatedCheckBox = (CheckBox)PageControl.Controls[
					"HardwareAccelerated" ];
				hardwareAccelecatedCheckBox.Enable = false;
				hardwareAccelecatedCheckBox.Checked = PhysicsWorld.Instance.HardwareAccelerated;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				PageControl.Controls[ "Bodies" ].Text =
					PhysicsWorld.Instance.Bodies.Count.ToString( "N0" );
				PageControl.Controls[ "Joints" ].Text =
					PhysicsWorld.Instance.Joints.Count.ToString( "N0" );
				PageControl.Controls[ "Motors" ].Text =
					PhysicsWorld.Instance.Motors.Count.ToString( "N0" );
			}
		}

		/////////////////////////////////////////////

		class SoundSystemPage : Page
		{
			public SoundSystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				ComboBox comboBox;

				string libraryText = SoundWorld.Instance.DriverName;
				if( SoundWorld.Instance.DriverName != "NULL" )
					libraryText += string.Format( " ({0})", SoundWorld.Instance.DriverAssemblyFileName );
				PageControl.Controls[ "Library" ].Text = libraryText;

				comboBox = (ComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Loaded sounds" );
				comboBox.Items.Add( "Virtual channels" );
				comboBox.Items.Add( "Real channels" );
				comboBox.SelectedIndex = 0;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				int informationTypeIndex = ( (ComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				StringBuilder text = new StringBuilder( "" );

				if( informationTypeIndex == 0 )
				{
					//loaded sounds

					text.AppendFormat( "Count: {0}\n", SoundWorld.Instance.Sounds.Count );
					text.Append( "\n" );

					foreach( Sound sound in SoundWorld.Instance.Sounds )
						text.AppendFormat( "{0}\n", sound );
				}
				else if( informationTypeIndex == 1 )
				{
					//virtual channels

					int activeChannelCount = SoundWorld.Instance.ActiveVirtual2DChannels.Count +
						SoundWorld.Instance.ActiveVirtual3DChannels.Count;

					text.AppendFormat( "Active channels: {0}\n", activeChannelCount );
					text.Append( "\n" );

					for( int nChannels = 0; nChannels < 2; nChannels++ )
					{
						IEnumerable<VirtualChannel> activeChannels = nChannels == 0 ?
							SoundWorld.Instance.ActiveVirtual2DChannels :
							SoundWorld.Instance.ActiveVirtual3DChannels;

						foreach( VirtualChannel virtualChannel in activeChannels )
						{
							if( virtualChannel.CurrentRealChannel != null )
								text.Append( "Real - " );
							else
								text.Append( "Virtual - " );

							string soundName;

							if( virtualChannel.CurrentSound.Name != null )
								soundName = virtualChannel.CurrentSound.Name;
							else
								soundName = "DataBuffer";
							text.AppendFormat( "{0}  Volume {1}\n", soundName,
								virtualChannel.GetTotalVolume().ToString( "F3" ) );
						}
					}
				}
				else
				{
					//real channels

					int freeCount = 0;
					int activeCount = 0;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IEnumerable<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;
						foreach( RealChannel realChannel in realChannels )
						{
							if( realChannel.CurrentVirtualChannel == null )
								freeCount++;
							else
								activeCount++;
						}
					}

					text.AppendFormat( "Free channels: {0}\n", freeCount );
					text.AppendFormat( "Active channels: {0}\n", activeCount );
					text.Append( "\n" );

					bool last3d = false;

					for( int nRealChannels = 0; nRealChannels < 2; nRealChannels++ )
					{
						IEnumerable<RealChannel> realChannels = nRealChannels == 0 ?
							SoundWorld.Instance.Real2DChannels : SoundWorld.Instance.Real3DChannels;

						foreach( RealChannel realChannel in realChannels )
						{
							VirtualChannel virtualChannel = realChannel.CurrentVirtualChannel;

							if( !last3d && realChannel.Is3D )
							{
								last3d = true;
								text.Append( "\n" );
							}

							text.AppendFormat( "{0}: ", realChannel.Is3D ? "3D" : "2D" );

							if( virtualChannel != null )
							{
								string soundName;

								if( virtualChannel.CurrentSound.Name != null )
									soundName = virtualChannel.CurrentSound.Name;
								else
									soundName = "DataBuffer";

								text.AppendFormat( "{0}  Volume {1}\n", soundName,
									virtualChannel.GetTotalVolume().ToString( "F3" ) );
							}
							else
								text.Append( "Free\n" );
						}
					}
				}

				PageControl.Controls[ "Information" ].Text = text.ToString();
			}
		}

		/////////////////////////////////////////////

		class EntitySystemPage : Page
		{
			public EntitySystemPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				if( Entities.Instance != null )
				{
					PageControl.Controls[ "EntityClasses" ].Text =
						EntityTypes.Instance.Classes.Count.ToString( "N0" );
					PageControl.Controls[ "EntityTypes" ].Text =
						EntityTypes.Instance.Types.Count.ToString( "N0" );
					PageControl.Controls[ "Entities" ].Text =
						Entities.Instance.EntitiesCollection.Count.ToString( "N0" );
				}
				else
				{
					PageControl.Controls[ "EntityClasses" ].Text = "";
					PageControl.Controls[ "EntityTypes" ].Text = "";
					PageControl.Controls[ "Entities" ].Text = "";
				}
			}
		}

		/////////////////////////////////////////////

		class MemoryManagementPage : Page
		{
			public MemoryManagementPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				PageControl.Controls[ "NetMemory" ].Text = GC.GetTotalMemory( false ).ToString( "N0" );

				int totalAllocatedMemory = 0;
				int totalAllocationCount = 0;

				for( int n = 0; n < (int)NativeMemoryAllocationType.Count; n++ )
				{
					NativeMemoryAllocationType allocationType = (NativeMemoryAllocationType)n;

					int allocatedMemory;
					int allocationCount;
					NativeMemoryManager.GetStatistics( allocationType, out allocatedMemory,
						out allocationCount );

					string typeString = allocationType.ToString();

					PageControl.Controls[ typeString + "Allocations" ].Text =
						allocationCount.ToString( "N0" );
					PageControl.Controls[ typeString + "Memory" ].Text =
						allocatedMemory.ToString( "N0" );

					totalAllocatedMemory += allocatedMemory;
					totalAllocationCount += allocationCount;
				}

				PageControl.Controls[ "TotalAllocations" ].Text = totalAllocationCount.ToString( "N0" );
				PageControl.Controls[ "TotalMemory" ].Text = totalAllocatedMemory.ToString( "N0" );

				int crtAllocatedMemory;
				int crtAllocationCount;
				NativeMemoryManager.GetCRTStatistics( out crtAllocatedMemory, out crtAllocationCount );

				PageControl.Controls[ "CRTAllocations" ].Text = crtAllocationCount.ToString( "N0" );
				PageControl.Controls[ "CRTMemory" ].Text = crtAllocatedMemory.ToString( "N0" );
			}
		}

		/////////////////////////////////////////////

		class DLLPage : Page
		{
			bool updated;
			ComboBox comboBox;

			//

			public DLLPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				comboBox = (ComboBox)PageControl.Controls[ "InformationType" ];
				comboBox.Items.Add( "Managed assemblies" );
				comboBox.Items.Add( "DLLs" );
				comboBox.SelectedIndex = 0;
				comboBox.SelectedIndexChange += ComboBox_SelectedIndexChange;
			}

			void ComboBox_SelectedIndexChange( ComboBox sender )
			{
				updated = false;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				if( updated )
					return;
				updated = true;

				int informationTypeIndex = ( (ComboBox)PageControl.Controls[ "InformationType" ] ).
					SelectedIndex;

				ListBox listBox = (ListBox)PageControl.Controls[ "Information" ];

				int lastSelectedIndex = listBox.SelectedIndex;

				listBox.Items.Clear();

				if( informationTypeIndex == 0 )
				{
					//managed assemblies

					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

					List<AssemblyName> resultAssemblyNames = new List<AssemblyName>( assemblies.Length );
					{
						List<Assembly> remainingAssemblies = new List<Assembly>( assemblies );

						while( true )
						{
							Assembly notReferencedAssembly = null;
							{
								foreach( Assembly assembly in remainingAssemblies )
								{
									AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();

									foreach( Assembly a in remainingAssemblies )
									{
										if( assembly == a )
											continue;

										AssemblyName aName = a.GetName();

										foreach( AssemblyName referencedAssembly in referencedAssemblies )
										{
											if( referencedAssembly.Name == aName.Name )
												goto nextAssembly;
										}
									}

									notReferencedAssembly = assembly;
									break;

									nextAssembly: ;
								}
							}

							if( notReferencedAssembly != null )
							{
								remainingAssemblies.Remove( notReferencedAssembly );
								resultAssemblyNames.Add( notReferencedAssembly.GetName() );
							}
							else
							{
								//no exists not referenced assemblies
								foreach( Assembly assembly in remainingAssemblies )
									resultAssemblyNames.Add( assembly.GetName() );
								break;
							}
						}
					}

					foreach( AssemblyName assemblyName in resultAssemblyNames )
					{
						string text = string.Format( "{0}, {1}", assemblyName.Name,
							assemblyName.Version );
						listBox.Items.Add( text );
					}
				}
				else if( informationTypeIndex == 1 )
				{
					//dlls
					string[] names = EngineApp.Instance.GetNativeModuleNames();

					ArrayUtils.SelectionSort( names, delegate( string s1, string s2 )
					{
						return string.Compare( s1, s2, true );
					} );

					foreach( string name in names )
					{
						string text = string.Format( "{0} - {1}", Path.GetFileName( name ), name );
						listBox.Items.Add( text );
					}
				}

				if( lastSelectedIndex >= 0 && lastSelectedIndex < listBox.Items.Count )
					listBox.SelectedIndex = lastSelectedIndex;
				if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
					listBox.SelectedIndex = 0;
			}
		}

		/////////////////////////////////////////////

		class NetworkingPage : Page
		{
			public NetworkingPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			void GetConnectedNodeData( NetworkNode.ConnectedNode connectedNode, StringBuilder text )
			{
				if( connectedNode.Status == NetworkConnectionStatuses.Connected )
				{
					text.AppendFormat( "- Connection with {0}\n", connectedNode.RemoteEndPoint.Address );

					NetworkNode.ConnectedNode.StatisticsData statistics = connectedNode.Statistics;

					text.AppendFormat(
						"-   Send: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesSent( true ) / 1024,
						(long)statistics.GetBytesSentPerSecond( true ) );

					text.AppendFormat(
						"-   Receive: Total: {0} kb, Speed: {1} b/s\n",
						statistics.GetBytesReceived( true ) / 1024,
						(long)statistics.GetBytesReceivedPerSecond( true ) );

					text.AppendFormat( "-   Ping: {0} ms\n",
						(int)( connectedNode.LastRoundtripTime * 1000 ) );
				}
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				StringBuilder text = new StringBuilder( "" );

				//GameNetworkServer
				GameNetworkServer server = GameNetworkServer.Instance;
				if( server != null )
				{
					text.Append( "Server:\n" );
					foreach( NetworkNode.ConnectedNode connectedNode in server.ConnectedNodes )
						GetConnectedNodeData( connectedNode, text );
					text.Append( "\n" );
				}

				//GameNetworkClient
				GameNetworkClient client = GameNetworkClient.Instance;
				if( client != null && client.Status == NetworkConnectionStatuses.Connected )
				{
					text.Append( "Client:\n" );
					GetConnectedNodeData( client.ServerConnectedNode, text );
					text.Append( "\n" );
				}

				if( text.Length == 0 )
					text.Append( "No connections" );

				//EntitySystem statistics
				if( EntitySystemWorld.Instance != null )
				{
					EntitySystemWorld.NetworkingInterface networkingInterface =
						EntitySystemWorld.Instance._GetNetworkingInterface();
					if( networkingInterface != null )
					{
						string[] lines = networkingInterface.GetStatisticsAsText();
						foreach( string line in lines )
							text.AppendFormat( "-   {0}\n", line );
					}
				}

				PageControl.Controls[ "Data" ].Text = text.ToString();
			}
		}

		/////////////////////////////////////////////

		class JoysticksPage : Page
		{
			ColorValue disabledColor = new ColorValue( .15f, .15f, .35f );
			ColorValue enabledColor = new ColorValue( 1, 1, 1 );
			ColorValue pressedColor = new ColorValue( 0, 1, 0 );

			JoystickInputDevice selectedJoystick;

			////////////////////////////////////////

			class TestEffectAxisItem
			{
				public JoystickAxes[] axes;

				public TestEffectAxisItem( JoystickAxes[] axes )
				{
					this.axes = axes;
				}

				public override string ToString()
				{
					string text = "";
					foreach( JoystickAxes axis in axes )
					{
						if( text.Length != 0 )
							text += " and ";
						text += axis.ToString();
					}
					return text;
				}
			}

			////////////////////////////////////////

			public JoysticksPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				//update device list
				{
					ComboBox devicesComboBox = (ComboBox)PageControl.Controls[ "Devices" ];

					devicesComboBox.Items.Clear();

					if( InputDeviceManager.Instance != null )
					{
						foreach( InputDevice device in InputDeviceManager.Instance.Devices )
						{
							JoystickInputDevice joystick = device as JoystickInputDevice;
							if( joystick != null )
								devicesComboBox.Items.Add( joystick );
						}
					}

					if( devicesComboBox.Items.Count > 0 )
					{
						devicesComboBox.SelectedIndexChange += devicesComboBox_SelectedIndexChange;
						devicesComboBox.SelectedIndex = 0;
					}
					else
					{
						devicesComboBox.Enable = false;
					}
				}

				//subscribe to button click events
				( (Button)PageControl.Controls[ "EffectDestroy" ] ).Click += EffectDestroy_Click;
				( (Button)PageControl.Controls[ "TestEffectCreate" ] ).Click += TestEffectCreate_Click;
			}

			void devicesComboBox_SelectedIndexChange( ComboBox sender )
			{
				selectedJoystick = sender.SelectedItem as JoystickInputDevice;

				//update TestEffectType combo box
				{
					ComboBox typeComboBox = (ComboBox)PageControl.Controls[ "TestEffectType" ];

					typeComboBox.Items.Clear();

					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						int typeCount = Enum.GetValues( typeof( ForceFeedbackEffectTypes ) ).Length;
						for( int n = 0; n < typeCount; n++ )
						{
							ForceFeedbackEffectTypes effectType = (ForceFeedbackEffectTypes)n;
							if( selectedJoystick.ForceFeedbackController.IsEffectSupported( effectType ) )
							{
								typeComboBox.Items.Add( effectType );
								if( effectType == ForceFeedbackEffectTypes.ConstantForce )
									typeComboBox.SelectedIndex = typeComboBox.Items.Count - 1;
							}
						}
					}

					if( typeComboBox.Items.Count > 0 && typeComboBox.SelectedIndex == -1 )
						typeComboBox.SelectedIndex = 0;
				}

				//update TestEffectAxis combo box
				{
					ComboBox axisComboBox = (ComboBox)PageControl.Controls[ "TestEffectAxis" ];

					axisComboBox.Items.Clear();

					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						bool existsX = false;
						bool existsY = false;

						for( int n = 0; n < selectedJoystick.Axes.Length; n++ )
						{
							JoystickInputDevice.Axis axis = selectedJoystick.Axes[ n ];

							axisComboBox.Items.Add( new TestEffectAxisItem( new JoystickAxes[] { axis.Name } ) );

							if( axis.Name == JoystickAxes.X )
								existsX = true;
							if( axis.Name == JoystickAxes.Y )
								existsY = true;
						}

						if( existsX && existsY )
						{
							axisComboBox.Items.Add( new TestEffectAxisItem(
								new JoystickAxes[] { JoystickAxes.X, JoystickAxes.Y } ) );
							axisComboBox.SelectedIndex = axisComboBox.Items.Count - 1;
						}
					}

					if( axisComboBox.Items.Count > 0 && axisComboBox.SelectedIndex == -1 )
						axisComboBox.SelectedIndex = 0;
				}
			}

			void UpdateInfoControls()
			{
				string text;
				if( selectedJoystick != null )
				{
					text = string.Format( "Buttons: {1}, Axes: {0}, POVs: {2}, Sliders: {3}\n",
						selectedJoystick.Buttons.Length, selectedJoystick.Axes.Length,
						selectedJoystick.POVs.Length, selectedJoystick.Sliders.Length );

					if( selectedJoystick.ForceFeedbackController != null )
						text += "Force feedback supported";
					else
						text += "Force feedback does not supported";
				}
				else
					text = "";

				PageControl.Controls[ "Info" ].ColorMultiplier =
					selectedJoystick != null ? enabledColor : disabledColor;
				PageControl.Controls[ "InfoValue" ].Text = text;
			}

			void UpdateButtonControls()
			{
				PageControl.Controls[ "Buttons" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.Buttons.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 10; n++ )
				{
					string name = string.Format( "Button{0}Name", n + 1 );
					Control control = PageControl.Controls[ name ];

					ColorValue color;
					if( selectedJoystick != null && n < selectedJoystick.Buttons.Length )
						color = enabledColor;
					else
						color = disabledColor;

					control.ColorMultiplier = color;
				}

				for( int n = 0; n < 30; n++ )
				{
					ColorValue color;

					if( selectedJoystick != null && n < selectedJoystick.Buttons.Length )
					{
						if( selectedJoystick.Buttons[ n ].Pressed )
							color = pressedColor;
						else
							color = enabledColor;
					}
					else
					{
						color = disabledColor;
					}

					string name = string.Format( "Button{0}", n + 1 );
					Control control = PageControl.Controls[ name ];
					control.ColorMultiplier = color;
				}
			}

			void UpdatePOVControls()
			{
				PageControl.Controls[ "POVs" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.POVs.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 3; n++ )
				{
					JoystickInputDevice.POV pov = null;
					if( selectedJoystick != null && n < selectedJoystick.POVs.Length )
						pov = selectedJoystick.POVs[ n ];

					string labelName = string.Format( "POV{0}Name", n + 1 );
					string name = string.Format( "POV{0}", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					Control control = PageControl.Controls[ name ];
					labelControl.ColorMultiplier = pov != null ? enabledColor : disabledColor;
					control.ColorMultiplier = pov != null ? enabledColor : disabledColor;

					//update POV buttons
					{
						JoystickPOVDirections[] directions = new JoystickPOVDirections[]
						{
							JoystickPOVDirections.North, 
							JoystickPOVDirections.South,
							JoystickPOVDirections.East, 
							JoystickPOVDirections.West
						};

						foreach( JoystickPOVDirections direction in directions )
						{
							bool pressed = pov != null && ( pov.Value & direction ) > 0;

							string buttonName = string.Format( "Button{0}", direction );
							Control buttonControl = control.Controls[ buttonName ];
							buttonControl.ColorMultiplier = pressed ? pressedColor : new ColorValue( 1, 1, 1 );
						}
					}
				}
			}

			void UpdateBarControlValue( Control control, float value )
			{
				Rect screenRectangle = control.GetScreenRectangle();
				Vec2 rectSize = screenRectangle.Size;
				float width = rectSize.X / 2 * Math.Abs( value );
				Rect rect;
				if( value > 0 )
				{
					float left = screenRectangle.Left + rectSize.X / 2;
					rect = new Rect( left, screenRectangle.Top, left + width, screenRectangle.Bottom );
				}
				else
				{
					float right = screenRectangle.Left + rectSize.X / 2;
					rect = new Rect( right - width, screenRectangle.Top, right, screenRectangle.Bottom );
				}

				Control barControl = control.Controls[ "Bar" ];
				barControl.SetScreenClipRectangle( rect );
			}

			void UpdateAxisControls()
			{
				PageControl.Controls[ "Axes" ].ColorMultiplier =
					( selectedJoystick != null && selectedJoystick.Axes.Length > 0 ) ?
					enabledColor : disabledColor;

				for( int n = 0; n < 6; n++ )
				{
					JoystickInputDevice.Axis axis = null;
					if( selectedJoystick != null && n < selectedJoystick.Axes.Length )
						axis = selectedJoystick.Axes[ n ];
					float value = axis != null ? axis.Value : 0;

					string labelName = string.Format( "Axis{0}Name", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					string name = string.Format( "Axis{0}", n + 1 );
					Control control = PageControl.Controls[ name ];

					ColorValue color = axis != null ? enabledColor : disabledColor;
					labelControl.ColorMultiplier = color;
					labelControl.Text = axis != null ? axis.Name.ToString() : "Not available";
					control.ColorMultiplier = color;
					UpdateBarControlValue( control, value );
				}
			}

			void UpdateSliderControls()
			{
				{
					ColorValue color = ( selectedJoystick != null && selectedJoystick.Sliders.Length > 0 ) ?
						enabledColor : disabledColor;
					PageControl.Controls[ "Sliders" ].ColorMultiplier = color;
					PageControl.Controls[ "SliderX" ].ColorMultiplier = color;
					PageControl.Controls[ "SliderY" ].ColorMultiplier = color;
				}

				for( int n = 0; n < 6; n++ )
				{
					JoystickInputDevice.Slider slider = null;
					if( selectedJoystick != null && n < selectedJoystick.Sliders.Length )
						slider = selectedJoystick.Sliders[ n ];
					Vec2 value = slider != null ? slider.Value : Vec2.Zero;

					string labelName = string.Format( "Slider{0}Name", n + 1 );
					string nameX = string.Format( "Slider{0}X", n + 1 );
					string nameY = string.Format( "Slider{0}Y", n + 1 );
					Control labelControl = PageControl.Controls[ labelName ];
					Control controlX = PageControl.Controls[ nameX ];
					Control controlY = PageControl.Controls[ nameY ];

					ColorValue color = slider != null ? enabledColor : disabledColor;
					labelControl.ColorMultiplier = color;
					controlX.ColorMultiplier = color;
					controlY.ColorMultiplier = color;
					UpdateBarControlValue( controlX, value.X );
					UpdateBarControlValue( controlY, value.Y );
				}
			}

			ForceFeedbackEffect GetSelectedEffect()
			{
				ListBox listBox = (ListBox)PageControl.Controls[ "EffectList" ];
				return listBox.SelectedItem as ForceFeedbackEffect;
			}

			void UpdateEffectList()
			{
				ListBox listBox = (ListBox)PageControl.Controls[ "EffectList" ];

				List<ForceFeedbackEffect> newList = new List<ForceFeedbackEffect>();
				{
					if( selectedJoystick != null && selectedJoystick.ForceFeedbackController != null )
					{
						foreach( ForceFeedbackEffect effect in selectedJoystick.ForceFeedbackController.Effects )
							newList.Add( effect );
					}
				}

				bool needUpdate = false;
				{
					if( newList.Count == listBox.Items.Count )
					{
						for( int n = 0; n < newList.Count; n++ )
						{
							if( newList[ n ] != listBox.Items[ n ] )
								needUpdate = true;
						}
					}
					else
						needUpdate = true;
				}

				if( needUpdate )
				{
					object lastSelectedItem = listBox.SelectedItem;

					listBox.Items.Clear();
					foreach( ForceFeedbackEffect effect in newList )
					{
						listBox.Items.Add( effect );
						if( effect == lastSelectedItem )
							listBox.SelectedIndex = listBox.Items.Count - 1;
					}

					if( listBox.Items.Count != 0 && listBox.SelectedIndex == -1 )
						listBox.SelectedIndex = 0;
				}
			}

			void UpdateSelectedEffectInfo()
			{
				StringBuilder text = new StringBuilder();

				ForceFeedbackEffect effect = GetSelectedEffect();
				if( effect != null )
				{
					text.Append( effect.ToString() );

					if( effect.Direction != null )
					{
						text.Append( "; Direction =" );
						foreach( float value in effect.Direction )
							text.AppendFormat( " {0}", value.ToString( "F2" ) );
					}

					text.AppendFormat( "; Duration = {0}", effect.Duration.ToString( "F2" ) );

					//ConstantForce type
					ForceFeedbackConstantForceEffect constantForce = effect as ForceFeedbackConstantForceEffect;
					if( constantForce != null )
					{
						text.AppendFormat( "; Magnitude = {0}", constantForce.Magnitude.ToString( "F2" ) );
					}

					//SawtoothDown, SawtoothUp, Sine, Square, Triangle types.
					ForceFeedbackPeriodicEffect periodic = effect as ForceFeedbackPeriodicEffect;
					if( periodic != null )
					{
						text.AppendFormat( "; Magnitude = {0}", periodic.Magnitude.ToString( "F2" ) );
						text.AppendFormat( "; Offset = {0}", periodic.Offset.ToString( "F2" ) );
						text.AppendFormat( "; Phase = {0}", periodic.Phase.ToString( "F2" ) );
						text.AppendFormat( "; Period = {0}", periodic.Period.ToString( "F2" ) );
					}

					//Spring, Damper, Friction, Inertia types.
					ForceFeedbackConditionEffect condition = effect as ForceFeedbackConditionEffect;
					if( condition != null )
					{
						text.AppendFormat( "; Offset = {0}", condition.Offset.ToString( "F2" ) );
						text.AppendFormat( "; PositiveSaturation = {0}", condition.PositiveSaturation.ToString( "F2" ) );
						text.AppendFormat( "; NegativeSaturation = {0}", condition.NegativeSaturation.ToString( "F2" ) );
						text.AppendFormat( "; PositiveCoefficient = {0}", condition.PositiveCoefficient.ToString( "F2" ) );
						text.AppendFormat( "; NegativeCoefficient = {0}", condition.NegativeCoefficient.ToString( "F2" ) );
						text.AppendFormat( "; DeadBand = {0}", condition.DeadBand.ToString( "F2" ) );
					}

					//Ramp type
					ForceFeedbackRampEffect ramp = effect as ForceFeedbackRampEffect;
					if( ramp != null )
					{
						text.AppendFormat( "; StartForce = {0}", ramp.StartForce.ToString( "F2" ) );
						text.AppendFormat( "; EndForce = {0}", ramp.EndForce.ToString( "F2" ) );
					}

				}

				PageControl.Controls[ "EffectInfo" ].Text = text.ToString();
			}

			void UpdateForceFeedbackControls()
			{
				UpdateEffectList();
				UpdateSelectedEffectInfo();

				bool availableForceFeedback = selectedJoystick != null &&
					selectedJoystick.ForceFeedbackController != null;

				ColorValue color = availableForceFeedback ? enabledColor : disabledColor;
				PageControl.Controls[ "ForceFeedbackEffects" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectList" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectTypeLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectAxisLabel" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectType" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectAxis" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectInfo" ].ColorMultiplier = color;
				PageControl.Controls[ "EffectDestroy" ].ColorMultiplier = color;
				PageControl.Controls[ "TestEffectCreate" ].ColorMultiplier = color;

				PageControl.Controls[ "EffectDestroy" ].Enable = availableForceFeedback &&
					GetSelectedEffect() != null;
				PageControl.Controls[ "TestEffectCreate" ].Enable = availableForceFeedback;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				UpdateInfoControls();
				UpdateButtonControls();
				UpdatePOVControls();
				UpdateAxisControls();
				UpdateSliderControls();
				UpdateForceFeedbackControls();
			}

			void EffectDestroy_Click( Button sender )
			{
				ForceFeedbackEffect effect = GetSelectedEffect();
				if( effect != null )
					effect.Destroy();
			}

			void TestEffectCreate_Click( Button sender )
			{
				if( selectedJoystick == null )
					return;
				if( selectedJoystick.ForceFeedbackController == null )
					return;

				ForceFeedbackEffectTypes effectType = ForceFeedbackEffectTypes.ConstantForce;
				JoystickAxes[] axes = null;
				{
					ComboBox typeComboBox = (ComboBox)PageControl.Controls[ "TestEffectType" ];
					ComboBox axisComboBox = (ComboBox)PageControl.Controls[ "TestEffectAxis" ];
					if( typeComboBox.SelectedIndex != -1 && axisComboBox.SelectedIndex != -1 )
					{
						effectType = (ForceFeedbackEffectTypes)typeComboBox.SelectedItem;
						axes = ( (TestEffectAxisItem)axisComboBox.SelectedItem ).axes;
					}
				}

				if( axes == null )
					return;

				//create effect
				ForceFeedbackEffect effect = selectedJoystick.ForceFeedbackController.CreateEffect( effectType, axes );
				if( effect != null )
				{
					//you can set duration in seconds.
					//effect.Duration = 3;

					if( axes.Length == 2 )
					{
						Radian angle = new Degree( 30 ).InRadians();
						float x = MathFunctions.Cos( angle );
						float y = MathFunctions.Sin( angle );
						effect.SetDirection( new float[] { x, y } );
					}

					//ConstantForce type
					ForceFeedbackConstantForceEffect constantForce = effect as ForceFeedbackConstantForceEffect;
					if( constantForce != null )
					{
						constantForce.Magnitude = 1;
					}

					//SawtoothDown, SawtoothUp, Sine, Square, Triangle types.
					ForceFeedbackPeriodicEffect periodic = effect as ForceFeedbackPeriodicEffect;
					if( periodic != null )
					{
						periodic.Magnitude = 1.0f;
						periodic.Offset = 0.0f;
						periodic.Phase = 0.5f;
						periodic.Period = 2;
					}

					//Spring, Damper, Friction, Inertia types.
					ForceFeedbackConditionEffect condition = effect as ForceFeedbackConditionEffect;
					if( condition != null )
					{
						condition.Offset = 0.5f;
						condition.PositiveSaturation = 1.0f;
						condition.NegativeSaturation = 1.0f;
						condition.PositiveCoefficient = 1.0f;
						condition.NegativeCoefficient = -1.0f;
						condition.DeadBand = 0.0f;
					}

					//Ramp type
					ForceFeedbackRampEffect ramp = effect as ForceFeedbackRampEffect;
					if( ramp != null )
					{
						ramp.StartForce = -1.0f;
						ramp.EndForce = 1.0f;
					}

					effect.Start();
				}
			}
		}

		/////////////////////////////////////////////

		class GeneralOptionsPage : Page
		{
			List<CheckBox> checkBoxes = new List<CheckBox>();
			bool disableChangeEvents;

			//

			public GeneralOptionsPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				Type type = typeof( EngineDebugSettings );
				InitCheckBox( "StaticPhysics", type.GetProperty( "DrawStaticPhysics" ) );
				InitCheckBox( "DynamicPhysics", type.GetProperty( "DrawDynamicPhysics" ) );
				InitCheckBox( "SceneGraphInfo", type.GetProperty( "DrawSceneGraphInfo" ) );
				InitCheckBox( "Regions", type.GetProperty( "DrawRegions" ) );
				InitCheckBox( "MapObjectBounds", type.GetProperty( "DrawMapObjectBounds" ) );
				InitCheckBox( "SceneNodeBounds", type.GetProperty( "DrawSceneNodeBounds" ) );
				InitCheckBox( "StaticMeshObjectBounds", type.GetProperty( "DrawStaticMeshObjectBounds" ) );
				InitCheckBox( "ZonesPortalsOccluders", type.GetProperty( "DrawZonesPortalsOccluders" ) );
				InitCheckBox( "FrustumTest", type.GetProperty( "FrustumTest" ) );
				InitCheckBox( "Lights", type.GetProperty( "DrawLights" ) );
				InitCheckBox( "StaticGeometry", type.GetProperty( "DrawStaticGeometry" ) );
				InitCheckBox( "Models", type.GetProperty( "DrawModels" ) );
				InitCheckBox( "Effects", type.GetProperty( "DrawEffects" ) );
				InitCheckBox( "Gui", type.GetProperty( "DrawGui" ) );
				InitCheckBox( "Wireframe", type.GetProperty( "DrawWireframe" ) );
				InitCheckBox( "PostEffects", type.GetProperty( "DrawPostEffects" ) );
				InitCheckBox( "GameSpecificDebugGeometry", type.GetProperty( "DrawGameSpecificDebugGeometry" ) );
				InitCheckBox( "DrawShadowDebugging", type.GetProperty( "DrawShadowDebugging" ) );

				( (Button)PageControl.Controls[ "Defaults" ] ).Click += Defaults_Click;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;
				foreach( CheckBox checkBox in checkBoxes )
					UpdateCheckBox( checkBox );
				disableChangeEvents = false;
			}

			void Defaults_Click( Button sender )
			{
				foreach( Control control in PageControl.Controls )
				{
					CheckBox checkBox = control as CheckBox;
					if( checkBox == null )
						continue;

					PropertyInfo property = checkBox.UserData as PropertyInfo;
					if( property == null )
						continue;

					DefaultValueAttribute[] attributes = (DefaultValueAttribute[])property.
						GetCustomAttributes( typeof( DefaultValueAttribute ), true );

					if( attributes.Length == 0 )
						continue;

					checkBox.Checked = (bool)attributes[ 0 ].Value;
				}
			}

			void InitCheckBox( string name, PropertyInfo property )
			{
				CheckBox checkBox = (CheckBox)PageControl.Controls[ name ];
				checkBox.UserData = property;
				checkBox.Checked = (bool)property.GetValue( null, null );

				checkBox.CheckedChange += checkBox_CheckedChange;

				checkBoxes.Add( checkBox );
			}

			void checkBox_CheckedChange( CheckBox sender )
			{
				if( !disableChangeEvents )
				{
					PropertyInfo p = (PropertyInfo)sender.UserData;
					p.SetValue( null, !(bool)p.GetValue( null, null ), null );
				}
			}

			void UpdateCheckBox( CheckBox checkBox )
			{
				PropertyInfo property = (PropertyInfo)checkBox.UserData;

				checkBox.Checked = (bool)property.GetValue( null, null );
			}
		}

		///////////////////////////////////////////

		class TimePage : Page
		{
			ScrollBar engineTimeScaleScrollBar;
			ScrollBar soundPitchScaleScrollBar;
			bool disableChangeEvents;

			//

			public TimePage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				//EngineTimeScale
				engineTimeScaleScrollBar = (ScrollBar)PageControl.Controls[ "EngineTimeScale" ];
				engineTimeScaleScrollBar.Value = EngineApp.Instance.TimeScale;
				UpdateEngineTimeScaleValue();
				engineTimeScaleScrollBar.ValueChange += engineTimeScaleScrollBar_ValueChange;

				//SoundPitchScale
				soundPitchScaleScrollBar = (ScrollBar)PageControl.Controls[ "SoundPitchScale" ];
				soundPitchScaleScrollBar.Value = SoundWorld.Instance.MasterChannelGroup.Pitch;
				UpdateSoundPitchScaleValue();
				soundPitchScaleScrollBar.ValueChange += soundPitchScaleScrollBar_ValueChange;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;

				engineTimeScaleScrollBar.Value = EngineApp.Instance.TimeScale;
				UpdateEngineTimeScaleValue();
				soundPitchScaleScrollBar.Value = SoundWorld.Instance.MasterChannelGroup.Pitch;
				UpdateSoundPitchScaleValue();

				disableChangeEvents = false;
			}

			void engineTimeScaleScrollBar_ValueChange( ScrollBar sender )
			{
				if( !disableChangeEvents )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					EngineApp.Instance.TimeScale = value;
					UpdateEngineTimeScaleValue();
				}
			}

			void soundPitchScaleScrollBar_ValueChange( ScrollBar sender )
			{
				if( !disableChangeEvents )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					SoundWorld.Instance.MasterChannelGroup.Pitch = value;
					UpdateSoundPitchScaleValue();
				}
			}

			void UpdateEngineTimeScaleValue()
			{
				PageControl.Controls[ "EngineTimeScaleValue" ].Text =
					EngineApp.Instance.TimeScale.ToString( "F1" );
			}

			void UpdateSoundPitchScaleValue()
			{
				PageControl.Controls[ "SoundPitchScaleValue" ].Text =
					SoundWorld.Instance.MasterChannelGroup.Pitch.ToString( "F1" );
			}

		}

		/////////////////////////////////////////////

		class PostEffectsPage : Page
		{
			Viewport viewport;

			ListBox listBox;

			CheckBox checkBoxEnabled;
			ScrollBar[] scrollBarFloatParameters = new ScrollBar[ 7 ];
			CheckBox[] checkBoxBoolParameters = new CheckBox[ 1 ];

			bool disableChangeEvents;

			//

			public PostEffectsPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

				disableChangeEvents = true;

				viewport = RendererWorld.Instance.DefaultViewport;

				//listBox
				{
					listBox = (ListBox)PageControl.Controls[ "List" ];

					//fill list box of post effects. "HDR" is always first.
					if( CompositorManager.Instance.GetByName( "HDR" ) != null )
						listBox.Items.Add( "HDR" );
					foreach( Compositor compositor in CompositorManager.Instance.Compositors )
					{
						if( compositor.Name != "HDR" )
							listBox.Items.Add( compositor.Name );
					}

					listBox.SelectedIndexChange += listBox_SelectedIndexChange;

					for( int n = 0; n < listBox.Items.Count; n++ )
					{
						Button itemButton = listBox.ItemButtons[ n ];
						CheckBox checkBox = (CheckBox)itemButton.Controls[ "CheckBox" ];

						if( itemButton.Text == "HDR" )
							checkBox.Enable = false;

						checkBox.Click += listBoxCheckBox_Click;
					}

					if( listBox.Items.Count > 0 )
						listBox.SelectedIndex = 0;
				}

				//checkBoxEnabled
				checkBoxEnabled = (CheckBox)PageControl.Controls[ "Enabled" ];
				checkBoxEnabled.Click += checkBoxEnabled_Click;
				if( listBox.Items.Count == 0 )
					checkBoxEnabled.Enable = false;

				//scrollBarFloatParameters, checkBoxBoolParameters
				for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
				{
					scrollBarFloatParameters[ n ] =
						(ScrollBar)PageControl.Controls[ "FloatParameter" + n.ToString() ];
					scrollBarFloatParameters[ n ].ValueChange += floatParameter_ValueChange;
				}
				for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
				{
					checkBoxBoolParameters[ n ] =
						(CheckBox)PageControl.Controls[ "BoolParameter" + n.ToString() ];
					checkBoxBoolParameters[ n ].CheckedChange += boolParameter_CheckedChange;
				}

				//ApplyToTheScreenGUI
				{
					CheckBox checkBox = (CheckBox)PageControl.Controls[ "ApplyToTheScreenGUI" ];
					checkBox.Click += applyToTheScreenGUICheckBox_Click;
				}

				UpdateListBox();
				UpdateApplyToTheScreenGUICheckBox();
				UpdateCurrentPostEffectControls();

				disableChangeEvents = false;
			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				disableChangeEvents = true;

				UpdateListBox();
				UpdateApplyToTheScreenGUICheckBox();
				UpdateCurrentPostEffectControls();

				disableChangeEvents = false;
			}

			string GetListCompositorItemName( string itemName )
			{
				return itemName.Split( new char[] { ' ' } )[ 0 ];
			}

			void listBox_SelectedIndexChange( ListBox sender )
			{
				if( !disableChangeEvents )
				{
					disableChangeEvents = true;

					UpdateCurrentPostEffectControls();

					disableChangeEvents = false;
				}
			}

			void listBoxCheckBox_Click( CheckBox sender )
			{
				//set listBox current item
				for( int n = 0; n < listBox.Items.Count; n++ )
				{
					Button itemButton = listBox.ItemButtons[ n ];
					if( itemButton.Controls[ "CheckBox" ] == sender )
						listBox.SelectedIndex = n;
				}

				checkBoxEnabled.Checked = sender.Checked;
				checkBoxEnabled.Enable = ( sender.Parent.Text != "HDR" );

				UpdateCurrentPostEffect();
			}

			void applyToTheScreenGUICheckBox_Click( CheckBox sender )
			{
				EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer = sender.Checked;
			}

			void checkBoxEnabled_Click( CheckBox sender )
			{
				if( listBox.SelectedItem != null )
				{
					Button itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
					( (CheckBox)itemButton.Controls[ "CheckBox" ] ).Checked = sender.Checked;

					UpdateCurrentPostEffect();
				}
			}

			void floatParameter_ValueChange( ScrollBar sender )
			{
				PageControl.Controls[ sender.Name + "Value" ].Text = sender.Value.ToString( "F2" );
				if( !disableChangeEvents )
					UpdateCurrentPostEffect();
			}

			void boolParameter_CheckedChange( CheckBox sender )
			{
				if( !disableChangeEvents )
					UpdateCurrentPostEffect();
			}

			void UpdateListBox()
			{
				for( int n = 0; n < listBox.Items.Count; n++ )
				{
					Button itemButton = listBox.ItemButtons[ n ];
					CheckBox checkBox = (CheckBox)itemButton.Controls[ "CheckBox" ];

					string name = GetListCompositorItemName( (string)listBox.Items[ n ] );
					CompositorInstance compositorInstance = viewport.GetCompositorInstance( name );
					checkBox.Checked = compositorInstance != null && compositorInstance.Enabled;
				}
			}

			void UpdateApplyToTheScreenGUICheckBox()
			{
				CheckBox checkBox = (CheckBox)PageControl.Controls[ "ApplyToTheScreenGUI" ];
				checkBox.Checked = EngineApp.Instance.ScreenGuiRenderer.ApplyPostEffectsToScreenRenderer;
			}

			void UpdateCurrentPostEffectControls()
			{
				//Hide controls
				{
					for( int n = 0; n < scrollBarFloatParameters.Length; n++ )
					{
						string s = "FloatParameter" + n.ToString();
						PageControl.Controls[ s + "Text" ].Visible = false;
						PageControl.Controls[ s ].Visible = false;
						PageControl.Controls[ s + "Value" ].Visible = false;
						PageControl.Controls[ s + "Value" ].Text = "";
					}
					for( int n = 0; n < checkBoxBoolParameters.Length; n++ )
					{
						string s = "BoolParameter" + n.ToString();
						PageControl.Controls[ s ].Visible = false;
					}
					PageControl.Controls[ "Description" ].Text = "";
				}

				if( listBox.SelectedItem != null )
				{
					string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

					//Set post effect name
					PageControl.Controls[ "Name" ].Text = name;

					//Update "Enabled" check box

					Button itemButton = listBox.ItemButtons[ listBox.SelectedIndex ];
					checkBoxEnabled.Checked = ( (CheckBox)itemButton.Controls[ "CheckBox" ] ).Checked;
					checkBoxEnabled.Enable = ( itemButton.Text != "HDR" );

					//Show need parameters

					//MotionBlur specific
					if( name == "MotionBlur" )
					{
						PageControl.Controls[ "FloatParameter0Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter0Text" ].Text = "Blur";
						PageControl.Controls[ "FloatParameter0Value" ].Visible = true;
						scrollBarFloatParameters[ 0 ].Visible = true;
						scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 2 );
						scrollBarFloatParameters[ 0 ].Value = MotionBlurCompositorInstance.Blur;

						PageControl.Controls[ "Description" ].Text = "This effect can be disabled by game logic " +
							"code (for unit contusion effect). For test this effect you can use map without " +
							"player character (as example RTSDemo).";
					}

					//Blur specific
					if( name == "Blur" )
					{
						PageControl.Controls[ "FloatParameter0Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter0Text" ].Text = "Fuzziness";
						PageControl.Controls[ "FloatParameter0Value" ].Visible = true;
						scrollBarFloatParameters[ 0 ].Visible = true;
						scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 15 );
						scrollBarFloatParameters[ 0 ].Value = BlurCompositorInstance.Fuzziness;
					}

					//RadialBlur specific
					if( name == "RadialBlur" )
					{
						PageControl.Controls[ "FloatParameter0Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter0Text" ].Text = "Blur Factor";
						PageControl.Controls[ "FloatParameter0Value" ].Visible = true;
						scrollBarFloatParameters[ 0 ].Visible = true;
						scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 1 );
						scrollBarFloatParameters[ 0 ].Value = RadialBlurCompositorInstance.BlurFactor;
					}

					//ColorCorrection specific
					if( name == "ColorCorrection" )
					{
						PageControl.Controls[ "FloatParameter0Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter0Text" ].Text = "Red";
						PageControl.Controls[ "FloatParameter0Value" ].Visible = true;
						scrollBarFloatParameters[ 0 ].Visible = true;
						scrollBarFloatParameters[ 0 ].ValueRange = new Range( 0, 5 );
						scrollBarFloatParameters[ 0 ].Value = ColorCorrectionCompositorInstance.Red;

						PageControl.Controls[ "FloatParameter1Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter1Text" ].Text = "Green";
						PageControl.Controls[ "FloatParameter1Value" ].Visible = true;
						scrollBarFloatParameters[ 1 ].Visible = true;
						scrollBarFloatParameters[ 1 ].ValueRange = new Range( 0, 5 );
						scrollBarFloatParameters[ 1 ].Value = ColorCorrectionCompositorInstance.Green;

						PageControl.Controls[ "FloatParameter2Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter2Text" ].Text = "Blue";
						PageControl.Controls[ "FloatParameter2Value" ].Visible = true;
						scrollBarFloatParameters[ 2 ].Visible = true;
						scrollBarFloatParameters[ 2 ].ValueRange = new Range( 0, 5 );
						scrollBarFloatParameters[ 2 ].Value = ColorCorrectionCompositorInstance.Blue;
					}

					//HDR specific
					if( name == "HDR" )
					{
						//Adaptation enable
						PageControl.Controls[ "BoolParameter0" ].Visible = true;
						PageControl.Controls[ "BoolParameter0" ].Text = "Adaptation";
						checkBoxBoolParameters[ 0 ].Checked = HDRCompositorInstance.Adaptation;

						//AdaptationVelocity
						PageControl.Controls[ "FloatParameter1Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter1Text" ].Text = "Adaptation velocity";
						PageControl.Controls[ "FloatParameter1Value" ].Visible = true;
						scrollBarFloatParameters[ 1 ].Visible = true;
						scrollBarFloatParameters[ 1 ].ValueRange = new Range( .1f, 10 );
						scrollBarFloatParameters[ 1 ].Value = HDRCompositorInstance.AdaptationVelocity;

						//AdaptationMiddleBrightness
						PageControl.Controls[ "FloatParameter2Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter2Text" ].Text = "Adaptation middle brightness";
						PageControl.Controls[ "FloatParameter2Value" ].Visible = true;
						scrollBarFloatParameters[ 2 ].Visible = true;
						scrollBarFloatParameters[ 2 ].ValueRange = new Range( .1f, 2 );
						scrollBarFloatParameters[ 2 ].Value = HDRCompositorInstance.AdaptationMiddleBrightness;

						//AdaptationMinimum
						PageControl.Controls[ "FloatParameter3Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter3Text" ].Text = "Adaptation minimum";
						PageControl.Controls[ "FloatParameter3Value" ].Visible = true;
						scrollBarFloatParameters[ 3 ].Visible = true;
						scrollBarFloatParameters[ 3 ].ValueRange = new Range( .1f, 2 );
						scrollBarFloatParameters[ 3 ].Value = HDRCompositorInstance.AdaptationMinimum;

						//AdaptationMaximum
						PageControl.Controls[ "FloatParameter4Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter4Text" ].Text = "Adaptation maximum";
						PageControl.Controls[ "FloatParameter4Value" ].Visible = true;
						scrollBarFloatParameters[ 4 ].Visible = true;
						scrollBarFloatParameters[ 4 ].ValueRange = new Range( .1f, 2 );
						scrollBarFloatParameters[ 4 ].Value = HDRCompositorInstance.AdaptationMaximum;

						//BloomBrightThreshold
						PageControl.Controls[ "FloatParameter5Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter5Text" ].Text = "Bloom bright threshold";
						PageControl.Controls[ "FloatParameter5Value" ].Visible = true;
						scrollBarFloatParameters[ 5 ].Visible = true;
						scrollBarFloatParameters[ 5 ].ValueRange = new Range( .1f, 2.0f );
						scrollBarFloatParameters[ 5 ].Value = HDRCompositorInstance.BloomBrightThreshold;

						//BloomScale
						PageControl.Controls[ "FloatParameter6Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter6Text" ].Text = "Bloom scale";
						PageControl.Controls[ "FloatParameter6Value" ].Visible = true;
						scrollBarFloatParameters[ 6 ].Visible = true;
						scrollBarFloatParameters[ 6 ].ValueRange = new Range( 0, 5 );
						scrollBarFloatParameters[ 6 ].Value = HDRCompositorInstance.BloomScale;

						PageControl.Controls[ "Description" ].Text = "Use Configurator for enable/disable HDR.\n" +
							"Default values can be changed in \"Data\\Definitions\\Renderer.config\".";
					}

					//LDRBloom specific
					if( name == "LDRBloom" )
					{
						//BloomBrightThreshold
						PageControl.Controls[ "FloatParameter0Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter0Text" ].Text = "Bloom bright threshold";
						PageControl.Controls[ "FloatParameter0Value" ].Visible = true;
						scrollBarFloatParameters[ 0 ].Visible = true;
						scrollBarFloatParameters[ 0 ].ValueRange = new Range( .1f, 2.0f );
						scrollBarFloatParameters[ 0 ].Value = LDRBloomCompositorInstance.BloomBrightThreshold;

						//BloomScale
						PageControl.Controls[ "FloatParameter1Text" ].Visible = true;
						PageControl.Controls[ "FloatParameter1Text" ].Text = "Bloom scale";
						PageControl.Controls[ "FloatParameter1Value" ].Visible = true;
						scrollBarFloatParameters[ 1 ].Visible = true;
						scrollBarFloatParameters[ 1 ].ValueRange = new Range( 0, 5 );
						scrollBarFloatParameters[ 1 ].Value = LDRBloomCompositorInstance.BloomScale;
					}
				}
			}

			void UpdateCurrentPostEffect()
			{
				string name = GetListCompositorItemName( listBox.SelectedItem.ToString() );

				bool enabled = checkBoxEnabled.Checked;
				CompositorInstance instance = viewport.GetCompositorInstance( name );

				if( enabled )
				{
					//Enable
					instance = viewport.AddCompositor( name );
					if( instance != null )
						instance.Enabled = true;
				}
				else
				{
					//Disable
					if( name == "MotionBlur" )
					{
						//MotionBlur game specific. No remove compositor. only disable.
						if( instance != null )
							instance.Enabled = false;
					}
					else
						viewport.RemoveCompositor( name );
				}

				if( enabled )
				{
					//MotionBlur specific
					if( name == "MotionBlur" )
					{
						//Update post effect parameters
						MotionBlurCompositorInstance.Blur = scrollBarFloatParameters[ 0 ].Value;
					}

					//Blur specific
					if( name == "Blur" )
					{
						//Update post effect parameters
						BlurCompositorInstance.Fuzziness = scrollBarFloatParameters[ 0 ].Value;
					}

					//RadialBlur specific
					if( name == "RadialBlur" )
					{
						//Update post effect parameters
						RadialBlurCompositorInstance.BlurFactor = scrollBarFloatParameters[ 0 ].Value;
					}

					//ColorCorrection specific
					if( name == "ColorCorrection" )
					{
						//Update post effect parameters
						ColorCorrectionCompositorInstance.Red = scrollBarFloatParameters[ 0 ].Value;
						ColorCorrectionCompositorInstance.Green = scrollBarFloatParameters[ 1 ].Value;
						ColorCorrectionCompositorInstance.Blue = scrollBarFloatParameters[ 2 ].Value;
					}

					//HDR specific
					if( name == "HDR" )
					{
						//Update post effect parameters
						HDRCompositorInstance.Adaptation = checkBoxBoolParameters[ 0 ].Checked;
						HDRCompositorInstance.AdaptationVelocity = scrollBarFloatParameters[ 1 ].Value;
						HDRCompositorInstance.AdaptationMiddleBrightness = scrollBarFloatParameters[ 2 ].Value;
						HDRCompositorInstance.AdaptationMinimum = scrollBarFloatParameters[ 3 ].Value;
						HDRCompositorInstance.AdaptationMaximum = scrollBarFloatParameters[ 4 ].Value;
						HDRCompositorInstance.BloomBrightThreshold = scrollBarFloatParameters[ 5 ].Value;
						HDRCompositorInstance.BloomScale = scrollBarFloatParameters[ 6 ].Value;
					}

					//LDRBloom specific
					if( name == "LDRBloom" )
					{
						//Update post effect parameters
						LDRBloomCompositorInstance.BloomBrightThreshold = scrollBarFloatParameters[ 0 ].Value;
						LDRBloomCompositorInstance.BloomScale = scrollBarFloatParameters[ 1 ].Value;
					}
				}
			}
		}

		/////////////////////////////////////////////

		class ProjectSpecificPage : Page
		{
			public ProjectSpecificPage( string caption, string fileName )
				: base( caption, fileName )
			{
			}

			public override void OnInit()
			{
				base.OnInit();

			}

			public override void OnUpdate()
			{
				base.OnUpdate();

				//code from your brain: add me here.
			}
		}

	}
}
