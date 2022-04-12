using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpWalrus
{
	public static class JBUtils
	{
		public static JBPlayer getPlayerByName( IReadOnlyList<Entity> all, string playerName )
		{
			JBPlayer[] players = all.OfType<JBPlayer>().ToArray();
			for ( int i = 0; i < players.Length; i++ )
			{
				JBPlayer player = players[i];
				if ( player.Client.Name.Equals( playerName ) )
				{
					return player;
				}
			}
			return null;
		}
	}
}
