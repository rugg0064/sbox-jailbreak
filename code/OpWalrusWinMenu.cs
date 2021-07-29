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
			StyleSheet.Load( "OpWalrusHud.scss" );
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
			}
			else
			{
				winlabel.SetText( "" );
			}

			base.Tick();
		}
	}
}
