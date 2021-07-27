
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
		public float gameLength = 60f;
		public float postGameLength = 2f;
		public float prisonerOverGuardRatio = 4f / 1f;

		Dictionary<OpWalrusGameInfo.Team, int> teamCounts { get; set; }
		[Net] public OpWalrusGameInfo.GameState gamestate { set; get; }
		[Net] public float lastGameStateChangeTime { get; set; }
		public List<OpWalrusPlayer> spectators = new List<OpWalrusPlayer>();

		public OpWalrusGame()
		{
			if ( IsServer )
			{
				gamestate = OpWalrusGameInfo.GameState.Pregame;
				lastGameStateChangeTime = Time.Now;

				teamCounts = new Dictionary<OpWalrusGameInfo.Team, int>();
				OpWalrusGameInfo.Team[] teams = Enum.GetValues<OpWalrusGameInfo.Team>();
				for ( int i = 0; i < teams.Length; i++ )
				{
					teamCounts[teams[i]] = 0;
				}
				
				Log.Info( "My Gamemode Has Created Serverside!" );

				new OpWalrusHud();
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
		}

		public override void OnKilled( Entity pawn )
		{
			base.OnKilled( pawn );

			if (pawn is OpWalrusPlayer opwp)
			{
				opwp.Inventory.DeleteContents();
				opwp.setSpectator( true );
				spectators.Add( opwp );
			}
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			if ( IsServer )
			{
				switch ( gamestate )
				{
					case OpWalrusGameInfo.GameState.Pregame:
						simulatePregame();
						break;
					case OpWalrusGameInfo.GameState.Playing:
						simulatePlaying();
						break;
					case OpWalrusGameInfo.GameState.PostGame:
						simulatePostGame();
						break;
				}
			}
		}

		public void killPlayer(OpWalrusPlayer player)
		{
			player.setSpectator( true );
			spectators.Add( player );
		}

		public void simulatePregame()
		{
			if ( Time.Now > preGameLength + lastGameStateChangeTime )
			{
				if ( preparePregame() )
				{
					setGameState( OpWalrusGameInfo.GameState.Playing );
				}
			}
		}

		//Tries to set the game up to be playable
		// ie. needs at least one guard.
		// needs at least one prisoner, etc.
		// returns false if it could not be prepared.
		public bool preparePregame()
		{
			OpWalrusPlayer[] players = All.OfType<OpWalrusPlayer>().ToArray();
			for(int i = 0; i < players.Length; i++ )
			{
				Log.Info( "Respawn" );
				players[i].Respawn();
				moveToRandomSpawnpoint( players[i] );
			}

			for(int i = 0; i < spectators.Count; i++ )
			{
				spectators[i].setSpectator( false );
			}
			spectators.Clear();
			return true;
		}

		public void moveToRandomSpawnpoint(OpWalrusPlayer player)
		{
			List<Vector3> spawnPositions = getSpawnPositions( OpWalrusGameInfo.roleToTeam[player.role] );
			if ( spawnPositions.Count == 0 )
			{
				throw new Exception( "Error: no spawn positions found" );
			}

			int randIndex = Rand.Int( 0, spawnPositions.Count - 1 );
			//Log.Info( randIndex );c
			player.Position = spawnPositions[randIndex];
			player.Velocity = Vector3.Zero;
		}


		public List<Vector3> getSpawnPositions(OpWalrusGameInfo.Team team)
		{
			List<Vector3> positions = new List<Vector3>();
			Entity[] entities;
			if (team == OpWalrusGameInfo.Team.Guards)
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

		public void simulatePlaying()
		{
			if ( Time.Now > gameLength + lastGameStateChangeTime )
			{
				setGameState( OpWalrusGameInfo.GameState.PostGame );
			}
		}
		public void simulatePostGame()
		{
			if ( Time.Now > postGameLength + lastGameStateChangeTime )
			{
				preparePostGame();
				setGameState( OpWalrusGameInfo.GameState.Pregame );
			}
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

			return source == dest;

			if(source == dest)
			{
				return false;
			}

			bool returnVal = !spectators.Contains( (OpWalrusPlayer)source.Pawn );

			for (int i = 0; i < spectators.Count; i++)
			{
				//Log.Info( spectators[i].GetClientOwner() );
			}
			//Log.Info( returnVal );
			return returnVal;
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
			List<OpWalrusPlayer> spectators = ((OpWalrusGame)Game.Current).spectators;
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

		public bool trySwitchTeam(OpWalrusPlayer player, OpWalrusGameInfo.Team wishTeam)
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

		public void preparePostGame()
		{
			for(int i = 0; i < All.Count; i++ )
			{
				Log.Info( (All[i], All[i].EntityName, All[i].GetType()) );
				if( All[i].GetType().IsSubclassOf(typeof(Weapon)) )
				{
					All[i].Delete();
				}
				else if(All[i] is EntDoor door )
				{

					//door.Close( );
					//FuncButton
					//new Output( door, "Close" ).Fire( this, 0.0f );
					//door.FireOutput( "Toggle", this, null, 0f );
				}
			}
		}
	}
}
