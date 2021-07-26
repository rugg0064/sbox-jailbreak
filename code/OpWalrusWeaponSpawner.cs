using Sandbox;

namespace OpWalrus
{
	[Library( "wepSpawner", Description = "Spawns weapons according to an input" )]
	[Hammer.EditorModel( "models/maya_testcube_100.vmdl" )]
	public partial class OpWalrusWeaponSpawner : Entity
	{
		[Property( Title = "Weapon to spawn" )]
		public int ID { get; set; } = 0;


		[Input]
		public void spawnItem()
		{
			Entity p;
			switch ( ID )
			{
				case 0:
					p = new Pistol();
					p.Position = this.Position;
					p.Spawn();
					break;
				case 1:
					p = new Shotgun();
					p.Position = this.Position;
					p.Spawn();
					break;
				case 2:
					p = new SMG();
					p.Position = this.Position;
					p.Spawn();
					break;
			}
		}
	}
}
