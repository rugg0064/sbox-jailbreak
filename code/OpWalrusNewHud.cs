using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	[Library]
	public partial class OpWalrusNewHud : Panel
	{
		Label currentModeIndicator;
		Label timeLeftIndicator;
		Label hpIndicator;
		Label topIndicator;
		Panel currentWardenIndicator;
		OpWalrusPlayerLabel wardenName;
		Label noWardenIndicator;
		Label peopleTalkingIndicator;
		OpWalrusPlayerLabel lookingAtIndicator;

		public OpWalrusNewHud()
		{ 
			StyleSheet.Load( "OpWalrusNewHud.scss" );
			AddClass( "overlay" );

			Panel topContainer = new Panel();
			topContainer.AddClass( "topContainer" );
			topContainer.Parent = this;

			topIndicator = new Label();
			topIndicator.AddClass( "topIndicator" );
			topIndicator.Parent = topContainer;

			timeLeftIndicator = new Label();
			timeLeftIndicator.AddClass( "timeLeftIndicator" );
			timeLeftIndicator.Parent = topContainer;

			hpIndicator = new Label();
			hpIndicator.AddClass( "hpIndicator" );
			hpIndicator.Parent = this;

			currentWardenIndicator = new Panel();
			currentWardenIndicator.AddClass( "currentWardenIndicator" );
			currentWardenIndicator.Parent = this;

			wardenName = new OpWalrusPlayerLabel();
			wardenName.AddClass( "wardenName" );
			wardenName.Parent = currentWardenIndicator;

			noWardenIndicator = new Label();
			noWardenIndicator.AddClass( "noWardenIndicator" );
			noWardenIndicator.Parent = currentWardenIndicator;

			peopleTalkingIndicator = new Label();
			peopleTalkingIndicator.AddClass( "peopleTalkingIndicator" );
			peopleTalkingIndicator.Parent = this;

			lookingAtIndicator = new OpWalrusPlayerLabel();
			lookingAtIndicator.AddClass( "lookingAtIndicator" );
			lookingAtIndicator.Parent = this;
		}

		public override void Tick()
		{
			base.Tick();

			OpWalrusGame game = (OpWalrusGame)Game.Current;

			float lct = game.lastGameStateChangeTime;
			float tn = Time.Now;
			float d = OpWalrusGameInfo.gameStateLengths[game.gamestate];

			//timeLeftIndicator.SetText( "Time left: " + (d + lct - tn) );
			timeLeftIndicator.SetText( "2:30" );
			float hp = ((OpWalrusPlayer)Local.Pawn).Health;
			hpIndicator.SetText( "+ " + hp);

			OpWalrusGameInfo.Role role = ((OpWalrusPlayer)Local.Pawn).role;
			OpWalrusPlayer warden = game.curWarden;
			if(warden != null)
			{
				//currentWardenIndicator.SetText( "Current Warden: " + warden.GetClientOwner().Name + (game.spectators.Contains(warden) ? " (Dead)" : "") );
				wardenName.player = warden;
				wardenName.SetShown( true );
				noWardenIndicator.SetClass( "hidden", true );
			}
			else
			{
				//currentWardenIndicator.SetText( "Current Warden: " + "None");
				wardenName.SetShown( false );
				noWardenIndicator.SetClass( "hidden", true );
			}
			if (game.gamestate == OpWalrusGameInfo.GameState.Playing)
			{
				currentWardenIndicator.SetClass( "hidden", false );
				if (hp > 0) {
					switch ( OpWalrusGameInfo.roleToTeam[role] )
					{
						case OpWalrusGameInfo.Team.Prisoners:
							topIndicator.SetClass( "prisoner", true );
							break;
						case OpWalrusGameInfo.Team.Guards:
							topIndicator.SetClass( "guard", true );
							break;
						default:
							ResetTopIndicator();
							break;
					}
				} else
				{
					ResetTopIndicator();
				}
				
			} else
			{
				currentWardenIndicator.SetClass( "hidden", true );
				ResetTopIndicator();
			}

			switch ( game.gamestate )
			{
				case OpWalrusGameInfo.GameState.EnoughPlayersCheck:
					topIndicator.SetText( "Waiting For Players" );
					break;
				case OpWalrusGameInfo.GameState.Pregame:
					topIndicator.SetText( "Pregame" );
					break;
				case OpWalrusGameInfo.GameState.Playing:
					topIndicator.SetText( role.ToString() );
					break;
				case OpWalrusGameInfo.GameState.PostGame:
					topIndicator.SetText( "Game Over" );
					break;
				default:
					topIndicator.SetText( "This text shouldn't show." );
					break;
			}

			string talking = "Players talking: ";

			for(int i = 0; i < game.speakingList.Count; i++ )
			{
				talking += game.speakingList[i].GetClientOwner().Name;
				if( i != game.speakingList.Count - 1)
				{
					talking += ", ";
				}
			}
			peopleTalkingIndicator.SetText( talking );

			
			OpWalrusPlayer localPlayer = (OpWalrusPlayer)Local.Pawn;
			FirstPersonCamera localCamera = ((FirstPersonCamera)localPlayer.Camera);
			TraceResult tr = Trace.Ray( localCamera.Pos, localCamera.Pos + (localCamera.Rot.Forward * 2048) ).Ignore( localPlayer ).Run();

			
			if(tr.Entity != null && tr.Entity is OpWalrusPlayer otherPlayer)
			{
				lookingAtIndicator.player = otherPlayer;
				lookingAtIndicator.SetShown( true );
			} else
			{
				lookingAtIndicator.SetShown( false );
			}
		}

		public void ResetTopIndicator()
		{
			topIndicator.SetClass( "guard", false );
			topIndicator.SetClass( "prisoner", false );
		}
	}
}
