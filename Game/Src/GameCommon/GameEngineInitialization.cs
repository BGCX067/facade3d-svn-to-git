// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;

namespace GameCommon
{
	/// <summary>
	/// Class for execute actions after initialization of the engine.
	/// </summary>
	/// <remarks>
	/// It is class works in simulation application and editors (Resource Editor, Map Editor).
	/// </remarks>
	public class GameEngineInitialization : EngineInitialization
	{
		protected override bool OnInit()
		{
			CorrectRenderTechnique();

			//Initialize HDR compositor for HDR render technique
			if( EngineApp.RenderTechnique == "HDR" )
				InitializeHDRCompositor();

			//Initialize LDRBloom for standard technique
			if( EngineApp.RenderTechnique == "Standard" )
			{
				if( IsActivateLDRBloomByDefault() )
					InitializeLDRBloomCompositor();
			}

			return true;
		}

		bool IsHDRSupported()
		{
			Compositor compositor = CompositorManager.Instance.GetByName( "HDR" );
			if( compositor == null || !compositor.IsSupported() )
				return false;

			bool floatTexturesSupported = TextureManager.Instance.IsEquivalentFormatSupported(
				Texture.Type.Type2D, PixelFormat.Float16RGB, Texture.Usage.RenderTarget );
			if( !floatTexturesSupported )
				return false;

			if( RenderSystem.Instance.GPUIsGeForce() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV30 )
				return false;
			if( RenderSystem.Instance.GPUIsRadeon() &&
				RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
				RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
				return false;

			return true;
		}

		bool IsActivateHDRByDefault()
		{
			//HDR is disabled by default.

			//if( IsHDRSupported() )
			//{
			//   if( RenderSystem.Instance.GPUIsGeForce() )
			//   {
			//      if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
			//         RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
			//      {
			//         return false;
			//      }
			//      else
			//         return true;
			//   }
			//   if( RenderSystem.Instance.GPUIsRadeon() )
			//   {
			//      if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
			//         RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
			//      {
			//         return false;
			//      }
			//      else
			//         return true;
			//   }
			//}

			return false;
		}

		bool IsActivateLDRBloomByDefault()
		{
			//if( RenderSystem.Instance.HasShaderModel2() )
			//   return true;

			return false;
		}

		void CorrectRenderTechnique()
		{
			//HDR choose by default
			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) && IsHDRSupported() )
				EngineApp.RenderTechnique = IsActivateHDRByDefault() ? "HDR" : "Standard";

			//HDR render technique support check
			if( EngineApp.RenderTechnique == "HDR" && !IsHDRSupported() )
			{
				bool nullRenderSystem = RenderSystem.Instance.Name.ToLower().Contains( "null" );

				if( !nullRenderSystem )//no warning for null render system
				{
					Log.Warning( "HDR render technique is not supported. " +
						"Using \"Standard\" render technique." );
				}
				EngineApp.RenderTechnique = "Standard";
			}

			if( string.IsNullOrEmpty( EngineApp.RenderTechnique ) )
				EngineApp.RenderTechnique = "Standard";
		}

		void InitializeHDRCompositor()
		{
			//Add HDR compositor
			HDRCompositorInstance instance = (HDRCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "HDR", 0 );

			if( instance != null )
				instance.Enabled = true;
		}

		void InitializeLDRBloomCompositor()
		{
			LDRBloomCompositorInstance instance = (LDRBloomCompositorInstance)
				RendererWorld.Instance.DefaultViewport.AddCompositor( "LDRBloom", 0 );

			if( instance != null )
				instance.Enabled = true;
		}

	}
}
