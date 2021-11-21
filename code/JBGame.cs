
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static OpWalrus.JBGameInfo;

namespace OpWalrus
{
	public partial class JBGame : Sandbox.Game
	{
		public float preGameLength = 2f;
		public float gameLength = 2f;
		public float postGameLength = 2f;
		public float playersPerGuard = 4f;
		//For instance, for every 4 players, there is 1 guard (the guard is included)
		//the amount of guards is +1 so at population (1,3) there is 1 guard, and at (4,7) there is 2. etc.
		ModelEntity curPing;
		float pingCreationTime;
		float pingLifespan = 5f;

		[Net] public JBGameInfo.GameState gamestate { set; get; }
		[Net] public float lastGameStateChangeTime { get; set; }
		[Net] public List<JBPlayer> spectators { set; get; }
		[Net] public JBPlayer curWarden { set; get; }
		[Net] public List<JBPlayer> speakingList { set; get; }
		Dictionary<JBPlayer, float> lastTalkTime { set; get; }
		[Net] public JBGameInfo.Team winningTeam { get; set; }
		float talkTimeDecay = 1f;

		public JBGame()
		{
			if ( IsServer )
			{
				spectators = new List<JBPlayer>();
				gamestate = JBGameInfo.GameState.EnoughPlayersCheck;
				lastGameStateChangeTime = Time.Now;
				speakingList = new List<JBPlayer>();
				lastTalkTime = new Dictionary<JBPlayer, float>();

				new JBHud();
				new JBCrosshairHud();
			}

			if ( IsClient )
			{

			}
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			JBPlayer player = new JBPlayer();
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
			if (pawn is JBPlayer opwp )
			{
				opwp.Inventory.DeleteContents();
				opwp.setSpectator( true );
				spectators.Add( opwp );
			}
			checkWinCondition();
		}

		public bool checkWinCondition()
		{
			bool didChangeGameState = false;

			bool foundAnyGuards = false;
			bool foundAnyPrisoners = true;

			bool foundALivingGuard = false;
			bool foundALivingPrisoner = false;

			foreach(JBPlayer player in All.OfType<JBPlayer>())
			{
				if ( player.role == JBGameInfo.Role.Prisoner )
				{
					foundAnyPrisoners = true;
					if ( !spectators.Contains( player ) )
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

			if ( foundAnyGuards && !foundALivingGuard )
			{
				goToNextGameState();
				winningTeam = Team.Prisoners;
				didChangeGameState = true;
			}
			else if ( foundAnyPrisoners && !foundALivingPrisoner )
			{
				goToNextGameState();
				winningTeam = Team.Guards;
				didChangeGameState = true;
			}
			return didChangeGameState;
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );
			if ( IsServer )
			{
				if (Time.Now > pingCreationTime + pingLifespan)
				{
					if(curPing != null && curPing.IsValid())
					{
						curPing.Delete();
					}
				}


				speakingList.Clear();
				foreach ( JBPlayer player in All.OfType<JBPlayer>() )
				{
					if ( lastTalkTime.ContainsKey( player ) && Time.Now < lastTalkTime[player] + talkTimeDecay )
					{
						speakingList.Add( player );
					}
				}
				
				switch(gamestate)
				{
					case GameState.Playing:
						//Check if mid-game some players left making the game state invalid
						if ( !isEnoughPlayers() )
						{
							goToNextGameState();
							return;
						}

						//Check if mid-game players died or left causing a game-over
						if(checkWinCondition())
						{
							return;
						}
						break;
				}

				if (Time.Now > lastGameStateChangeTime + JBGameInfo.gameStateLengths[gamestate])
				{
					goToNextGameState();
				}
				else
				{
					simulateGameState();
				}
			}
		}

		[ServerCmd]
		public static void createPing(Vector3 position, Vector3 normal)
		{
			JBGame game = (JBGame)Game.Current;
			JBPlayer player = (JBPlayer)ConsoleSystem.Caller.Pawn;

			if (player.role == Role.Warden)
			{
				if ( game.curPing != null && game.curPing.IsValid())
				{
					game.curPing.Delete();
				}

				game.curPing = Library.Create<ModelEntity>();
				game.curPing.Position = position;
				game.curPing.SetModel( "models/ping/pingsystem" );
				game.curPing.Spawn();
				game.curPing.Scale = 0.25f;
				game.curPing.Position = position;

				float rad2Deg = 360 / (MathF.PI * 2);
				float horizDistance = MathF.Sqrt( (normal.x * normal.x) + (normal.y * normal.y) );
				float pitch = rad2Deg * MathF.Atan2( horizDistance, normal.z );
				float yaw   = rad2Deg * MathF.Atan2( normal.y,      normal.x );
				float roll  = 0;
				Angles angles = new Angles( pitch, yaw, roll );
				game.curPing.Rotation = Rotation.From( angles );

				game.pingCreationTime = Time.Now;
			}
		}

		public void goToNextGameState()
		{
			JBGameInfo.GameState[] states = Enum.GetValues<JBGameInfo.GameState>();
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

		public bool beginGameState(JBGameInfo.GameState newGameState)
		{
			switch ( gamestate )
			{
				case JBGameInfo.GameState.Playing:
					break;
				case JBGameInfo.GameState.PostGame:
					beginPostGame();
					break;
			}
			return true;
		}

		//Returns true if the gamestate is ready to be ended
		public bool endGameState( JBGameInfo.GameState oldGameState)
		{
			switch ( gamestate )
			{
				case JBGameInfo.GameState.EnoughPlayersCheck:
					return isEnoughPlayers();
					break;
				case JBGameInfo.GameState.Pregame:
					return endPregame();
					break;
				case JBGameInfo.GameState.Playing:
					break;
				case JBGameInfo.GameState.PostGame:
					break;
			}
			return true;
		}

		public bool isEnoughPlayers()
		{
			return getAllPlayersOfTeam( JBGameInfo.Team.Prisoners ).Count > 0 &&
					getAllPlayersOfTeam( JBGameInfo.Team.Guards ).Count > 0;
		}

		public bool endPregame()
		{
			JBPlayer[] players = All.OfType<JBPlayer>().ToArray();

			//Set all spectators back to normal players
			for ( int i = 0; i < spectators.Count; i++ )
			{
				//spectators[i].setSpectator( false );
			}
			spectators.Clear();

			foreach( JBPlayer player in All.OfType<JBPlayer>().ToArray<JBPlayer>() )
			{
				player.Respawn();
				moveToRandomSpawnpoint( player );
			}

			//Set a random guard to become warden
			List<JBPlayer> guards = getAllPlayersOfTeam( JBGameInfo.Team.Guards );
			List<JBPlayer> optedInGuards = new List<JBPlayer>();
			if(guards.Count > 0)
			{
				foreach(JBPlayer guard in guards)
				{
					if(guard.optinWarden)
					{
						optedInGuards.Add( guard );
					}
				}

				if(optedInGuards.Count > 0)
				{
					curWarden = Rand.FromList<JBPlayer>( optedInGuards );
					curWarden.role = JBGameInfo.Role.Warden;
				}
				else
				{
					curWarden = Rand.FromList<JBPlayer>( guards );
					curWarden.role = JBGameInfo.Role.Warden;
				}
			}


			return true;
		}

		public bool beginPostGame()
		{

			tryAutobalance();

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
				else if ( e is JBPlayer player )
				{
					player.setSpectator( true );
					spectators.Add( player );
					if ( player.role == JBGameInfo.Role.Warden )
					{
						player.role = JBGameInfo.Role.Guard;
						curWarden = null;
					}
				}
			}

			return true;
		}

		public List<JBPlayer> getAllPlayersOfRole( JBGameInfo.Role role )
		{
			IEnumerable<JBPlayer> players = All.OfType<JBPlayer>();
			List<JBPlayer> playersOfRole = new List<JBPlayer>();
			foreach( JBPlayer player in players )
			{
				if(player.role == role)
				{
					playersOfRole.Add( player );
				}
			}

			return playersOfRole;
		}

		public List<JBPlayer> getAllPlayersOfTeam( JBGameInfo.Team team )
		{
			IEnumerable<JBPlayer> players = All.OfType<JBPlayer>();
			List<JBPlayer> playersOfTeam = new List<JBPlayer>();
			foreach ( JBPlayer player in players )
			{
				if ( JBGameInfo.roleToTeam[player.role] == team )
				{
					playersOfTeam.Add( player );
				}
			}
			return playersOfTeam;
		}

		//Returns true if game was autobalanced
		public bool tryAutobalance()
		{
			int numOfPlayers = All.OfType<JBPlayer>().Count();

			int maxNumOfGuards = (int)(numOfPlayers / playersPerGuard) + 1;

			int curNumOfGuards = getAllPlayersOfTeam( JBGameInfo.Team.Guards ).Count;

			bool autobalanced = false;
			if(curNumOfGuards > maxNumOfGuards)
			{
				int guardsToKick = curNumOfGuards - maxNumOfGuards;
				for(int i = 0; i < guardsToKick; i++ )
				{
					JBPlayer randomGuard = Rand.FromList<JBPlayer>( getAllPlayersOfTeam( JBGameInfo.Team.Guards ) );
					randomGuard.role = Role.Prisoner;
				}
				autobalanced = true;
			}
			return autobalanced;
		}

		public override bool CanHearPlayerVoice( Client source, Client dest )
		{
			Host.AssertServer();
			if(spectators.Contains((JBPlayer)source.Pawn))
			{
				return false;
			}
			lastTalkTime[(JBPlayer)source.Pawn] = Time.Now;
			return true;
		}

		[ServerCmd( "trySwitchTeam" )]
		public static void trySwitchTeamCmd( string team )
		{
			JBGameInfo.Team[] teams = Enum.GetValues<JBGameInfo.Team>();
			bool found = false;
			for ( int i = 0; i < teams.Length && !found; i++ )
			{
				if ( team.Equals( teams[i].ToString() ) )
				{
					found = true;
					((JBGame)Game.Current).trySwitchTeam( (JBPlayer)(ConsoleSystem.Caller.Pawn), teams[i] );
				}
			}
		}

		public bool trySwitchTeam( JBPlayer player, JBGameInfo.Team wishTeam)
		{
			JBGameInfo.Team oldTeam = JBGameInfo.roleToTeam[player.role];

			//Cannot join your own team
			if ( JBGameInfo.roleToTeam[player.role].Equals(wishTeam))
			{
				return false;
			}

			bool succeeded = false;
			switch(wishTeam)
			{
				case JBGameInfo.Team.Prisoners:
					player.role = JBGameInfo.Role.Prisoner;
					succeeded = true;
					break;
				case JBGameInfo.Team.Guards:
					int numOfPlayers = All.OfType<JBPlayer>().Count();
					int maxNumOfGuards = (int)(numOfPlayers / playersPerGuard) + 1;
					int curNumOfGuards = getAllPlayersOfTeam( JBGameInfo.Team.Guards ).Count;
					Log.Info( (maxNumOfGuards, curNumOfGuards) );
					if(curNumOfGuards + 1 <= maxNumOfGuards)
					{	
						player.role = JBGameInfo.Role.Guard;
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

		public void moveToRandomSpawnpoint( JBPlayer player )
		{
			List<Vector3> spawnPositions = getSpawnPositions( JBGameInfo.roleToTeam[player.role] );
			if ( spawnPositions.Count == 0 )
			{
				throw new Exception( "Error: no spawn positions found" );
			}

			player.Position = Rand.FromList<Vector3>( spawnPositions );
			player.Velocity = Vector3.Zero;
		}

		public List<Vector3> getSpawnPositions( JBGameInfo.Team team )
		{
			List<Vector3> positions = new List<Vector3>();
			Entity[] entities;
			if ( team == JBGameInfo.Team.Guards )
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
