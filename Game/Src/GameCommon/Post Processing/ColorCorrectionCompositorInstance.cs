// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;

namespace GameCommon
{
	/// <summary>
	/// Represents work with the ColorCorrection post effect.
	/// </summary>
	[CompositorName( "ColorCorrection" )]
	public class ColorCorrectionCompositorInstance : CompositorInstance
	{
		static float red = 1;
		static float green = 1;
		static float blue = 1;

		//

		public static float Red
		{
			get { return red; }
			set { red = value; }
		}

		public static float Green
		{
			get { return green; }
			set { green = value; }
		}

		public static float Blue
		{
			get { return blue; }
			set { blue = value; }
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 500 )
			{
				Vec4 multiplier = new Vec4( Red, Green, Blue, 1 );

				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				parameters.SetNamedConstant( "multiplier", multiplier );
			}
		}
	}
}
