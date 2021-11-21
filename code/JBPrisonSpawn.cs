using Sandbox;

namespace OpWalrus
{
	[Library( "opwps", Description = "Spawn a prisoner here" )]
	[Hammer.EditorModel( "models/editor/playerstart.vmdl" )]
	[Hammer.EntityTool( "Prisoner Spawn", "Player", "Defines a point where the prisoners can (re)spawn" )]
	public partial class opwps : Entity
	{

	}
}
