using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	[Library]
	public partial class JBNewHud : Panel
	{
		Label currentModeIndicator;
		Label timeLeftIndicator;
		Label hpIndicator;
		Label topIndicator;
		Panel currentWardenIndicator;
		JBPlayerLabel wardenName;
		Label noWardenIndicator;
		Label peopleTalkingIndicator;
		JBPlayerLabel lookingAtIndicator;

		int TalkersCount = 0;

		Panel talkingContainer;

		public JBNewHud()
		{ 
			StyleSheet.Load( "JBNewHud.scss" );
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

			wardenName = new JBPlayerLabel();
			wardenName.AddClass( "wardenName" );
			wardenName.Parent = currentWardenIndicator;

			noWardenIndicator = new Label();
			noWardenIndicator.AddClass( "noWardenIndicator" );
			noWardenIndicator.Parent = currentWardenIndicator;

			peopleTalkingIndicator = new Label();
			peopleTalkingIndicator.AddClass( "peopleTalkingIndicator" );
			peopleTalkingIndicator.Parent = this;

			lookingAtIndicator = new JBPlayerLabel();
			lookingAtIndicator.AddClass( "lookingAtIndicator" );
			lookingAtIndicator.Parent = this;

			talkingContainer = new Panel();
			talkingContainer.AddClass( "talkingContainer" );
			talkingContainer.Parent = this;
		}

		public override void Tick()
		{
			base.Tick();

			JBGame game = (JBGame)Game.Current;

			float lct = game.lastGameStateChangeTime;
			float tn = Time.Now;
			float d = JBGameInfo.gameStateLengths[game.gamestate];

			//timeLeftIndicator.SetText( "Time left: " + (d + lct - tn) );
			timeLeftIndicator.SetText( FormatTimer( (d + lct - tn) ) );
			float hp = ((JBPlayer)Local.Pawn).Health;
			hpIndicator.SetText( "+ " + hp);

			JBGameInfo.Role role = ((JBPlayer)Local.Pawn).role;
			JBPlayer warden = game.curWarden;
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
			if (game.gamestate == JBGameInfo.GameState.Playing)
			{
				currentWardenIndicator.SetClass( "hidden", false );
				if (hp > 0) {
					switch ( JBGameInfo.roleToTeam[role] )
					{
						case JBGameInfo.Team.Prisoners:
							topIndicator.SetClass( "prisoner", true );
							break;
						case JBGameInfo.Team.Guards:
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

			timeLeftIndicator.SetClass( "hidden", false );

			switch ( game.gamestate )
			{
				case JBGameInfo.GameState.EnoughPlayersCheck:
					topIndicator.SetText( "Waiting For Players" );
					timeLeftIndicator.SetClass( "hidden", true );
					break;
				case JBGameInfo.GameState.Pregame:
					topIndicator.SetText( "Pregame" );
					break;
				case JBGameInfo.GameState.Playing:
					topIndicator.SetText( role.ToString() );
					break;
				case JBGameInfo.GameState.PostGame:
					topIndicator.SetText( "Game Over" );
					break;
				default:
					topIndicator.SetText( "This text shouldn't show." );
					break;
			}

			//Log.Error( game.speakingList.Count );
			if ( TalkersCount != game.speakingList.Count )
			{
				RefreshTalkers();
				TalkersCount = game.speakingList.Count;
			}


			JBPlayer localPlayer = (JBPlayer)Local.Pawn;
			FirstPersonCamera localCamera = ((FirstPersonCamera)localPlayer.Components.Get<CameraMode>());
			TraceResult tr = Trace.Ray( localCamera.Position, localCamera.Position + (localCamera.Rotation.Forward * 2048) ).Ignore( localPlayer ).Run();

			
			if(tr.Entity != null && tr.Entity is JBPlayer otherPlayer)
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
		
		public string FormatTimer(float time)
		{
			int secs = MathX.CeilToInt( time );
			float mins = secs / 60;
			int roundMins = MathX.FloorToInt( mins );
			int minsSecs = secs - roundMins*60;
			string secPortion = minsSecs.ToString();
			if (minsSecs < 10)
			{
				secPortion = "0" + secPortion;
			}
			return roundMins + ":" + secPortion;
		}

		private void RefreshTalkers()
		{
			JBGame game = (JBGame)Game.Current;
			talkingContainer.DeleteChildren();

			for ( int i = 0; i < game.speakingList.Count; i++ )
			{
				JBPlayer speaker = game.speakingList[i];
				JBTalkingIndicator newInd = new JBTalkingIndicator( speaker );
				switch ( JBGameInfo.roleToTeam[speaker.role] )
				{
					case JBGameInfo.Team.Prisoners:
						newInd.AddClass( "prisoner" );
						break;
					case JBGameInfo.Team.Guards:
						newInd.AddClass( "guard" );
						break;
					default:
						break;
				}
				talkingContainer.AddChild( newInd );
			}
		}
	}
}
