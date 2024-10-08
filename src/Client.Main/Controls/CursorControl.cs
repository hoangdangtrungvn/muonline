﻿using Client.Main.Controls.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Main.Controls
{
    public enum CursorStatus
    {
        Normal,
        AddIn,
        Attack,
        Attack2,
        DontMove,
        Eye,
        Get,
        Id,
        LeanAgainst,
        Push,
        Repair,
        SitDown,
        Talk
    }

    public class CursorControl : TextureControl
    {
        public CursorControl()
        {
            BlendState = Blendings.Alpha;
            TexturePath = "Interface/Cursor.tga";
        }

        public override void Update(GameTime gameTime)
        {
            X = MuGame.Instance.Mouse.X;
            Y = MuGame.Instance.Mouse.Y;

            if (MuGame.Instance.Mouse.LeftButton == ButtonState.Pressed)
                TexturePath = "Interface/CursorPush.tga";
            else
                TexturePath = "Interface/Cursor.tga";

            base.Update(gameTime);
        }
    }
}
