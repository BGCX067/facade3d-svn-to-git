// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines an input point in the application.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			if( Debugger.IsAttached )
			{
				Main2();
			}
			else
			{
				try
				{
					Main2();
				}
				catch( Exception e )
				{
					Log.FatalAsException( e.ToString() );
				}
			}
		}

		static void Main2()
		{
			if( !VirtualFileSystem.Init( "user:Logs/Game.log", true, null, null, null ) )
				return;

			EngineApp.ConfigName = "user:Configs/Game.config";

			if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
				EngineApp.UseDirectInputForMouseRelativeMode = true;
			EngineApp.AllowJoysticksAndCustomInputDevices = true;
			EngineApp.AllowWriteEngineConfigFile = true;
			EngineApp.AllowChangeVideoMode = true;

			EngineApp.Init( new GameEngineApp() );
			EngineApp.Instance.WindowTitle = "Game";

			if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
				EngineApp.Instance.Icon = Game.Properties.Resources.Logo;

			EngineConsole.Init();

			EngineApp.Instance.Config.RegisterClassParameters( typeof( GameEngineApp ) );

			//EngineApp.Instance.SuspendWorkingWhenApplicationIsNotActive = false;

			if( EngineApp.Instance.Create() )
				EngineApp.Instance.Run();

			EngineApp.Shutdown();

			Log.DumpToFile( "Program END\r\n" );

			VirtualFileSystem.Shutdown();
		}

		public static void WebPlayer_Message( EngineApp.WebPlayerMessages message, IntPtr data )
		{
			try
			{
				switch( message )
				{
				case EngineApp.WebPlayerMessages.Init:

					unsafe
					{
						EngineApp.WebPlayerInitData* initData = (EngineApp.WebPlayerInitData*)data;

						if( !VirtualFileSystem.Init( "user:Logs/WebPlayer.log", false,
							initData->ExecutableDirectoryPath, null, null ) )
							return;

						//set render settings
						//RendererWorld.InitializationOptions.MaxPixelShadersVersion = RendererWorld.MaxPixelShadersVersions.PS0;
						//RendererWorld.InitializationOptions.MaxVertexShadersVersion = RendererWorld.MaxVertexShadersVersions.VS0;
						//RendererWorld.InitializationOptions.FullSceneAntialiasing = 0;
						//RendererWorld.InitializationOptions.FilteringMode = RendererWorld.FilteringModes.Trilinear;

						EngineApp.Init( new GameEngineApp() );

						EngineApp.WebPlayer_Message( message, data );

						EngineConsole.Init();

						EngineApp.Instance.Config.RegisterClassParameters( typeof( GameEngineApp ) );

						EngineApp.Instance.Create();
					}
					break;

				case EngineApp.WebPlayerMessages.Shutdown:

					EngineApp.WebPlayer_Message( message, data );

					EngineApp.Shutdown();
					Log.DumpToFile( "Program END\r\n" );
					VirtualFileSystem.Shutdown();

					break;

				case EngineApp.WebPlayerMessages.WindowMessage:
					EngineApp.WebPlayer_Message( message, data );
					break;
				}

			}
			catch( Exception e )
			{
				Log.FatalAsException( e.ToString() );
			}
		}

	}
}
