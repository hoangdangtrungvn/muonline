﻿using Client.Data;
using Client.Main.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;

namespace Client.Main.Objects
{
    [MapObjectType(min: ModelType.HouseEtc01, max: ModelType.HouseEtc03)]
    public class HouseEtcObject : WorldObject
    {
        public HouseEtcObject()
        {
            LightEnabled = false;
        }

        public override async Task Load(GraphicsDevice graphicsDevice)
        {
            var idx = (Type - (ushort)ModelType.HouseEtc01 + 1).ToString().PadLeft(2, '0');
            Model = await BMDLoader.Instance.Prepare($"Object1/HouseEtc{idx}.bmd");
            await base.Load(graphicsDevice);
        }
    }
}
