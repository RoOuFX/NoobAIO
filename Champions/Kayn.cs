using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using Utility = EnsoulSharp.SDK.Utility;
using SharpDX.Direct3D9;
using NoobAIO.Misc;
using EnsoulSharp.SDK.Prediction;
using System.Collections.Generic;

namespace NoobAIO.Champions
{
    class Kayn
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static void CreateMenu()
        {
            Menu = new Menu("Kayn", "Noob Kayn", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuKeyBind("walk", "Walk to X pos", System.Windows.Forms.Keys.T, KeyBindType.Toggle),
                new MenuKeyBind("useW", "Use W", System.Windows.Forms.Keys.U, KeyBindType.Toggle)
            };
            Menu.Add(comboMenu);
            Menu.Attach();
        }

        public Kayn()
        {
            q = new Spell(SpellSlot.Q);
            e = new Spell(SpellSlot.E);
            w = new Spell(SpellSlot.W, 900f);
            w.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.Line);
            r = new Spell(SpellSlot.R);

            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }
        private static void GameOnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            if (Menu["Combo"].GetValue<MenuKeyBind>("useW").Active)
            {
                w.Cast(Player.Position, false);
            }
            if (Menu["Combo"].GetValue<MenuKeyBind>("walk").Active)
            {
                if (Player.Position.Distance(new Vector3(174, 758, 5748)) < 400)
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, new Vector3(174, 758, 5748));
                }
            }
        }
        private static void OnDraw(EventArgs args)
        {
            Render.Circle.DrawCircle(Player.Position, 10, System.Drawing.Color.DarkCyan);
            Drawing.DrawCircle(new Vector3(78, 1094, 92), 10, System.Drawing.Color.White);
        }
    }
}
