
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace OpWalrus
{
	[Library]
	public partial class TeamMenu : Panel
	{
		Label wardenText;
		Panel wardenPanel;
		public TeamMenu()
		{
			StyleSheet.Load( "/ui/TeamMenu.scss" );

			Panel body = Add.Panel( "body" );

			Panel policeBody = body.Add.Panel( "policeBody" );
			policeBody.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "trySwitchTeam Guards" );
			} );
			Label policeText = policeBody.Add.Label( "Guards", "policeText" );

			wardenPanel = Add.Panel( "wardenPanel" );
			wardenPanel.AddEventListener( "onclick", () =>
			{
				OpWalrusPlayer.flipWardenOptin();
			} );
			wardenPanel.Add.Label( "Opt in to warden?", "textA" );
			wardenText = wardenPanel.Add.Label( "", "textB" );

			Panel prisonerBody = body.Add.Panel( "prisonerBody" );
			prisonerBody.AddEventListener( "onclick", () =>
			{
				ConsoleSystem.Run( "trySwitchTeam Prisoners" );
			} );
			Label prisonerText = prisonerBody.Add.Label( "Prisoners", "prisonerText" );
		}

		public override void Tick()
		{
			base.Tick();

			OpWalrusPlayer localPlayer = ((OpWalrusPlayer)Local.Pawn);

			Parent.SetClass( "teammenuopen", Input.Down( InputButton.Menu ) );

			wardenText.SetText( localPlayer.optinWarden ? "Yes" : "No" );
			
			wardenPanel.SetClass( "visible", OpWalrusGameInfo.roleToTeam[localPlayer.role] == OpWalrusGameInfo.Team.Guards );
		}

	}

}
