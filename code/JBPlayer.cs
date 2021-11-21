using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpWalrus
{
	public partial class JBPlayer : Player
	{
		private DamageInfo lastDamage;

		[Net] public JBGameInfo.Role role { get; set; }
		[Net] public bool optinWarden { get; set; }
		List<ModelEntity> clothes;		

		public JBPlayer() : base()
		{
			clothes = new List<ModelEntity>();
			role = JBGameInfo.Role.Prisoner;
			this.Inventory = new Inventory( this );

			optinWarden = false;
		}

		public override void Respawn()
		{
			base.Respawn();

			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();

			Animator = new StandardPlayerAnimator();

			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;


			dressToTeam();

			((JBGame)Game.Current).moveToRandomSpawnpoint( this );

			Inventory.DeleteContents();
			Inventory.Add( new SWB_WEAPONS.Bayonet() );
		}

		[ServerCmd]
		public static void flipWardenOptin()
		{
			// ^= true flips the bool
			((JBPlayer)ConsoleSystem.Caller.Pawn).optinWarden ^= true;
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
				case JBGameInfo.Role.Warden:
					//Helmet
					clothesToAdd.Add( "models/hat_securityhelmetnostrap.vmdl" );
					//Legs
					clothesToAdd.Add( "models/trousers.smart.vmdl" );
					//Shirt
					clothesToAdd.Add( "models/shirt_longsleeve.police.vmdl" );
					//Shoes
					clothesToAdd.Add( "models/shoes.police.vmdl" );
					break;
				case JBGameInfo.Role.Guard:
					//Helmet
					clothesToAdd.Add( "models/hat_securityhelmet.vmdl" );
					//Legs
					clothesToAdd.Add( "models/trousers.smart.vmdl" );
					//Shirt
					clothesToAdd.Add( "models/shirt_longsleeve.police.vmdl" );
					//Shoes
					clothesToAdd.Add( "models/shoes.police.vmdl" );
					break;
				case JBGameInfo.Role.Prisoner:
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
						activeEntity.Position = EyePos + EyeRot.Forward * 48;

						Inventory.Drop( activeEntity );

						activeEntity.Velocity += EyeRot.Forward * 400;
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

				using(Prediction.Off())
				{
					if ( Input.Pressed( InputButton.Flashlight ) && IsClient)
					{
						TraceResult tr = Trace.Ray( EyePos, EyePos + EyeRot.Forward * 2048 ).Ignore( this ).Run();
						JBGame.createPing( tr.EndPos, tr.Normal );
					}
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
			JBPlayer p = JBUtils.getPlayerByName( All, player );
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
			BecomeRagdollOnClient( Velocity, lastDamage.Flags, lastDamage.Position, lastDamage.Force, GetHitboxBone( lastDamage.HitboxIndex ) );
			StopUsing();
		}

		public override void TakeDamage( DamageInfo info )
		{
			/*if ( GetHitboxGroup( info.HitboxIndex ) == 1 )
			{
				info.Damage *= 10.0f;
			}*/
			// above is the headshot code from sandbox, uncomment for headshots

			lastDamage = info;

			//TookDamage( lastDamage.Flags, lastDamage.Position, lastDamage.Force );
			// this was a rpc call to a function with nothing in it

			base.TakeDamage( info );
		}

		[ServerCmd( "setSpectator" )]
		public static void setSpectatorCmd( string playerName )
		{
			JBPlayer player = JBUtils.getPlayerByName( All, playerName );
			IList<JBPlayer> spectators = ((JBGame)Game.Current).spectators;
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
