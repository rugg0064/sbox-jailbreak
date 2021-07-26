using Sandbox;

namespace OpWalrus
{
	[Library( "opwgs", Description = "Spawn a guard here" )]
	[Hammer.EditorModel( "models/editor/playerstart.vmdl" )]
	[Hammer.EntityTool( "Guard Spawn", "Player", "Defines a point where the prisoners can (re)spawn" )]
	public partial class opwgs : Entity
	{

	}
}
