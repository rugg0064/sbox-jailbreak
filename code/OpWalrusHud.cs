using Sandbox.UI;

namespace OpWalrus
{
	/// <summary>
	/// This is the HUD entity. It creates a RootPanel clientside, which can be accessed
	/// via RootPanel on this entity, or Local.Hud.
	/// </summary>
	public partial class OpWalrusHud : Sandbox.HudEntity<RootPanel>
	{
		public OpWalrusHud()
		{
			if ( IsClient )
			{
				RootPanel.AddChild<OpWalrusOverlay>();
				RootPanel.AddChild<OpWalrusWinMenu>();
				RootPanel.AddChild<TeamMenu>();
			}
		}
	}

}
