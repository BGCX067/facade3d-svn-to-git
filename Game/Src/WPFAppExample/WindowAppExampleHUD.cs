// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace WPFAppExample
{
	public class WindowsAppExampleHUD : Control
	{
		public WindowsAppExampleHUD()
		{
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			Control window = ControlDeclarationManager.Instance.CreateControl(
				 "Gui\\WindowsAppExampleHUD.gui" );
			Controls.Add( window );

			( (Button)window.Controls[ "Close" ] ).Click += CloseButton_Click;
		}

		void CloseButton_Click( Button sender )
		{
			SetShouldDetach();
		}
	}
}
