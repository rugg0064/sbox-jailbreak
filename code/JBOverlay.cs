using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	[Library]
	public partial class JBOverlay : Panel
	{
		Label gamemodeIndicator;
		Label currentModeIndicator;
		Label timeLeftIndicator;
		Label hpIndicator;
		Label teamIndicator;
		Label currentWardenIndicator;
		Label peopleTalkingIndicator;
		Label youAreLookingAtIndicator;
		public JBOverlay()
		{ 
			StyleSheet.Load( "OpWalrusHud.scss" );
			AddClass( "overlay" );

			Panel container = new Panel();
			container.AddClass( "container" );
			container.Parent = this;

			gamemodeIndicator = new Label();
			gamemodeIndicator.AddClass( "gamemodeIndicator" );
			gamemodeIndicator.Parent = container;

			currentModeIndicator = new Label();
			currentModeIndicator.AddClass( "currentModeIndicator" );
			currentModeIndicator.Parent = container;


			timeLeftIndicator = new Label();
			timeLeftIndicator.AddClass( "timeLeftIndicator" );
			timeLeftIndicator.Parent = container;


			hpIndicator = new Label();
			hpIndicator.AddClass( "hpIndicator" );
			hpIndicator.Parent = container;


			teamIndicator = new Label();
			teamIndicator.AddClass( "teamIndicator" );
			teamIndicator.Parent = container;


			currentWardenIndicator = new Label();
			currentWardenIndicator.AddClass( "currentWardenIndicator" );
			currentWardenIndicator.Parent = container;

			peopleTalkingIndicator = new Label();
			peopleTalkingIndicator.AddClass( "peopleTalkingIndicator" );
			peopleTalkingIndicator.Parent = container;

			youAreLookingAtIndicator = new Label();
			youAreLookingAtIndicator.AddClass( "youAreLookingAtIndicator" );
			youAreLookingAtIndicator.Parent = container;
		}

		public override void Tick()
		{
			base.Tick();

			JBGame game = (JBGame)Game.Current;
			gamemodeIndicator.SetText( "Jailbreak" );
			currentModeIndicator.SetText( "Mode: " + game.gamestate );

			float lct = game.lastGameStateChangeTime;
			float tn = Time.Now;
			float d = JBGameInfo.gameStateLengths[game.gamestate];

			timeLeftIndicator.SetText( "Time left: " + (d + lct - tn) );
			hpIndicator.SetText( "HP: " + ((JBPlayer) Local.Pawn).Health);

			JBGameInfo.Role role = ((JBPlayer)Local.Pawn).role;
			teamIndicator.SetText( "Team: " + role + " (" + JBGameInfo.roleToTeam[role] + ")");
			JBPlayer warden = game.curWarden;
			if(warden != null)
			{
				currentWardenIndicator.SetText( "Current Warden: " + warden.GetClientOwner().Name + (game.spectators.Contains(warden) ? " (Dead)" : "") );
			}
			else
			{
				currentWardenIndicator.SetText( "Current Warden: " + "None");
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

			
			JBPlayer localPlayer = (JBPlayer)Local.Pawn;
			FirstPersonCamera localCamera = ((FirstPersonCamera)localPlayer.Camera);
			TraceResult tr = Trace.Ray( localCamera.Pos, localCamera.Pos + (localCamera.Rot.Forward * 2048) ).Ignore( localPlayer ).Run();
			string lookingAtText = "Looking at: ";
			
			if(tr.Entity != null && tr.Entity is JBPlayer otherPlayer)
			{
				lookingAtText += otherPlayer.GetClientOwner().Name;
			}
			youAreLookingAtIndicator.SetText( lookingAtText );
		}
	}
}
