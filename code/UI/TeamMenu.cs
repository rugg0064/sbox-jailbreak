
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

[Library]
public partial class TeamMenu : Panel
{
	public static TeamMenu Instance;

	public TeamMenu()
	{
		Instance = this;

		StyleSheet.Load( "/ui/TeamMenu.scss" );

		var left = Add.Panel( "left" );
		{
			var body = left.Add.Panel( "body" );
			{
				var policeBody = body.Add.Panel( "policeBody" );
				{
					var policeButton = policeBody.Add.Button( "Police", "policeButton" );
					policeButton.AddEventListener("onclick", () =>
						{
							ConsoleSystem.Run( "trySwitchTeam Guards" );
							Log.Info( "Clicked on police" );
						}
					);
				}
				var prisonerBody = body.Add.Panel( "prisonerBody" );
				{
					var prisonerButton = prisonerBody.Add.Button( "Prisoner", "prisonerButton" );
					prisonerButton.AddEventListener("onclick", () =>
						{
							ConsoleSystem.Run( "trySwitchTeam Prisoners" );
						}
					
					);
				}
			}
		}
		
	}

	public override void Tick()
	{
		base.Tick();

		Parent.SetClass( "teammenuopen", Input.Down( InputButton.Menu ) );
	}

}
