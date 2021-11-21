using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	// A class for prefixing player names with dead status and role
	[Library]
	public partial class JBPlayerLabel : Panel
	{
		public JBPlayer player;
		Label playerName;
		Label roleIndicator;
		Label deadIndicator;

		public JBPlayerLabel()
		{
			AddClass( "playerLabel" );

			deadIndicator = new Label();
			deadIndicator.AddClass( "indicatorDead" );
			deadIndicator.Parent = this;
			deadIndicator.Text = "DEAD";

			roleIndicator = new Label();
			roleIndicator.AddClass( "indicatorRole" );
			roleIndicator.Parent = this;

			playerName = new Label();
			playerName.AddClass( "playerLabelName" );
			playerName.Parent = this;
		}

		public override void Tick()
		{
			base.Tick();
			if (player != null && player.IsValid())
			{
				playerName.Text = player.GetClientOwner().Name;
				deadIndicator.SetClass( "visible", player.Health <= 0 );
				roleIndicator.RemoveClass( "warden" );
				roleIndicator.RemoveClass( "prisoner" );
				roleIndicator.RemoveClass( "guard" );
				switch ( player.role )
				{
					case JBGameInfo.Role.Warden:
						roleIndicator.AddClass( "warden" );
						roleIndicator.SetText( "WARDEN" );
						break;
					case JBGameInfo.Role.Guard:
						roleIndicator.AddClass( "guard" );
						roleIndicator.SetText( "GUARD" );
						break;
					case JBGameInfo.Role.Prisoner:
						roleIndicator.AddClass( "prisoner" );
						roleIndicator.SetText( "PRISONER" );
						break;
					default:
						// role indicator is not visible by default
						break;
				}
			} else
			{
				playerName.Text = "";
				deadIndicator.SetClass( "visible", false );
				roleIndicator.RemoveClass( "warden" );
				roleIndicator.RemoveClass( "prisoner" );
				roleIndicator.RemoveClass( "guard" );
			}
		}
		
		public void SetShown(bool x)
		{
			SetClass( "hidden", !x );
		}
	}
}
