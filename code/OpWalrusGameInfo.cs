using System.Collections.Generic;

namespace OpWalrus
{
	public static class OpWalrusGameInfo
	{
		public enum Team
		{
			Prisoners,
			Guards
		}

		public enum Role
		{
			Warden,
			Guard,
			Prisoner
		}

		public static Dictionary<Role, Team> roleToTeam = new Dictionary<Role, Team>
		{
			{Role.Warden, Team.Guards },
			{Role.Guard, Team.Guards },
			{Role.Prisoner, Team.Prisoners }
		};

		public static Dictionary<Team, Role> teamToDefaultRole = new Dictionary<Team, Role>
		{
			{Team.Guards, Role.Guard },
			{Team.Prisoners, Role.Prisoner }
		};

		public enum GameState
		{
			EnoughPlayersCheck,
			Pregame,
			Playing,
			PostGame
		}

		public static Dictionary<GameState, float> gameStateLengths = new Dictionary<GameState, float>
		{
			{GameState.EnoughPlayersCheck, 0.15f },
			{GameState.Pregame, 0.0f },
			{GameState.Playing, 360.0f },
			{GameState.PostGame, 7.5f }
		};
	}
}
