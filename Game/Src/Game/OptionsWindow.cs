// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a window of options.
	/// </summary>
	public class OptionsWindow : Control
	{
		Control window;
		ComboBox comboBoxResolution;
		ComboBox comboBoxInputDevices;

		///////////////////////////////////////////

		public class ShadowTechniqueItem
		{
			ShadowTechniques technique;
			string text;

			public ShadowTechniqueItem( ShadowTechniques technique, string text )
			{
				this.technique = technique;
				this.text = text;
			}

			public ShadowTechniques Technique
			{
				get { return technique; }
			}

			public override string ToString()
			{
				return text;
			}
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			ComboBox comboBox;
			ScrollBar scrollBar;
			CheckBox checkBox;

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\OptionsWindow.gui" );
			Controls.Add( window );

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;

			( (Button)window.Controls[ "Options" ].Controls[ "Quit" ] ).Click += delegate( Button sender )
			{
				SetShouldDetach();
			};

			//pageVideo
			{
				Control pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

				Vec2i currentMode = EngineApp.Instance.VideoMode;

				//screenResolutionComboBox
				comboBox = (ComboBox)pageVideo.Controls[ "ScreenResolution" ];
				comboBox.Enable = !EngineApp.Instance.WebPlayerMode;
				comboBoxResolution = comboBox;

				foreach( Vec2i mode in DisplaySettings.VideoModes )
				{
					if( mode.X < 640 )
						continue;

					comboBox.Items.Add( mode.X.ToString() + "x" + mode.Y.ToString() );

					if( mode == currentMode )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}

				comboBox.SelectedIndexChange += delegate( ComboBox sender )
				{
					ChangeVideoMode();
				};

				//gamma
				scrollBar = (ScrollBar)pageVideo.Controls[ "Gamma" ];
				scrollBar.Value = GameEngineApp._Gamma;
				scrollBar.Enable = !EngineApp.Instance.WebPlayerMode;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					GameEngineApp._Gamma = value;
					pageVideo.Controls[ "GammaValue" ].Text = value.ToString( "F1" );
				};
				pageVideo.Controls[ "GammaValue" ].Text = GameEngineApp._Gamma.ToString( "F1" );

				//MaterialScheme
				{
					comboBox = (ComboBox)pageVideo.Controls[ "MaterialScheme" ];
					foreach( MaterialSchemes materialScheme in
						Enum.GetValues( typeof( MaterialSchemes ) ) )
					{
						comboBox.Items.Add( materialScheme.ToString() );

						if( GameEngineApp.MaterialScheme == materialScheme )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
							GameEngineApp.MaterialScheme = (MaterialSchemes)sender.SelectedIndex;
					};
				}

				//ShadowTechnique
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowTechnique" ];

					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.None, "None" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapLow, "Shadowmap Low" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapMedium, "Shadowmap Medium" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapHigh, "Shadowmap High" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapLowPSSM, "PSSMx3 Low" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapMediumPSSM, "PSSMx3 Medium" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.ShadowmapHighPSSM, "PSSMx3 High" ) );
					comboBox.Items.Add( new ShadowTechniqueItem( ShadowTechniques.Stencil, "Stencil" ) );

					for( int n = 0; n < comboBox.Items.Count; n++ )
					{
						ShadowTechniqueItem item = (ShadowTechniqueItem)comboBox.Items[ n ];
						if( item.Technique == GameEngineApp.ShadowTechnique )
							comboBox.SelectedIndex = n;
					}

					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
						{
							ShadowTechniqueItem item = (ShadowTechniqueItem)sender.SelectedItem;
							GameEngineApp.ShadowTechnique = item.Technique;
						}
						UpdateShadowControlsEnable();
					};
					UpdateShadowControlsEnable();
				}

				//ShadowUseMapSettings
				{
					checkBox = (CheckBox)pageVideo.Controls[ "ShadowUseMapSettings" ];
					checkBox.Checked = GameEngineApp.ShadowUseMapSettings;
					checkBox.CheckedChange += delegate( CheckBox sender )
					{
						GameEngineApp.ShadowUseMapSettings = sender.Checked;
						if( sender.Checked && Map.Instance != null )
						{
							GameEngineApp.ShadowPSSMSplitFactors = Map.Instance.InitialShadowPSSMSplitFactors;
							GameEngineApp.ShadowFarDistance = Map.Instance.InitialShadowFarDistance;
							GameEngineApp.ShadowColor = Map.Instance.InitialShadowColor;
						}

						UpdateShadowControlsEnable();

						if( sender.Checked )
						{
							( (ScrollBar)pageVideo.Controls[ "ShadowFarDistance" ] ).Value =
								GameEngineApp.ShadowFarDistance;

							pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
								( (int)GameEngineApp.ShadowFarDistance ).ToString();

							ColorValue color = GameEngineApp.ShadowColor;
							( (ScrollBar)pageVideo.Controls[ "ShadowColor" ] ).Value =
								( color.Red + color.Green + color.Blue ) / 3;
						}
					};
				}

				//ShadowPSSMSplitFactor1
				scrollBar = (ScrollBar)pageVideo.Controls[ "ShadowPSSMSplitFactor1" ];
				scrollBar.Value = GameEngineApp.ShadowPSSMSplitFactors[ 0 ];
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowPSSMSplitFactors = new Vec2(
						sender.Value, GameEngineApp.ShadowPSSMSplitFactors[ 1 ] );
					pageVideo.Controls[ "ShadowPSSMSplitFactor1Value" ].Text =
						( GameEngineApp.ShadowPSSMSplitFactors[ 0 ].ToString( "F2" ) ).ToString();
				};
				pageVideo.Controls[ "ShadowPSSMSplitFactor1Value" ].Text =
					( GameEngineApp.ShadowPSSMSplitFactors[ 0 ].ToString( "F2" ) ).ToString();

				//ShadowPSSMSplitFactor2
				scrollBar = (ScrollBar)pageVideo.Controls[ "ShadowPSSMSplitFactor2" ];
				scrollBar.Value = GameEngineApp.ShadowPSSMSplitFactors[ 1 ];
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowPSSMSplitFactors = new Vec2(
						GameEngineApp.ShadowPSSMSplitFactors[ 0 ], sender.Value );
					pageVideo.Controls[ "ShadowPSSMSplitFactor2Value" ].Text =
						( GameEngineApp.ShadowPSSMSplitFactors[ 1 ].ToString( "F2" ) ).ToString();
				};
				pageVideo.Controls[ "ShadowPSSMSplitFactor2Value" ].Text =
					( GameEngineApp.ShadowPSSMSplitFactors[ 1 ].ToString( "F2" ) ).ToString();

				//ShadowFarDistance
				scrollBar = (ScrollBar)pageVideo.Controls[ "ShadowFarDistance" ];
				scrollBar.Value = GameEngineApp.ShadowFarDistance;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.ShadowFarDistance = sender.Value;
					pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
						( (int)GameEngineApp.ShadowFarDistance ).ToString();
				};
				pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
					( (int)GameEngineApp.ShadowFarDistance ).ToString();

				//ShadowColor
				scrollBar = (ScrollBar)pageVideo.Controls[ "ShadowColor" ];
				scrollBar.Value = ( GameEngineApp.ShadowColor.Red + GameEngineApp.ShadowColor.Green +
					GameEngineApp.ShadowColor.Blue ) / 3;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					float color = sender.Value;
					GameEngineApp.ShadowColor = new ColorValue( color, color, color, color );
				};

				//ShadowDirectionalLightTextureSize
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowDirectionalLightTextureSize" ];
					comboBox.Items.Add( 256 );
					comboBox.Items.Add( 512 );
					comboBox.Items.Add( 1024 );
					comboBox.Items.Add( 2048 );
					if( GameEngineApp.ShadowDirectionalLightTextureSize == 256 )
						comboBox.SelectedIndex = 0;
					if( GameEngineApp.ShadowDirectionalLightTextureSize == 512 )
						comboBox.SelectedIndex = 1;
					else if( GameEngineApp.ShadowDirectionalLightTextureSize == 1024 )
						comboBox.SelectedIndex = 2;
					else if( GameEngineApp.ShadowDirectionalLightTextureSize == 2048 )
						comboBox.SelectedIndex = 3;
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowDirectionalLightTextureSize = (int)sender.SelectedItem;
					};
				}

				////ShadowDirectionalLightMaxTextureCount
				//{
				//   comboBox = (EComboBox)pageVideo.Controls[ "ShadowDirectionalLightMaxTextureCount" ];
				//   for( int n = 0; n < 3; n++ )
				//   {
				//      int count = n + 1;
				//      comboBox.Items.Add( count );
				//      if( count == GameEngineApp.ShadowDirectionalLightMaxTextureCount )
				//         comboBox.SelectedIndex = n;
				//   }
				//   comboBox.SelectedIndexChange += delegate( EComboBox sender )
				//   {
				//      GameEngineApp.ShadowDirectionalLightMaxTextureCount = (int)sender.SelectedItem;
				//   };
				//}

				//ShadowSpotLightTextureSize
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowSpotLightTextureSize" ];
					comboBox.Items.Add( 256 );
					comboBox.Items.Add( 512 );
					comboBox.Items.Add( 1024 );
					comboBox.Items.Add( 2048 );
					if( GameEngineApp.ShadowSpotLightTextureSize == 256 )
						comboBox.SelectedIndex = 0;
					if( GameEngineApp.ShadowSpotLightTextureSize == 512 )
						comboBox.SelectedIndex = 1;
					else if( GameEngineApp.ShadowSpotLightTextureSize == 1024 )
						comboBox.SelectedIndex = 2;
					else if( GameEngineApp.ShadowSpotLightTextureSize == 2048 )
						comboBox.SelectedIndex = 3;
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowSpotLightTextureSize = (int)sender.SelectedItem;
					};
				}

				//ShadowSpotLightMaxTextureCount
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowSpotLightMaxTextureCount" ];
					for( int n = 0; n < 3; n++ )
					{
						int count = n + 1;
						comboBox.Items.Add( count );
						if( count == GameEngineApp.ShadowSpotLightMaxTextureCount )
							comboBox.SelectedIndex = n;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowSpotLightMaxTextureCount = (int)sender.SelectedItem;
					};
				}

				//ShadowPointLightTextureSize
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowPointLightTextureSize" ];
					comboBox.Items.Add( 256 );
					comboBox.Items.Add( 512 );
					comboBox.Items.Add( 1024 );
					comboBox.Items.Add( 2048 );
					if( GameEngineApp.ShadowPointLightTextureSize == 256 )
						comboBox.SelectedIndex = 0;
					if( GameEngineApp.ShadowPointLightTextureSize == 512 )
						comboBox.SelectedIndex = 1;
					else if( GameEngineApp.ShadowPointLightTextureSize == 1024 )
						comboBox.SelectedIndex = 2;
					else if( GameEngineApp.ShadowPointLightTextureSize == 2048 )
						comboBox.SelectedIndex = 3;
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowPointLightTextureSize = (int)sender.SelectedItem;
					};
				}

				//ShadowPointLightMaxTextureCount
				{
					comboBox = (ComboBox)pageVideo.Controls[ "ShadowPointLightMaxTextureCount" ];
					for( int n = 0; n < 3; n++ )
					{
						int count = n + 1;
						comboBox.Items.Add( count );
						if( count == GameEngineApp.ShadowPointLightMaxTextureCount )
							comboBox.SelectedIndex = n;
					}
					comboBox.SelectedIndexChange += delegate( ComboBox sender )
					{
						GameEngineApp.ShadowPointLightMaxTextureCount = (int)sender.SelectedItem;
					};
				}

				//fullScreen
				checkBox = (CheckBox)pageVideo.Controls[ "FullScreen" ];
				checkBox.Enable = !EngineApp.Instance.WebPlayerMode;
				checkBox.Checked = EngineApp.Instance.FullScreen;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					EngineApp.Instance.FullScreen = sender.Checked;
				};

				//waterReflectionLevel
				comboBox = (ComboBox)pageVideo.Controls[ "WaterReflectionLevel" ];
				foreach( WaterPlane.ReflectionLevels level in Enum.GetValues(
					typeof( WaterPlane.ReflectionLevels ) ) )
				{
					comboBox.Items.Add( level );
					if( GameEngineApp.WaterReflectionLevel == level )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}
				comboBox.SelectedIndexChange += delegate( ComboBox sender )
				{
					GameEngineApp.WaterReflectionLevel = (WaterPlane.ReflectionLevels)sender.SelectedItem;
				};

				//showDecorativeObjects
				checkBox = (CheckBox)pageVideo.Controls[ "ShowDecorativeObjects" ];
				checkBox.Checked = GameEngineApp.ShowDecorativeObjects;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp.ShowDecorativeObjects = sender.Checked;
				};

				//showSystemCursorCheckBox
				checkBox = (CheckBox)pageVideo.Controls[ "ShowSystemCursor" ];
				checkBox.Checked = GameEngineApp._ShowSystemCursor;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp._ShowSystemCursor = sender.Checked;
					sender.Checked = GameEngineApp._ShowSystemCursor;
				};

				//showFPSCheckBox
				checkBox = (CheckBox)pageVideo.Controls[ "ShowFPS" ];
				checkBox.Checked = GameEngineApp._DrawFPS;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					GameEngineApp._DrawFPS = sender.Checked;
					sender.Checked = GameEngineApp._DrawFPS;
				};

			}

			//pageSound
			{
				Control pageSound = window.Controls[ "TabControl" ].Controls[ "Sound" ];

				//soundVolumeCheckBox
				scrollBar = (ScrollBar)pageSound.Controls[ "SoundVolume" ];
				scrollBar.Value = GameEngineApp.SoundVolume;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.SoundVolume = sender.Value;
				};

				//musicVolumeCheckBox
				scrollBar = (ScrollBar)pageSound.Controls[ "MusicVolume" ];
				scrollBar.Value = GameEngineApp.MusicVolume;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					GameEngineApp.MusicVolume = sender.Value;
				};
			}

			//pageControls
			{
				Control pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

				//MouseHSensitivity
				scrollBar = (ScrollBar)pageControls.Controls[ "MouseHSensitivity" ];
				scrollBar.Value = GameControlsManager.Instance.MouseSensitivity.X;
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.X = sender.Value;
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVSensitivity
				scrollBar = (ScrollBar)pageControls.Controls[ "MouseVSensitivity" ];
				scrollBar.Value = Math.Abs( GameControlsManager.Instance.MouseSensitivity.Y );
				scrollBar.ValueChange += delegate( ScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					bool invert = ( (CheckBox)pageControls.Controls[ "MouseVInvert" ] ).Checked;
					value.Y = sender.Value * ( invert ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVInvert
				checkBox = (CheckBox)pageControls.Controls[ "MouseVInvert" ];
				checkBox.Checked = GameControlsManager.Instance.MouseSensitivity.Y < 0;
				checkBox.CheckedChange += delegate( CheckBox sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.Y =
						( (ScrollBar)pageControls.Controls[ "MouseVSensitivity" ] ).Value *
						( sender.Checked ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//Devices
				comboBox = (ComboBox)pageControls.Controls[ "InputDevices" ];
				comboBoxInputDevices = comboBox;
				comboBox.Items.Add( "Keyboard/Mouse" );
				if( InputDeviceManager.Instance != null )
				{
					foreach( InputDevice device in InputDeviceManager.Instance.Devices )
						comboBox.Items.Add( device );
				}
				comboBox.SelectedIndex = 0;

				comboBox.SelectedIndexChange += delegate( ComboBox sender )
				{
					UpdateBindedInputControlsTextBox();
				};

				//Controls
				UpdateBindedInputControlsTextBox();
			}
		}

		void UpdateShadowControlsEnable()
		{
			Control pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

			bool textureShadows =
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLow ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMedium ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHigh ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHighPSSM;

			bool pssm = GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLowPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapMediumPSSM ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHighPSSM;

			bool allowShadowColor = GameEngineApp.ShadowTechnique != ShadowTechniques.None;

			pageVideo.Controls[ "ShadowColor" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && allowShadowColor;

			pageVideo.Controls[ "ShadowPSSMSplitFactor1" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && pssm;

			pageVideo.Controls[ "ShadowPSSMSplitFactor2" ].Enable =
				!GameEngineApp.ShadowUseMapSettings && pssm;

			pageVideo.Controls[ "ShadowFarDistance" ].Enable =
				!GameEngineApp.ShadowUseMapSettings &&
				GameEngineApp.ShadowTechnique != ShadowTechniques.None;

			pageVideo.Controls[ "ShadowDirectionalLightTextureSize" ].Enable = textureShadows;
			//pageVideo.Controls[ "ShadowDirectionalLightMaxTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowSpotLightTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowSpotLightMaxTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowPointLightTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowPointLightMaxTextureCount" ].Enable = textureShadows;
		}

		void ChangeVideoMode()
		{
			Vec2i size;
			{
				size = EngineApp.Instance.VideoMode;

				if( comboBoxResolution.SelectedIndex != -1 )
				{
					string s = (string)( comboBoxResolution ).SelectedItem;
					s = s.Replace( "x", " " );
					size = Vec2i.Parse( s );
				}
			}

			EngineApp.Instance.VideoMode = size;
		}

		void UpdateBindedInputControlsTextBox()
		{
			Control pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

			//!!!!temp

			string text = "Configuring of custom controls is not implemented\n";
			text += "\n";

			InputDevice inputDevice = comboBoxInputDevices.SelectedItem as InputDevice;

			text += "Binded keys:\n\n";

			foreach( GameControlsManager.GameControlItem item in
				GameControlsManager.Instance.Items )
			{
				string valueStr = "";

				//keys and mouse buttons
				if( inputDevice == null )
				{
					foreach( GameControlsManager.SystemKeyboardMouseValue value in
						item.DefaultKeyboardMouseValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemKeyboardMouseValue.Types.Key:
							valueStr += string.Format( "Key: {0}", value.Key );
							break;

						case GameControlsManager.SystemKeyboardMouseValue.Types.MouseButton:
							valueStr += string.Format( "MouseButton: {0}", value.MouseButton );
							break;
						}
					}
				}

				//joystick
				JoystickInputDevice joystickInputDevice = inputDevice as JoystickInputDevice;
				if( joystickInputDevice != null )
				{
					foreach( GameControlsManager.SystemJoystickValue value in
						item.DefaultJoystickValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemJoystickValue.Types.Button:
							if( joystickInputDevice.GetButtonByName( value.Button ) != null )
								valueStr += string.Format( "Button: {0}", value.Button );
							break;

						case GameControlsManager.SystemJoystickValue.Types.Axis:
							if( joystickInputDevice.GetAxisByName( value.Axis ) != null )
								valueStr += string.Format( "Axis: {0}({1})", value.Axis, value.AxisFilter );
							break;

						case GameControlsManager.SystemJoystickValue.Types.POV:
							if( joystickInputDevice.GetPOVByName( value.POV ) != null )
								valueStr += string.Format( "POV: {0}({1})", value.POV, value.POVDirection );
							break;
						}
					}
				}

				if( valueStr != "" )
					text += string.Format( "{0} - {1}\n", item.ControlKey.ToString(), valueStr );
			}

			pageControls.Controls[ "Controls" ].Text = text;
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
