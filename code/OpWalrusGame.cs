
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static OpWalrus.OpWalrusGameInfo;

namespace OpWalrus
{
	public partial class OpWalrusGame : Sandbox.Game
	{
		public float preGameLength = 2f;
		public float gameLength = 2f;
		public float postGameLength = 2f;
		public float playersPerGuard = 4f;
		//For instance, for every 4 players, there is 1 guard (the guard is included)
		//the amount of guards is +1 so at population (1,3) there is 1 guard, and at (4,7) there is 2. etc.

		[Net] public OpWalrusGameInfo.GameState gamestate { set; get; }
		[Net] public float lastGameStateChangeTime { get; set; }
		[Net] public List<OpWalrusPlayer> spectators { set; get; }
		[Net] public OpWalrusPlayer curWarden { set; get; }

		public OpWalrusGame()
		{
			if ( IsServer )
			{
				spectators = new List<OpWalrusPlayer>();
				gamestate = OpWalrusGameInfo.GameState.EnoughPlayersCheck;
				lastGameStateChangeTime = Time.Now;

				new OpWalrusHud();
				new OpWalrusCrosshairHud();
			}

			if ( IsClient )
			{

			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			OpWalrusPlayer player = new OpWalrusPlayer();
			client.Pawn = player;
			player.Respawn();
			player.setSpectator( true );

			spectators.Add( player );
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{
			base.ClientDisconnect( cl, reason );
		}

		public override void OnKilled( Entity pawn )
		{
			base.OnKilled( pawn );
			if (pawn is OpWalrusPlayer opwp )
			{
				opwp.Inventory.DeleteContents();
				opwp.setSpectator( true );
				spectators.Add( opwp );

				bool foundAnyGuards = false;
				bool foundAnyPrisoners = true;

				bool foundALivingGuard = false;
				bool foundALivingPrisoner = false;

				OpWalrusPlayer[] players = All.OfType<OpWalrusPlayer>().ToArray();
				for (int i = 0; i < players.Length; i++)
				{
					OpWalrusPlayer player = players[i];
					if(player.role == OpWalrusGameInfo.Role.Prisoner)
					{
						foundAnyPrisoners = true;
						if(!spectators.Contains( player ))
						{
							foundALivingPrisoner = true;
						}
					}
					else
					{
						foundAnyGuards = true;
						if ( !spectators.Contains( player ) )
						{
							foundALivingGuard = true;
						}
					}
				}

				if(foundAnyGuards && !foundALivingGuard)
				{
					goToNextGameState();
				}
				else if ( foundAnyPrisoners && !foundALivingPrisoner )
				{
					goToNextGameState();
				}

			}
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			if ( IsServer )
			{
				if(Time.Now > lastGameStateChangeTime + OpWalrusGameInfo.gameStateLengths[gamestate])
				{
					goToNextGameState();
				}
				else
				{
					simulateGameState();
				}
			}
		}

		public void goToNextGameState()
		{
			OpWalrusGameInfo.GameState[] states = Enum.GetValues<OpWalrusGameInfo.GameState>();
			int curGameStateIndex = Array.IndexOf( states, gamestate );
			//Log.Info( curGameStateIndex );
			int nextGameStateIndex;
			if ( curGameStateIndex == states.Length - 1 )
			{
				nextGameStateIndex = 0;
			}
			else
			{
				nextGameStateIndex = curGameStateIndex + 1;
			}
			//gamestate = states[nextGameStateIndex];

			if ( endGameState(gamestate) )
            {
				gamestate = states[nextGameStateIndex];
				lastGameStateChangeTime = Time.Now;

				beginGameState( gamestate );
			}
			else
			{
				lastGameStateChangeTime = Time.Now;
			}
		}

		public void simulateGameState()
		{

		}

		public bool beginGameState(OpWalrusGameInfo.GameState newGameState)
		{
			switch ( gamestate )
			{
				case OpWalrusGameInfo.GameState.Playing:
					break;
				case OpWalrusGameInfo.GameState.PostGame:
					beginPostGame();
					break;
			}
			return true;
		}

		//Returns true if the gamestate is ready to be ended
		public bool endGameState( OpWalrusGameInfo.GameState oldGameState)
		{
			switch ( gamestate )
			{
				case OpWalrusGameInfo.GameState.EnoughPlayersCheck:
					return isEnoughPlayers();
					break;
				case OpWalrusGameInfo.GameState.Pregame:
					return endPregame();
					break;
				case OpWalrusGameInfo.GameState.Playing:
					break;
				case OpWalrusGameInfo.GameState.PostGame:
					break;
			}
			return true;
		}

		public bool isEnoughPlayers()
		{
			return getAllPlayersOfTeam( OpWalrusGameInfo.Team.Prisoners ).Count > 0 &&
					getAllPlayersOfTeam( OpWalrusGameInfo.Team.Guards ).Count > 0;
		}

		public bool endPregame()
		{
			OpWalrusPlayer[] players = All.OfType<OpWalrusPlayer>().ToArray();

			//Set all spectators back to normal players
			for ( int i = 0; i < spectators.Count; i++ )
			{
				spectators[i].setSpectator( false );
			}
			spectators.Clear();

			foreach( OpWalrusPlayer player in All.OfType<OpWalrusPlayer>().ToArray<OpWalrusPlayer>() )
			{
				player.Respawn();
				moveToRandomSpawnpoint( player );
			}

			//Set a random guard to become warden
			List<OpWalrusPlayer> guards = getAllPlayersOfTeam( OpWalrusGameInfo.Team.Guards );
			if(guards.Count > 0)
			{
				curWarden = Rand.FromList<OpWalrusPlayer>( guards );
				curWarden.role = OpWalrusGameInfo.Role.Warden;
			}

			return true;
		}

		public bool beginPostGame()
		{

			foreach( Entity e in All)
			{
				//Remove entities
				if ( e.GetType().IsSubclassOf( typeof( Weapon ) ) )
				{
					e.Delete();
				}

				//Begin doors closing
				else if ( e is DoorEntity door )
				{
					door.Close();
				}

				//Kill everyone & remove warden
				else if ( e is OpWalrusPlayer player )
				{
					player.setSpectator( true );
					if ( player.role == OpWalrusGameInfo.Role.Warden )
					{
						player.role = OpWalrusGameInfo.Role.Guard;
						curWarden = null;
					}
				}
			}

			return true;
		}

		public void setGameState( OpWalrusGameInfo.GameState newGameState)
		{
			Log.Info( "Setting game state to: " + newGameState );
			gamestate = newGameState;
			lastGameStateChangeTime = Time.Now;
		}

		public List<OpWalrusPlayer> getAllPlayersOfRole( OpWalrusGameInfo.Role role )
		{
			IEnumerable<OpWalrusPlayer> players = All.OfType<OpWalrusPlayer>();
			List<OpWalrusPlayer> playersOfRole = new List<OpWalrusPlayer>();
			foreach( OpWalrusPlayer player in players )
			{
				if(player.role == role)
				{
					playersOfRole.Add( player );
				}
			}

			return playersOfRole;
		}

		public List<OpWalrusPlayer> getAllPlayersOfTeam( OpWalrusGameInfo.Team team )
		{
			IEnumerable<OpWalrusPlayer> players = All.OfType<OpWalrusPlayer>();
			List<OpWalrusPlayer> playersOfTeam = new List<OpWalrusPlayer>();
			foreach ( OpWalrusPlayer player in players )
			{
				if ( OpWalrusGameInfo.roleToTeam[player.role] == team )
				{
					playersOfTeam.Add( player );
				}
			}
			return playersOfTeam;
		}

		public override bool CanHearPlayerVoice( Client source, Client dest )
		{
			Host.AssertServer();
			return true;
		}

		[ServerCmd( "getTeam" )]
		public static void getTeam( string playerName )
		{
			Log.Info( "Team of: " + playerName );

			OpWalrusPlayer player = OpWalrusUtils.getPlayerByName( All, playerName );

			if ( player != null )
			{
				Log.Info( OpWalrusGameInfo.roleToTeam[player.role] );
			}
			else
			{
				Log.Info( "Couldn't find player, ensure its typed exactly as the player's name." );
			}
		}

		[ServerCmd( "setRole" )]
		public static void setTeam( string playerName, string role )
		{
			OpWalrusPlayer player = OpWalrusUtils.getPlayerByName( All, playerName );
			OpWalrusGameInfo.Role roleToSet = OpWalrusGameInfo.Role.Prisoner;

			OpWalrusGameInfo.Role[] roles = Enum.GetValues<OpWalrusGameInfo.Role>();
			bool found = false;
			for ( int i = 0; i < roles.Length && !found; i++ )
			{
				if ( role.Equals( roles[i].ToString() ) )
				{
					roleToSet = roles[i];
					found = true;
				}
			}

			if ( player != null && found )
			{
				player.role = roleToSet;
				Log.Info( "Set role." );
			}
			else
			{
				Log.Info( "Couldn't set role." );
			}
		}

		[ServerCmd( "getRole" )]
		public static void getRole( string playerName )
		{
			Log.Info( "Role of: " + playerName );
			OpWalrusPlayer player = OpWalrusUtils.getPlayerByName( All, playerName );

			if ( player != null )
			{
				Log.Info( player.role );
			}
			else
			{
				Log.Info( "Couldn't find player, ensure its typed exactly as the player's name." );
			}
		}

		[ServerCmd( "getSpectators" )]
		public static void getSpectators()
		{
			IList<OpWalrusPlayer> spectators = ((OpWalrusGame)Game.Current).spectators;
			for ( int i = 0; i < spectators.Count; i++ )
			{
				Log.Info( spectators[i].GetClientOwner().Name );
			}
		}

		[ServerCmd( "trySwitchTeam" )]
		public static void trySwitchTeamCmd(string team)
		{
			OpWalrusGameInfo.Team[] teams = Enum.GetValues<OpWalrusGameInfo.Team>();
			bool found = false;
			for (int i = 0; i < teams.Length && !found; i++ )
			{
				if( team.Equals(teams[i].ToString()) )
				{
					found = true;
					((OpWalrusGame)Game.Current).trySwitchTeam( (OpWalrusPlayer) (ConsoleSystem.Caller.Pawn) , teams[i] );
				}
			}
		}

		public bool trySwitchTeam( OpWalrusPlayer player, OpWalrusGameInfo.Team wishTeam)
		{
			OpWalrusGameInfo.Team oldTeam = OpWalrusGameInfo.roleToTeam[player.role];

			//Cannot join your own team
			if ( OpWalrusGameInfo.roleToTeam[player.role].Equals(wishTeam))
			{
				return false;
			}

			bool succeeded = false;
			switch(wishTeam)
			{
				case OpWalrusGameInfo.Team.Prisoners:
					player.role = OpWalrusGameInfo.Role.Prisoner;
					succeeded = true;
					break;
				case OpWalrusGameInfo.Team.Guards:
					int numOfPlayers = All.OfType<OpWalrusPlayer>().Count();
					int maxNumOfGuards = (int)(numOfPlayers / playersPerGuard) + 1;
					int curNumOfGuards = getAllPlayersOfTeam( OpWalrusGameInfo.Team.Guards ).Count;
					
					if(curNumOfGuards + 1 >= maxNumOfGuards)
					{	
						player.role = OpWalrusGameInfo.Role.Guard;
						succeeded = true;
					}

					break;
			}

			if(succeeded)
			{
				player.OnKilled();
			}

			return succeeded;
		}

		public void moveToRandomSpawnpoint( OpWalrusPlayer player )
		{
			List<Vector3> spawnPositions = getSpawnPositions( OpWalrusGameInfo.roleToTeam[player.role] );
			if ( spawnPositions.Count == 0 )
			{
				throw new Exception( "Error: no spawn positions found" );
			}

			player.Position = Rand.FromList<Vector3>( spawnPositions );
			player.Velocity = Vector3.Zero;
		}

		public List<Vector3> getSpawnPositions( OpWalrusGameInfo.Team team )
		{
			List<Vector3> positions = new List<Vector3>();
			Entity[] entities;
			if ( team == OpWalrusGameInfo.Team.Guards )
			{
				entities = All.OfType<opwgs>().ToArray();
			}
			else
			{
				entities = All.OfType<opwps>().ToArray();
			}

			for ( int i = 0; i < entities.Length; i++ )
			{
				positions.Add( entities[i].Position );
			}

			return positions;
		}
	}
}
