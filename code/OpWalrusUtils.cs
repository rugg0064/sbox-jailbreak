using Sandbox;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpWalrus
{
	public static class OpWalrusUtils
	{
		public static OpWalrusPlayer getPlayerByName( IReadOnlyList<Entity> all, string playerName )
		{
			OpWalrusPlayer[] players = all.OfType<OpWalrusPlayer>().ToArray();
			for ( int i = 0; i < players.Length; i++ )
			{
				OpWalrusPlayer player = players[i];
				if ( player.GetClientOwner().Name.Equals( playerName ) )
				{
					return player;
				}
			}
			return null;
		}
	}
}
