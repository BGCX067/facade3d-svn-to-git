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
	public class Terminal_0 : Engine.EntitySystem.LogicSystem.LogicEntityObject
	{
		GameEntities.GameGuiObject __ownerEntity;
		
		public Terminal_0( GameEntities.GameGuiObject ownerEntity )
			: base( ownerEntity )
		{
			this.__ownerEntity = ownerEntity;
			ownerEntity.PostCreated += delegate( Engine.EntitySystem.Entity __entity, System.Boolean loaded ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )PostCreated( loaded ); };
		}
		
		public GameEntities.GameGuiObject Owner
		{
			get { return __ownerEntity; }
		}
		
		
		public void clear_Click( Engine.UISystem.Button sender )
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				string str = entity.UserData as string;
				if( str != null && str == "AllowClear" )
				{
					entity.SetDeleted();
					continue;
				}
			
				if( ( entity as Corpse ) != null )
					entity.SetDeleted();
			}
			
		}

		public void createZombie_Click( Engine.UISystem.Button sender )
		{
			EntityType type = EntityTypes.Instance.GetByName( "Zombie" );
			
			for( float y = -2; y <= 3; y += 2 )
			{
				bool positionFound = false;
				Vec3 position = Vec3.Zero;
			
				//put unit to free area
				for( float x = -12; x >= -20; x-- )
				{
					Vec3 pos = new Vec3( x, y, 1.2f );
					Bounds bounds = new Bounds(pos - new Vec3(1,1,1), pos + new Vec3(1,1,1));
					bool free = true;
					Map.Instance.GetObjects( bounds, delegate( MapObject o )
					{
						if( o.PhysicsModel != null )
							free = false;
					} );
			
					if( free )
					{
						positionFound = true;
						position = pos;
						break;
					}
				}
			
				if(positionFound)
				{
					Unit obj = (Unit)Entities.Instance.Create( type, Map.Instance );
					obj.UserData = "AllowClear";
					obj.Position = position;
					obj.PostCreate();
				}
			}
			
		}

		public void PostCreated( System.Boolean loaded )
		{
			Engine.EntitySystem.LogicClass __class = Engine.EntitySystem.LogicSystemManager.Instance.MapClassManager.GetByName( "Terminal_0" );
			Engine.EntitySystem.LogicSystem.LogicDesignerMethod __method = (Engine.EntitySystem.LogicSystem.LogicDesignerMethod)__class.GetMethodByName( "PostCreated" );
			__method.Execute( this, new object[ 1 ]{ loaded } );
		}

	}
}
