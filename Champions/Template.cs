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
    class Template
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static void CreateMenu()
        {
            Menu = new Menu("Template", "Noob Template", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuBool("comboQ", "Use Q"),
                new MenuBool("comboW", "Use W"),
                new MenuBool("comboE", "Use E"),
                new MenuBool("comboR", "Use R")
            };
            Menu.Add(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "Harass")
            {
                new MenuBool("harassQ", "Use Q"),
                new MenuBool("harassW", "Use W"),
                new MenuBool("harassE", "Use E"),
                new MenuBool("harassR", "Use R")
            };
            Menu.Add(harassMenu);

            // Lane clear
            var laneclearMenu = new Menu("Clear", "Farming")
            {
                new MenuSeparator("Head1", "Lane Clear"),
                new MenuBool("laneclearQ", "Use Q"),
                new MenuBool("laneclearW", "Use W"),
                new MenuBool("laneclearE", "Use E"),
                new MenuSeparator("Head2", "Jungle Clear"),
                new MenuBool("jungleclearQ", "Use Q"),
                new MenuBool("jungleclearW", "Use W"),
                new MenuBool("jungleclearE", "Use E")
            };
            Menu.Add(laneclearMenu);

            // Kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksQ", "Use Q"),
                new MenuBool("ksW", "Use W"),
                new MenuBool("ksE", "Use E"),
                new MenuBool("ksR", "Use R")
            };
            Menu.Add(killstealMenu);

            // Misc
            var miscMenu = new Menu("Misc", "Misc")
            {
                new MenuBool("Miscstuff", "Gapcloser")
            };
            Menu.Add(miscMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw")
            {
                new MenuBool("Qd", "Draw Q"),
                new MenuBool("Wd", "Draw W"),
                new MenuBool("Ed", "Draw E"),
                new MenuBool("Rd", "Draw R")
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }
        
        public Template()
        {
            q = new Spell(SpellSlot.Q);
            e = new Spell(SpellSlot.E);
            w = new Spell(SpellSlot.W);
            r = new Spell(SpellSlot.R);

            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }
        private static void GameOnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoLaneclear();
                    break;
            }
            // KS
            Active();
        }
        private static void DoCombo()
        {

        }
        private static void DoHarass()
        {

        }
        private static void DoLaneclear()
        {

        }
        private static void Active()
        {

        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "AttackBuff")
                Orbwalker.ResetAutoAttackTimer();
        }
        private static void OnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("Qd");
            var drawW = Menu["Drawing"].GetValue<MenuBool>("Wd");
            var drawE = Menu["Drawing"].GetValue<MenuBool>("Ed");
            var drawR = Menu["Drawing"].GetValue<MenuBool>("Rd");
            var p = Player.Position;

            if (drawQ && q.IsReady())
            {
                Render.Circle.DrawCircle(p, q.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawW && w.IsReady())
            {
                Render.Circle.DrawCircle(p, w.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawE && e.IsReady())
            {
                Render.Circle.DrawCircle(p, e.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawR && r.IsReady())
            {
                Render.Circle.DrawCircle(p, r.Range, System.Drawing.Color.DarkCyan);
            }
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (!Menu["Misc"].GetValue<MenuBool>("Miscstuff"))
            {
                return;
            }
            if (w.IsReady() && sender != null && sender.IsValidTarget(w.Range))
            {
                if (sender.IsMelee)
                {
                    if (sender.IsValidTarget(sender.AttackRange + sender.BoundingRadius + 100))
                    {
                        w.Cast();
                    }
                }

                if (sender.IsDashing())
                {
                    if (args.EndPosition.DistanceToPlayer() <= 250 ||
                        sender.PreviousPosition.DistanceToPlayer() <= 300)
                    {
                        w.Cast();
                    }
                }

                if (sender.IsCastingImporantSpell())
                {
                    if (sender.PreviousPosition.DistanceToPlayer() <= 300)
                    {
                        w.Cast();
                    }
                }
            }
        }
    }
}
