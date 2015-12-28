// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
	public enum MaterialSchemes
	{
		//no normal mapping, no receiving shadows, no specular.
		//Game: HeightmapTerrain will use SimpleRendering mode for this scheme.
		//Game: used for generation WaterPlane reflection.
		Low,

		//High. Maximum quality.
		//Resource Editor and Map Editor uses "Default" scheme by default.
		Default
	}
}
