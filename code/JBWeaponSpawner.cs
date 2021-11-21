using Sandbox;
using System.Threading.Tasks;

namespace OpWalrus
{
	[Library( "wepSpawner", Description = "Spawns weapons according to an input" )]
	[Hammer.EditorModel( "models/maya_testcube_100.vmdl" )]
	public partial class JBWeaponSpawner : Entity
	{
		[Property( Title = "Weapon to spawn" )]
		public int ID { get; set; } = 0;


		[Input]
		public void spawnItem()
		{
			Entity p = null;
			switch ( ID )
			{
				case 0:
					//p = Library.Create<Pistol>();
					p = new SWB_WEAPONS.Bayonet();
					//p = new Pistol();
					break;
				case 1:
					//p = Library.Create<Shotgun>();
					p = new SWB_WEAPONS.DEAGLE();
					//p = new Shotgun();
					break;
				case 2:
					//p = Library.Create<SMG>();
					p = new SWB_WEAPONS.SPAS12();
					//p = new SMG();
					break;
				case 3:
					//p = Library.Create<SMG>();
					p = new SWB_WEAPONS.FAL();
					//p = new SMG();
					break;
			}
			p.Position = this.Position;
		}

		protected async Task createWeapon(int id)
		{
			await GameTask.NextPhysicsFrame();
			
		}
	}
}
