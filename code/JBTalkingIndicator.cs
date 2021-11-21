using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	public partial class JBTalkingIndicator : Panel
	{
		Label talkingLabel;
		JBPlayerLabel playerName;

		public JBTalkingIndicator(JBPlayer ply)
		{
			AddClass( "talkingIndicator" );

			talkingLabel = new Label();
			talkingLabel.AddClass( "talkingLabel" );
			talkingLabel.Parent = this;
			talkingLabel.Text = "TALKING:";

			playerName = new JBPlayerLabel();
			playerName.AddClass( "talkingPlayerName" );
			playerName.Parent = this;
			playerName.player = ply;
		}

	}
}
