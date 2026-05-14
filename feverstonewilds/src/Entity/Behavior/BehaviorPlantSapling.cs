using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

public class BehaviorPlantSapling : EntityBehavior
{

    private float timer;
    private readonly float interval = 600f;
    private static readonly string[] Saplings = new string[]
	{
		"sapling-oak-free",
		"sapling-birch-free",
		"sapling-maple-free",
		"sapling-pine-free"
	};
    public BehaviorPlantSapling(Entity entity) : base(entity) { }

    public override string PropertyName() => "plantSapling";

    public override void OnGameTick(float deltaTime)
    {
        if (entity.World.Side != EnumAppSide.Server) return;

        timer += deltaTime;
        bool tlessi = timer < interval;
        if (!tlessi)
        {
            timer = 0f;
            entity.World.Logger.Notification("[Faunling] -- Trying to plant a sapling...");
            TryPlant();
        }
    }
    public void OnReceivedServerPacket(int packetid)
    {
        if (packetid != 1515) return;

        if (entity is EntityAgent agent)
        {
            agent.AnimManager?.StartAnimation(new AnimationMetaData()
            {
                Animation = "Plant",
                Code = "plant",
                BlendMode = EnumAnimationBlendMode.Average,
                EaseInSpeed = 6f,
                EaseOutSpeed = 6f,
                AnimationSpeed = 1f
            });
        }
    }

    private void TryPlant()
	{
		BlockPos groundPos = entity.Pos.AsBlockPos.Copy();
		groundPos.Y--;
		BlockPos plantPos = groundPos.UpCopy();

		IBlockAccessor ba = entity.World.BlockAccessor;

		Block ground = ba.GetBlock(groundPos);
		Block above = ba.GetBlock(plantPos);

		bool flag = above.Id != 0;
		if (!flag)
		{
			bool flag2 = (ground.Code.FirstCodePart() != "soil" || ground.Code.FirstCodePart() != "tallgrass") && above.Code.FirstCodePart() != "air";
			if (!flag2)
			{
				string saplingCode = Saplings[entity.World.Rand.Next(Saplings.Length)];
				Block sapling = entity.World.GetBlock(new AssetLocation(saplingCode));
				bool flag3 = sapling == null;
				if (!flag3)
				{
                    (entity.Api as ICoreServerAPI).Network.BroadcastEntityPacket(entity.EntityId, 1515, null);
					ba.SetBlock(sapling.Id, plantPos);
				}
			}
		}
	}
}