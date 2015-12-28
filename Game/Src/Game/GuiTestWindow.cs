// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;

namespace Game
{
	public class GuiTestWindow : Control
	{
		Control window;

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//create window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\GuiTestWindow.gui" );
			Controls.Add( window );

			window.Controls[ "CustomShaderMode" ].Enable = RenderSystem.Instance.HasShaderModel2();

			( (Button)window.Controls[ "Close" ] ).Click += Close_Click;
			( (Button)window.Controls[ "PlayVideo" ] ).Click += PlayVideo_Click;

			//Password edit box
			{
				EditBox editBox = window.Controls[ "PasswordEditBox" ] as EditBox;
				if( editBox != null )
				{
					//configure password feature
					editBox.UpdatingTextControl = delegate( EditBox sender, ref string text )
					{
						text = new string( '*', sender.Text.Length );
						if( sender.Focused )
							text += "_";
					};

					editBox.TextChange += passwordEditBox_TextChange;
				}
			}

			//Text typing filter edit box (numbers only)
			{
				EditBox editBox = window.Controls[ "TextTypingFilterEditBox" ] as EditBox;
				if( editBox != null )
				{
					editBox.TextTypingFilter = delegate( EditBox sender, EKeys key, char character, string newText )
					{
						if( character != 0 )
						{
							if( character < '0' || character > '9' )
								return false;
						}
						return true;
					};
				}
			}

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Close();
				return true;
			}

			return false;
		}

		void Close_Click( Button sender )
		{
			Close();
		}

		void PlayVideo_Click( Button sender )
		{
			VideoBox videoBox = window.Controls[ "VideoBox" ] as VideoBox;

			if( string.IsNullOrEmpty( videoBox.FileName ) )
				videoBox.FileName = "_Tests\\Video\\Test.ogv";

			if( videoBox.IsEndOfFile() )
				videoBox.Rewind();
		}

		void Close()
		{
			SetShouldDetach();
		}

		bool IsCustomShaderModeEnabled()
		{
			CheckBox checkBox = window.Controls[ "CustomShaderMode" ] as CheckBox;
			if( checkBox != null && checkBox.Checked )
				return true;
			return false;
		}

		protected override void OnBeforeRenderUIWithChildren( GuiRenderer renderer )
		{
			if( IsCustomShaderModeEnabled() )
			{
				//enable custom shader mode

				List<GuiRenderer.CustomShaderModeTexture> additionalTextures =
					new List<GuiRenderer.CustomShaderModeTexture>();
				additionalTextures.Add( new GuiRenderer.CustomShaderModeTexture(
					"Gui\\Various\\Engine.png", false ) );

				List<GuiRenderer.CustomShaderModeParameter> parameters =
					new List<GuiRenderer.CustomShaderModeParameter>();
				float offsetX = ( EngineApp.Instance.Time / 60 ) % 1;
				Vec2 mouse = EngineApp.Instance.MousePosition;
				parameters.Add( new GuiRenderer.CustomShaderModeParameter( "testParameter",
					new Vec4( offsetX, mouse.X, mouse.Y, 0 ) ) );

				renderer.PushCustomShaderMode( "Materials\\Common\\CustomGuiRenderingExample.cg_hlsl",
					additionalTextures, parameters );

				////second way: bind custom shader mode to this control and to all children.
				//EnableCustomShaderMode( true, "Materials\\Common\\CustomGuiRenderingExample.cg_hlsl",
				//   additionalTextures, parameters );
			}

			base.OnBeforeRenderUIWithChildren( renderer );
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );
		}

		protected override void OnAfterRenderUIWithChildren( GuiRenderer renderer )
		{
			DrawTrianglesAndLines( renderer );

			base.OnAfterRenderUIWithChildren( renderer );

			//disable custom shader mode
			if( IsCustomShaderModeEnabled() )
				renderer.PopCustomShaderMode();
		}

		void DrawTrianglesAndLines( GuiRenderer renderer )
		{
			List<GuiRenderer.TriangleVertex> vertices = new List<GuiRenderer.TriangleVertex>( 32 );

			Vec2 center = new Vec2( .7f, .4f );
			float radius = .1f;

			float step = MathFunctions.PI * 2 / 16;
			Vec2 lastPoint = Vec2.Zero;
			for( float angle = 0; angle <= MathFunctions.PI * 2 + step * .5f; angle += step )
			{
				Vec2 point = center + new Vec2(
					MathFunctions.Cos( angle ) * radius / renderer.AspectRatio,
					MathFunctions.Sin( angle ) * radius );

				if( angle != 0 )
				{
					vertices.Add( new GuiRenderer.TriangleVertex( center, new ColorValue( 1, 0, 0 ) ) );
					vertices.Add( new GuiRenderer.TriangleVertex( lastPoint, new ColorValue( 0, 1, 0 ) ) );
					vertices.Add( new GuiRenderer.TriangleVertex( point, new ColorValue( 0, 1, 0 ) ) );
				}

				lastPoint = point;
			}

			renderer.AddTriangles( vertices );

			Rect rect = new Rect(
				center.X - radius / renderer.AspectRatio, center.Y - radius,
				center.X + radius / renderer.AspectRatio, center.Y + radius );
			renderer.AddRectangle( rect, new ColorValue( 1, 1, 0 ) );
		}

		void passwordEditBox_TextChange( Control sender )
		{
			Control control = window.Controls[ "PasswordEditBoxCheck" ];
			if( control != null )
				control.Text = sender.Text;
		}

	}
}
