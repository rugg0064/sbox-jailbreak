
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpWalrus
{
	public partial class OpWalrusGame : Sandbox.Game
	{
		public float preGameLength = 2f;
		public float gameLength = 2f;
		public float postGameLength = 2f;
		public float prisonerOverGuardRatio = 4f / 1f;

		Dictionary<OpWalrusGameInfo.Team, int> teamCounts { get; set; }
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

				teamCounts = new Dictionary<OpWalrusGameInfo.Team, int>();
				OpWalrusGameInfo.Team[] teams = Enum.GetValues<OpWalrusGameInfo.Team>();
				for ( int i = 0; i < teams.Length; i++ )
				{
					teamCounts[teams[i]] = 0;
				}
				
				Log.Info( "My Gamemode Has Created Serverside!" );

				new OpWalrusHud();
				new OpWalrusCrosshairHud();
			}

			if ( IsClient )
			{
				Log.Info( "My Gamemode Has Created Clientside!" );
			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new OpWalrusPlayer();
			client.Pawn = player;
			player.Respawn();

			teamCounts[OpWalrusGameInfo.roleToTeam[player.role]]++;
			player.setSpectator( true );
			spectators.Add( player );
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{
			OpWalrusPlayer player = ((OpWalrusPlayer)cl.Pawn);
			teamCounts[OpWalrusGameInfo.roleToTeam[player.role]]--;

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
							break;
						}
					}
					else
					{
						foundAnyGuards = true;
						if ( !spectators.Contains( player ) )
						{
							foundALivingGuard = true;
							break;
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
					Log.Info( "Changing game state" );

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
			return teamCounts[OpWalrusGameInfo.Team.Prisoners] > 0 && teamCounts[OpWalrusGameInfo.Team.Guards] > 0;
		}

		public bool endPregame()
		{
			OpWalrusPlayer[] players = All.OfType<OpWalrusPlayer>().ToArray();
			List<OpWalrusPlayer> guards = new List<OpWalrusPlayer>();

			//Set all spectators back to normal players
			for ( int i = 0; i < spectators.Count; i++ )
			{
				spectators[i].setSpectator( false );
			}
			spectators.Clear();

			//Respawn everybody
			for ( int i = 0; i < players.Length; i++ )
			{
				Log.Info( "Respawn" );
				players[i].Respawn();
				moveToRandomSpawnpoint( players[i] );
				if ( players[i].role == OpWalrusGameInfo.Role.Guard )
				{
					guards.Add( players[i] );
				}
			}

			//Select a warden
			if ( guards.Count > 0 )
			{
				curWarden = guards[Rand.Int( 0, guards.Count - 1 )];
				curWarden.role = OpWalrusGameInfo.Role.Warden;
			}

			return true;
		}

		public bool beginPostGame()
		{
			for ( int i = 0; i < All.Count; i++ )
			{
				//Remove entities
				if ( All[i].GetType().IsSubclassOf( typeof( Weapon ) ) )
				{
					All[i].Delete();
				}
				
				//Begin doors closing
				else if ( All[i] is DoorEntity door )
				{
					door.Close();
				}
			
				//Kill everyone & remove warden
				else if( All[i] is OpWalrusPlayer player )
				{
					player.setSpectator( true );
					if(player.role == OpWalrusGameInfo.Role.Warden)
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
					Log.Info( ConsoleSystem.Caller.Name + " is calling for " + teams[i] );
					bool result = ((OpWalrusGame)Game.Current).trySwitchTeam( (OpWalrusPlayer)ConsoleSystem.Caller.Pawn, teams[i] );
					Log.Info( "Result: " + result );
				}
			}
		}

		public bool trySwitchTeam( OpWalrusPlayer player, OpWalrusGameInfo.Team wishTeam)
		{
			OpWalrusGameInfo.Team oldTeam = OpWalrusGameInfo.roleToTeam[player.role];

			if(OpWalrusGameInfo.roleToTeam[player.role].Equals(wishTeam))
			{
				return false;
			}

			bool succeeded = false;
			switch(wishTeam)
			{
				case OpWalrusGameInfo.Team.Prisoners:
					Log.Info( "prisoner" );
					player.role = OpWalrusGameInfo.Role.Prisoner;
					succeeded = true;
					break;
				case OpWalrusGameInfo.Team.Guards:
					Log.Info( "guards" );

					int curPrisonerCount = teamCounts[OpWalrusGameInfo.Team.Prisoners];
					int curGuardCount = teamCounts[OpWalrusGameInfo.Team.Guards];

					int prisonerCountIfSucceed = curPrisonerCount - 1;
					int guardCountIfSucceed = curGuardCount + 1;

					bool bypass = false;
					if(curGuardCount == 0)
					{
						bypass = true;
					}

					float ratio = ((float)prisonerCountIfSucceed) / guardCountIfSucceed;
					Log.Info( (prisonerCountIfSucceed, guardCountIfSucceed, ratio) );
					if ( ratio >= prisonerOverGuardRatio || bypass)
					{
						bool anyGuards = false;
						OpWalrusPlayer[] players = All.OfType<OpWalrusPlayer>().ToArray();
						for(int i = 0; i < players.Length; i++)
						{
							if(OpWalrusGameInfo.roleToTeam[players[i].role] == OpWalrusGameInfo.Team.Guards)
							{
								anyGuards = true;
								break;
							}
						}
						if(!anyGuards)
						{
							goToNextGameState();
						}

						player.role = OpWalrusGameInfo.Role.Guard;
						Log.Info( player.role );
						succeeded = true;
					}
					break;
			}

			if(succeeded)
			{
				player.OnKilled();
				teamCounts[oldTeam]--;
				teamCounts[wishTeam]++;
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

			int randIndex = Rand.Int( 0, spawnPositions.Count - 1 );

			player.Position = spawnPositions[randIndex];
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
