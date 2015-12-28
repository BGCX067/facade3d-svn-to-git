using System;
using System.Collections.Generic;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Maps_SwitchesDemo_LogicSystem_LogicSystemScripts
{
	public class PipeSwitch_Gas : Engine.EntitySystem.LogicSystem.LogicEntityObject
	{
		GameEntities.TurnFloatSwitch __ownerEntity;
		
		public PipeSwitch_Gas( GameEntities.TurnFloatSwitch ownerEntity )
			: base( ownerEntity )
		{
			this.__ownerEntity = ownerEntity;
			ownerEntity.ValueChange += delegate( GameEntities.Switch __entity ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )ValueChange(  ); };
		}
		
		public GameEntities.TurnFloatSwitch Owner
		{
			get { return __ownerEntity; }
		}
		
		
		public void ValueChange()
		{
			Engine.EntitySystem.LogicClass __class = Engine.EntitySystem.LogicSystemManager.Instance.MapClassManager.GetByName( "PipeSwitch_Gas" );
			Engine.EntitySystem.LogicSystem.LogicDesignerMethod __method = (Engine.EntitySystem.LogicSystem.LogicDesignerMethod)__class.GetMethodByName( "ValueChange" );
			__method.Execute( this, new object[ 0 ]{  } );
		}

	}
}
