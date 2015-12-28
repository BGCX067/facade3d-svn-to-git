// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Renderer;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a about us window.
	/// </summary>
	public class AboutWindow : Control
	{
		protected override void OnAttach()
		{
			base.OnAttach();

			Control window = ControlDeclarationManager.Instance.CreateControl( 
				"Gui\\AboutWindow.gui" );
			Controls.Add( window );

			window.Controls[ "Version" ].Text = EngineVersionInformation.Version;
			window.Controls[ "Copyright" ].Text = EngineVersionInformation.Copyright;
			window.Controls[ "WWW" ].Text = EngineVersionInformation.WWW;

			( (Button)window.Controls[ "Quit" ] ).Click += delegate( Button sender )
			{
				SetShouldDetach();
			};

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}
			return false;
		}
	}
}
