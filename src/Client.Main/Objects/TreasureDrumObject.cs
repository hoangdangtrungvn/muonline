﻿using Client.Data;
using Client.Main.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;

namespace Client.Main.Objects
{
    [MapObjectType(ModelType.TreasureDrum)]
    public class TreasureDrumObject : WorldObject
    {
        public TreasureDrumObject()
        {
            LightEnabled = true;
        }

        public override async Task Load(GraphicsDevice graphicsDevice)
        {
            Model = await BMDLoader.Instance.Prepare($"Object1/TreasureDrum01.bmd");
            await base.Load(graphicsDevice);
        }
    }
}
