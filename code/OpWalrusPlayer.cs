using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpWalrus
{
	public partial class OpWalrusPlayer : Player
	{
		[Net] public OpWalrusGameInfo.Role role { get; set; }
		List<ModelEntity> clothes;		

		public OpWalrusPlayer() : base()
		{
			clothes = new List<ModelEntity>();
			role = OpWalrusGameInfo.Role.Prisoner;
			this.Inventory = new Inventory( this );
		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();

			Animator = new StandardPlayerAnimator();

			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			base.Respawn();

			dressToTeam();

			((OpWalrusGame)Game.Current).moveToRandomSpawnpoint( this );

			Inventory.DeleteContents();
		}

		public void undress()
		{
			for ( int i = 0; i < clothes.Count; i++ )
			{
				clothes[i].Delete();
			}
			clothes.Clear();
		}

		public void dressToTeam()
		{
			undress();

			List<String> clothesToAdd = new List<String>();

			switch (this.role)
			{
				case OpWalrusGameInfo.Role.Warden:
					//Helmet
					clothesToAdd.Add( "models/hat_securityhelmetnostrap.vmdl" );
					//Legs
					clothesToAdd.Add( "models/trousers.smart.vmdl" );
					//Shirt
					clothesToAdd.Add( "models/shirt_longsleeve.police.vmdl" );
					//Shoes
					clothesToAdd.Add( "models/shoes.police.vmdl" );
					break;
				case OpWalrusGameInfo.Role.Guard:
					//Helmet
					clothesToAdd.Add( "models/hat_securityhelmet.vmdl" );
					//Legs
					clothesToAdd.Add( "models/trousers.smart.vmdl" );
					//Shirt
					clothesToAdd.Add( "models/shirt_longsleeve.police.vmdl" );
					//Shoes
					clothesToAdd.Add( "models/shoes.police.vmdl" );
					break;
				case OpWalrusGameInfo.Role.Prisoner:
					//Helmet
					clothesToAdd.Add( "models/hat_woolly.vmdl" );
					//Legs
					clothesToAdd.Add( "models/trousers_tracksuit.vmdl" );
					//Shirt
					clothesToAdd.Add( "models/jacket.red.vmdl" );
					//Shoes
					clothesToAdd.Add( "models/trainers.vmdl" );
					break;
			}

			for(int i = 0; i < clothesToAdd.Count; i++ )
			{
				string clothesModelString = clothesToAdd[i];
				Log.Info( "Putting on: " + clothesModelString );
				ModelEntity entity = new ModelEntity();
				entity.SetModel( clothesModelString );
				entity.SetParent( this, true );
				entity.EnableShadowInFirstPerson = true;
				entity.EnableHideInFirstPerson = true;
				clothes.Add( entity );
			}
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			SimulateActiveChild( cl, ActiveChild );

			//A jank check for if you are a spectator.
			if(this.Controller is not NoclipController)
			{
				TickPlayerUse();
				
				if ( Input.Pressed( InputButton.Drop ) )
				{
					int activeSlot;
					if ( (activeSlot = Inventory.GetActiveSlot()) != -1 )
					{
						Entity activeEntity = Inventory.GetSlot( activeSlot );
						activeEntity.Spawn();
						activeEntity.Position = EyePos + EyeRot.Forward * 64;

						Inventory.Drop( activeEntity );
					}
				}

				if ( Input.MouseWheel != 0 && Inventory.Count() != 0 )
				{
					int newSpot = Inventory.GetActiveSlot() + Math.Sign( Input.MouseWheel );

					if ( newSpot < -1 )
					{
						newSpot = Inventory.Count() - 1;
					}

					Inventory.SetActiveSlot( newSpot, true );
				}
			}
		}


		//Both are inclusive bound
		public static int wrapInBounds(int i, (int min, int max) bounds)
		{
			int newVal = i;
			int range = 1 + bounds.max - bounds.min;

			if(range == 0)
			{
				Log.Info( "wrapping to none" );
				newVal = bounds.min;
			}

			while(newVal < bounds.min)
			{
				newVal += range;
			}

			while(newVal > bounds.max)
			{
				newVal -= range;
			}

			return newVal;
		}

		[ServerCmd]
		public static void killPlayer(string player)
		{
			OpWalrusPlayer p = OpWalrusUtils.getPlayerByName( All, player );
			p.OnKilled();
		}


		public void setSpectator(bool newSpectatorState)
		{
			//Log.Info( IsServer );
			if( newSpectatorState  == true )
			{ //is setting to spectator
				undress();
				EnableAllCollisions = false;
				EnableDrawing = false;
				Controller = new NoclipController();
			}
			else
			{ //no longer spectator
				dressToTeam();
				EnableAllCollisions = true;
				EnableDrawing = true;
				Controller = new WalkController();
			}
		}

		public override void OnKilled()
		{
			Game.Current?.OnKilled( this );

			//LifeState = LifeState.Dead;
			StopUsing();
		}

		[ServerCmd( "setSpectator" )]
		public static void setSpectatorCmd( string playerName )
		{
			OpWalrusPlayer player = OpWalrusUtils.getPlayerByName( All, playerName );
			IList<OpWalrusPlayer> spectators = ((OpWalrusGame)Game.Current).spectators;
			bool isCurrentlySpectator = spectators.Contains( player );
			if(isCurrentlySpectator)
			{
				spectators.Remove( player );
			}
			else
			{
				spectators.Add( player );
			}
			player.setSpectator( !isCurrentlySpectator );
		}
	}
}
