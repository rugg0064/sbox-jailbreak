using Sandbox;
using Sandbox.UI;
using System.ComponentModel;

namespace OpWalrus
{
	[Library]
	public partial class JBWinMenu : Panel
	{
		Label winlabel;
		public JBWinMenu()
		{
			StyleSheet.Load( "JBNewHud.scss" );
			AddClass( "winOverlay" );

			winlabel = new Label();
			winlabel.AddClass( "winLabel" );
			winlabel.Parent = this;

		}

		public override void Tick()
		{
			JBGame curGame = ((JBGame)Game.Current);
			if(curGame.gamestate == JBGameInfo.GameState.PostGame)
			{
				winlabel.SetText( curGame.winningTeam + " win!" );
				winlabel.SetClass( "visible", true );
				switch ( curGame.winningTeam )
				{
					case JBGameInfo.Team.Prisoners:
						winlabel.SetClass( "prisoner", true );
						break;
					case JBGameInfo.Team.Guards:
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
