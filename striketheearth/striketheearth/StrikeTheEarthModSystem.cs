using StrikeTheEarth.Entities;
using StrikeTheEarth.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace StrikeTheEarth
{
    public class StrikeTheEarthModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
            api.RegisterItemClass(Mod.Info.ModID + ".throwingstick", typeof(ItemThrowingStick));
            api.RegisterEntity(Mod.Info.ModID + ".thrownstick", typeof(EntityThrownStick));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("striketheearth:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("striketheearth:hello"));
        }
    }
}
