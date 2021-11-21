using Sandbox.UI;

namespace OpWalrus
{
	/// <summary>
	/// This is the HUD entity. It creates a RootPanel clientside, which can be accessed
	/// via RootPanel on this entity, or Local.Hud.
	/// </summary>
	public partial class JBHud : Sandbox.HudEntity<RootPanel>
	{
		public JBHud()
		{
			if ( IsClient )
			{
				RootPanel.AddChild<JBNewHud>();
				RootPanel.AddChild<TeamMenu>();
				RootPanel.AddChild<JBWinMenu>();
			}
		}
	}

}
