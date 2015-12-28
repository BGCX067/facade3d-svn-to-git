// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a main menu.
	/// </summary>
	public class MainMenuWindow : Control
	{
		static MainMenuWindow instance;

		Control window;
        TextBox versionTextBox;
        SceneNode backgroundImageSceneNode;
        MeshObject menuBg;
        int menuRows = 25;
        int menuColumns = 23;
        JigsawPuzzlePiece[][] menuTiles;
        Angles[][] tileAngles;
        Angles tileDefaultAngle;
        Vec3 tileScale;
        bool shouldTilesRotate = false;
        float rotationTime = 2f;
        float tileRotationTime = 0.75f;
        float timeOfRotateStart;
        bool[][] rotationFinished;
        float[][] tileRotationStarted;
        bool[][] tileRotationTimeSet;
        OptionsWindow options = new OptionsWindow();


		Map mapInstance;

		///////////////////////////////////////////

		public static MainMenuWindow Instance
		{
			get { return instance; }
		}

		/// <summary>
		/// Creates a window of the main menu and creates the background world.
		/// </summary>
		protected override void OnAttach()
		{
			instance = this;
			base.OnAttach();

			//create main menu window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MainMenuWindow.gui" );

			window.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );
			Controls.Add( window );

			//no shader model 3 warning
			if( window.Controls[ "NoShaderModel3" ] != null )
				window.Controls[ "NoShaderModel3" ].Visible = !RenderSystem.Instance.HasShaderModel3();

			//button handlers
			( (Button)window.Controls[ "Run" ] ).Click += Run_Click;
			( (Button)window.Controls[ "Multiplayer" ] ).Click += Multiplayer_Click;
			( (Button)window.Controls[ "Maps" ] ).Click += Maps_Click;
			( (Button)window.Controls[ "LoadSave" ] ).Click += LoadSave_Click;
			( (Button)window.Controls[ "Options" ] ).Click += Options_Click;
			( (Button)window.Controls[ "Profiler" ] ).Click += Profiler_Click;
			( (Button)window.Controls[ "GuiTest" ] ).Click += GuiTest_Click;
			( (Button)window.Controls[ "About" ] ).Click += About_Click;
			( (Button)window.Controls[ "Exit" ] ).Click += Exit_Click;

			//add version info control
			versionTextBox = new TextBox();
			versionTextBox.TextHorizontalAlign = HorizontalAlign.Left;
			versionTextBox.TextVerticalAlign = VerticalAlign.Bottom;
			versionTextBox.Text = "Version " + EngineVersionInformation.Version;
			versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, 0 );

			Controls.Add( versionTextBox );

			//play background music
			GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//create the background world
			CreateMap();

            tileDefaultAngle = new Angles(90, 0, 0);//new Angles(90, -45, 0);
            tileScale = new Vec3(0.1f, 0.1f, 0.1f);

            CreateTiles();

			ResetTime();
		}

        private void CreateTiles()
        {
            menuBg = SceneManager.Instance.CreateMeshObject("JigsawPuzzleGame\\BackgroundImage.mesh");
            
            backgroundImageSceneNode = new SceneNode();
            backgroundImageSceneNode.Position = new Vec3(-5, -5, 0);
            backgroundImageSceneNode.Rotation = tileDefaultAngle.ToQuat();
            backgroundImageSceneNode.Scale = new Vec3(10, 10, 1);
            backgroundImageSceneNode.Attach(menuBg);


            /*JigsawPuzzlePiece piece = (JigsawPuzzlePiece)Entities.Instance.Create(
                "JigsawPuzzlePiece", Map.Instance);
            piece.Position = new Vec3(1, 1, 1);
            piece.Rotation = tileDefaultAngle.ToQuat();
            piece.Scale = tileScale;
            piece.PostCreate();*/
            menuTiles = new JigsawPuzzlePiece[menuRows][];
            for (int i = 0; i < menuRows; i++)
            {
                menuTiles[i] = new JigsawPuzzlePiece[menuColumns];
            }

            tileAngles = new Angles[menuRows][];
            for (int i = 0; i < menuRows; i++)
            {
                tileAngles[i] = new Angles[menuColumns];
            }

            rotationFinished = new bool[menuRows][];
            for (int i = 0; i < menuRows; i++)
            {
                rotationFinished[i] = new bool[menuColumns];
            }

            tileRotationTimeSet = new bool[menuRows][];
            for (int i = 0; i < menuRows; i++)
            {
                tileRotationTimeSet[i] = new bool[menuColumns];
            }

            tileRotationStarted = new float[menuRows][];
            for (int i = 0; i < menuRows; i++)
            {
                tileRotationStarted[i] = new float[menuColumns];
            }

            for (int i = 0; i < menuRows; i++)
            {
                for (int j = 0; j < menuColumns; j++)
                {

                    tileAngles[i][j] = tileDefaultAngle;
                    menuTiles[i][j] = (JigsawPuzzlePiece)Entities.Instance.Create(
                        "JigsawPuzzlePiece", Map.Instance);
                    menuTiles[i][j].Position = new Vec3(0 + 0.09f * j, 1.36f, 1.08f - 0.09f * i);
                    menuTiles[i][j].Rotation = tileDefaultAngle.ToQuat();
                    menuTiles[i][j].Scale = tileScale;
                    menuTiles[i][j].PostCreate();
                    rotationFinished[i][j] = false;
                    tileRotationTimeSet[i][j] = false;

                }
            }


        }

        private void makeTilesRotate(float delta)
        {
            double calc;
            for (int i = 0; i < menuRows; i++)
            {
                for (int j = 0; j < menuColumns; j++)
                {
                    if (!rotationFinished[i][j])
                    {
                        if (((Time - timeOfRotateStart) / rotationTime) * Math.Max(menuRows, menuColumns) > Math.Sqrt(i * i + j * j))
                        //if (j-((Time - timeOfRotateStart) / rotationTime) * Math.Max(menuRows, menuColumns) < i)
                        {
                            calc = 90 - 60 * Math.Sin((3f * (float)j) / 46f);
                            if (tileAngles[i][j].Pitch < calc)//90 - 57 * (Math.Sin(3f * ((((float)j / (float)menuColumns) * 4f) / 8f))))//90 - 60 * (Math.Sqrt((float)j / (float)menuColumns)))//90 - 57 * (Math.Sin(3.14f * ((((float)j / (float)menuColumns) * 4f) / 8f))))
                            {
                                if (tileRotationTimeSet[i][j] == false)
                                {
                                    tileRotationStarted[i][j] = Time;
                                    tileRotationTimeSet[i][j] = true;
                                }
                                tileAngles[i][j].Pitch += (float)calc * ((Time - tileRotationStarted[i][j]) / tileRotationTime);
                                menuTiles[i][j].Rotation = tileAngles[i][j].ToQuat();
                            }
                            else
                            {
                                tileAngles[i][j].Pitch = (float)calc;
                                menuTiles[i][j].Rotation = tileAngles[i][j].ToQuat();
                                rotationFinished[i][j] = true;
                            }
                        }
                    }
                    else
                    {
                        menuTiles[i][j].Visible = false;
                    }
                }
            }
        }

        private void startTilesRotate()
        {
            shouldTilesRotate = true;
            timeOfRotateStart = Time;
        }

		void Run_Click( Button sender )
		{
			GameEngineApp.Instance.SetNeedMapLoad( "Maps\\MainDemo\\Map.map" );
		}

		void Multiplayer_Click( Button sender )
		{
			Controls.Add( new MultiplayerLoginWindow() );
		}

		void Maps_Click( Button sender )
		{
			Controls.Add( new MapsWindow() );
		}

		void LoadSave_Click( Button sender )
		{
			Controls.Add( new WorldLoadSaveWindow() );
		}

		void Options_Click( Button sender )
        {
            startTilesRotate();
			//Controls.Add( options );
		}

		void Profiler_Click( Button sender )
		{
			if( EngineProfilerWindow.Instance == null )
				Controls.Add( new EngineProfilerWindow() );
		}

		void GuiTest_Click( Button sender )
		{
			GameEngineApp.Instance.ControlManager.Controls.Add( new GuiTestWindow() );
		}

		void About_Click( Button sender )
		{
			GameEngineApp.Instance.ControlManager.Controls.Add( new AboutWindow() );
		}

		void Exit_Click( Button sender )
		{
			GameEngineApp.Instance.SetFadeOutScreenAndExit();
		}

		/// <summary>
		/// Destroys the background world at closing the main menu.
		/// </summary>
		protected override void OnDetach()
		{
			//destroy the background world
			DestroyMap();

			base.OnDetach();
			instance = null;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			//if( e.Key == EKeys.Escape )
			//{
			//   Controls.Add( new MenuWindow() );
			//   return true;
			//}

			return false;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//Change window transparency
			{
				float alpha = 0;

				if( Time > 3 && Time <= 5 )
					alpha = ( Time - 3 ) / 2;
				else if( Time > 4 )
					alpha = 1;

				window.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
				versionTextBox.ColorMultiplier = new ColorValue( 1, 1, 1, alpha );
			}

            if (shouldTilesRotate)
            {
                makeTilesRotate(delta);
            }

			//update sound listener
			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			//Tick a background world
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick();
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );
		}

		protected override void OnRender()
		{
			base.OnRender();

			//Update camera orientation
			if( Map.Instance != null )
			{
				Vec3 from = Vec3.Zero;
				Vec3 to = new Vec3( 0, 1, 0 );
				float fov = 80;

				/*MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_MainMenu" ) as MapCamera;
				if( mapCamera != null )
				{
					from = mapCamera.Position;
					to = from + mapCamera.Rotation.GetForward();
					if( mapCamera.Fov != 0 )
						fov = mapCamera.Fov;
				}*/

				Camera camera = RendererWorld.Instance.DefaultCamera;
				camera.NearClipDistance = Map.Instance.NearFarClipDistance.Minimum;
				camera.FarClipDistance = Map.Instance.NearFarClipDistance.Maximum;
				camera.FixedUp = Vec3.ZAxis;
				camera.Fov = fov;
				camera.Position = from;
				camera.LookAt( to );

				//update game specific options
				{
					//water reflection level
					foreach( WaterPlane waterPlane in WaterPlane.Instances )
						waterPlane.ReflectionLevel = GameEngineApp.WaterReflectionLevel;

					//decorative objects
					if( DecorativeObjectManager.Instance != null )
						DecorativeObjectManager.Instance.Visible = GameEngineApp.ShowDecorativeObjects;

					//HeightmapTerrain
					//enable simple rendering for Low material scheme.
					foreach( HeightmapTerrain terrain in HeightmapTerrain.Instances )
						terrain.SimpleRendering = GameEngineApp.MaterialScheme == MaterialSchemes.Low;
				}

			}
		}

		/// <summary>
		/// Creates the background world.
		/// </summary>
		void CreateMap()
		{
			string mapName = "Maps\\ConstructionsDemo\\Map.map";

			if( VirtualFile.Exists( mapName ) )
			{
				WorldType worldType = EntityTypes.Instance.GetByName( "SimpleWorld" ) as WorldType;
				if( worldType == null )
					Log.Fatal( "MainMenuWindow: CreateMap: \"SimpleWorld\" type is not exists." );

				if( GameEngineApp.Instance.ServerOrSingle_MapLoad( mapName, worldType, true ) )
				{
					mapInstance = Map.Instance;
					EntitySystemWorld.Instance.Simulation = true;
				}
			}
		}

		/// <summary>
		/// Destroys the background world.
		/// </summary>
		void DestroyMap()
		{
			if( mapInstance == Map.Instance )
			{
				MapSystemWorld.MapDestroy();
				EntitySystemWorld.Instance.WorldDestroy();
			}
		}
	}
}
