using StrikeTheEarth.Entities;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace StrikeTheEarth.Items
{
	internal class ItemThrowingStick : Item
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			var bh = GetCollectibleBehavior<CollectibleBehaviorAnimationAuthoritative>(true);

			if (bh == null)
			{
				//api.World.Logger.Warning("Spear {0} uses ItemSpear class, but lacks required AnimationAuthoritative behavior. I'll take the freedom to add this behavior, but please fix json item type.", Code);
				bh = new CollectibleBehaviorAnimationAuthoritative(this);
				bh.OnLoaded(api);
				CollectibleBehaviors = CollectibleBehaviors.Append(bh);
			}

			bh.OnBeginHitEntity += ItemThrowingStick_OnBeginHitEntity;
		}

		private void ItemThrowingStick_OnBeginHitEntity(EntityAgent byEntity, ref EnumHandling handling)
		{
			if (byEntity.World.Side == EnumAppSide.Client)
			{
				return;
			}

			var entitySel = (byEntity as EntityPlayer)?.EntitySelection;

			if (byEntity.Attributes.GetInt("didattack") == 0 && entitySel != null)
			{
				byEntity.Attributes.SetInt("didattack", 1);
			}
		}

		public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
		{
			return null;
		}

		public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			if (handling == EnumHandHandling.PreventDefault) return;

			handling = EnumHandHandling.PreventDefault;

			// Not ideal to code the aiming controls this way. Needs an elegant solution - maybe an event bus?
			byEntity.Attributes.SetInt("aiming", 1);
			byEntity.Attributes.SetInt("aimingCancel", 0);
			byEntity.StartAnimation("aim");
		} 

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			return true;
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
		{
			byEntity.Attributes.SetInt("aiming", 0);
			byEntity.StopAnimation("aim");

			if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
			{
				byEntity.Attributes.SetInt("aimingCancel", 1);
			}

			return true;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
			if (byEntity.Attributes.GetInt("aimingCancel") == 1) return;

			byEntity.Attributes.SetInt("aiming", 0);
			byEntity.StopAnimation("aim");

			if (secondsUsed < 0.35f) return;

			float damage = 1.5f;

			if (slot.Itemstack.Collectible.Attributes != null)
			{
				damage = slot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
			}

			(api as ICoreClientAPI)?.World.AddCameraShake(0.17f);

			ItemStack stack = slot.TakeOut(1);
			slot.MarkDirty();

			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

			byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, false, 8);

			EntityProperties type = byEntity.World.GetEntityType(new AssetLocation("striketheearth:thrownstick"));
			EntityThrownStick enpr = byEntity.World.ClassRegistry.CreateEntity(type) as EntityThrownStick;
			enpr.FiredBy = byEntity;
			enpr.Damage = damage;
			enpr.DamageTier = Attributes["damageTier"].AsInt(0);
			enpr.ProjectileStack = stack;
			enpr.DropOnImpactChance = 1.1f;
			enpr.DamageStackOnImpact = true;
			enpr.Weight = 0.3f;


			float acc = (1 - byEntity.Attributes.GetFloat("aimingAccuracy", 0));
			double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * 0.75;
			double rndyaw = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * 0.75;

			Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y - 0.2, 0);

			Vec3d aheadPos = pos.AheadCopy(1, byEntity.ServerPos.Pitch + rndpitch, byEntity.ServerPos.Yaw + rndyaw);
			Vec3d velocity = (aheadPos - pos) * 0.65 * byEntity.Stats.GetBlended("bowDrawingStrength");
			Vec3d spawnPos = byEntity.ServerPos.BehindCopy(0.15).XYZ.Add(byEntity.LocalEyePos.X, byEntity.LocalEyePos.Y - 0.2, byEntity.LocalEyePos.Z);

			enpr.ServerPos.SetPosWithDimension(spawnPos);
			enpr.ServerPos.Motion.Set(velocity);


			enpr.Pos.SetFrom(enpr.ServerPos);
			enpr.World = byEntity.World;
			enpr.SetRotation();

			byEntity.World.SpawnEntity(enpr);
			byEntity.StartAnimation("throw");

			if (byEntity is EntityPlayer) RefillSlotIfEmpty(slot, byEntity, (itemstack) => itemstack.Collectible is ItemSpear);

			var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;
			byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)api.World.Rand.NextDouble() * 0.2f, 16, 0.35f);
		}

		public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
		{

		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			if (inSlot.Itemstack.Collectible.Attributes == null) return;

			float damage = 1.5f;

			if (inSlot.Itemstack.Collectible.Attributes != null)
			{
				damage = inSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
			}

			dsc.AppendLine(damage + Lang.Get("piercing-damage-thrown"));
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
		{
			return new WorldInteraction[] {
				new WorldInteraction()
				{
					ActionLangCode = "heldhelp-throw",
					MouseButton = EnumMouseButton.Right,
				}
			}.Append(base.GetHeldInteractionHelp(inSlot));
		}
	}
}
