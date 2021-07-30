using Sandbox;
using Sandbox.UI;

namespace OpWalrus
{
	public partial class OpWalrusTalkingIndicator : Panel
	{
		Label talkingLabel;
		OpWalrusPlayerLabel playerName;

		public OpWalrusTalkingIndicator(OpWalrusPlayer ply)
		{
			AddClass( "talkingIndicator" );

			talkingLabel = new Label();
			talkingLabel.AddClass( "talkingLabel" );
			talkingLabel.Parent = this;
			talkingLabel.Text = "TALKING:";

			playerName = new OpWalrusPlayerLabel();
			playerName.AddClass( "talkingPlayerName" );
			playerName.Parent = this;
			playerName.player = ply;
		}

	}
}
