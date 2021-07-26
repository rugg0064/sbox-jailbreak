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

			if( this.Inventory != null )
			{
				Inventory.DeleteContents();
			}

			if (OpWalrusGameInfo.roleToTeam[this.role] == OpWalrusGameInfo.Team.Guards)
			{
				Log.Info( "Is a guard" );
				Inventory.DeleteContents();
				Pistol pistol;
				Inventory.Add( pistol = new Pistol(), true );
			}
		}

		public void dressToTeam()
		{
			for (int i = 0; i < clothes.Count; i++ )
			{
				clothes[i].Delete();
			}
			clothes.Clear();

			string helmetModelString = null;
			switch (this.role)
			{
				case OpWalrusGameInfo.Role.Warden:
					helmetModelString = "addons/citizen/models/citizen_clothes/hat/hat_securityhelmetnostrap.vmdl";
					break;
				case OpWalrusGameInfo.Role.Guard:
					helmetModelString = "addons/citizen/models/citizen_clothes/hat/hat_securityhelmet.vmdl";
					break;
				case OpWalrusGameInfo.Role.Prisoner:
					helmetModelString = "addons/citizen/models/citizen_clothes/hat/hat_leathercapnobadge.vmdl";
					break;
			}

			if( helmetModelString != null)
			{
				Log.Info( "Putting on: " + helmetModelString );
				ModelEntity helmet = new ModelEntity();
				helmet.SetModel( helmetModelString );
				helmet.SetParent( this, true );
				helmet.EnableShadowInFirstPerson = true;
				helmet.EnableHideInFirstPerson = true;
				clothes.Add( helmet );
			}
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			SimulateActiveChild( cl, ActiveChild );

			TickPlayerUse();
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
			List<OpWalrusPlayer> spectators = ((OpWalrusGame)Game.Current).spectators;
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
