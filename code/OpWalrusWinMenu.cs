using Sandbox;
using Sandbox.UI;
using System.ComponentModel;

namespace OpWalrus
{
	[Library]
	public partial class OpWalrusWinMenu : Panel
	{
		Label winlabel;
		public OpWalrusWinMenu()
		{
			StyleSheet.Load( "OpWalrusNewHud.scss" );
			AddClass( "winOverlay" );

			winlabel = new Label();
			winlabel.AddClass( "winLabel" );
			winlabel.Parent = this;

		}

		public override void Tick()
		{
			OpWalrusGame curGame = ((OpWalrusGame)Game.Current);
			if(curGame.gamestate == OpWalrusGameInfo.GameState.PostGame)
			{
				winlabel.SetText( curGame.winningTeam + " win!" );
				winlabel.SetClass( "visible", true );
				switch ( curGame.winningTeam )
				{
					case OpWalrusGameInfo.Team.Prisoners:
						winlabel.SetClass( "prisoner", true );
						break;
					case OpWalrusGameInfo.Team.Guards:
						winlabel.SetClass( "guard", true );
						break;
					default:
						break;
				}
			}
			else
			{
				winlabel.SetText( "" );
				winlabel.SetClass( "guard", false );
				winlabel.SetClass( "prisoner", false );
				winlabel.SetClass( "visible", false );
			}

			base.Tick();

		}
	}
}
