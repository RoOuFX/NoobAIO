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
using EnsoulSharp.SDK.Prediction;

namespace NoobAIO.Champions
{
    class Shyvana
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        public static bool Ractive
        {
            get { return Player.HasBuff("ShyvanaTransformLeap"); }
        }
        #region Menu

        private static void CreateMenu()
        {
            Menu = new Menu("Shyvana", "Noob Shyvana", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.Add(new MenuBool("comboQ", "Use Q"));
            comboMenu.Add(new MenuBool("comboW", "Use W"));
            comboMenu.Add(new MenuBool("comboE", "Use E"));
            comboMenu.Add(new MenuBool("comboR", "Use R"));
            comboMenu.Add(new MenuSlider("MinR", "Use Ult if it hits atleast X  Enemies", 2, 1, 5));
            var heroes = GameObjects.EnemyHeroes;
            foreach (var hero in heroes)
            {
            comboMenu.Add(new MenuBool(hero.CharacterName, "Use R on " + hero.CharacterName));
            }
            Menu.Add(comboMenu);

            // lane clear
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

            // kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksQ", "Use Q"),
                new MenuBool("ksE", "Use E")
            };
            Menu.Add(killstealMenu);

            // Flee
            var miscMenu = new Menu("FleeMenu", "Flee")
            {
                new MenuKeyBind("fleekey", "Flee", System.Windows.Forms.Keys.Z, KeyBindType.Press)
            };
            Menu.Add(miscMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw")
            {
                new MenuBool("Ed", "Draw E"),
                new MenuBool("Rd", "Draw R")
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }
        #endregion
        public Shyvana()
        {
            q = new Spell(SpellSlot.Q, Player.GetRealAutoAttackRange());
            w = new Spell(SpellSlot.W, 325f);
            e = new Spell(SpellSlot.E, 950f);
            e.SetSkillshot(0.25f, 220f, 1575, false, SkillshotType.Line);
            r = new Spell(SpellSlot.R, 850f);
            e.SetSkillshot(0.25f, 160f, 1575, false, SkillshotType.Line);
            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
        }
        private void GameOnGameUpdate(EventArgs args)
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
                case OrbwalkerMode.LaneClear:
                    DoLaneclear();
                    break;
            }
            if (Menu["FleeMenu"].GetValue<MenuKeyBind>("fleekey").Active)
            {
                Flee();
            }
            Killsteal();
        }
        private static void OnDraw(EventArgs args)
        {
            var drawE = Menu["Drawing"].GetValue<MenuBool>("Ed");
            var drawR = Menu["Drawing"].GetValue<MenuBool>("Rd");
            var p = Player.Position;

            if (Player.IsDead || Player.IsRecalling())
                return;

            if (drawE && e.IsReady())
            {
                Render.Circle.DrawCircle(p, e.Range, System.Drawing.Color.Aqua);
            }
            if (drawR && r.IsReady())
            {
                Render.Circle.DrawCircle(p, r.Range, System.Drawing.Color.Aqua);
            }
        }

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(e.Range);
            var targetR = TargetSelector.GetTarget(r.Range);
            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var UseE = Menu["Combo"].GetValue<MenuBool>("comboE");
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var UseR = Menu["Combo"].GetValue<MenuBool>("comboR");
            var MinR = Menu["Combo"].GetValue<MenuSlider>("MinR");
            var Rtargets = Menu["Combo"].GetValue<MenuBool>(target.CharacterName);

            if (!target.IsValidTarget())
            {
                return;
            }
            // R usage
            if (UseR && r.IsReady() && targetR.IsValidTarget(r.Range) && Rtargets)
            {
                if (targetR.Position.CountEnemyHeroesInRange(160) >= MinR)
                {
                    Rusage(targetR);
                }
            }
            // Q usage
            if (UseQ && q.IsReady() && target.IsValidTarget(q.Range))
            {
                q.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            // W usage
            if (UseW && w.IsReady() && target.IsValidTarget(w.Range))
            {
                w.Cast();
            }
            // E usage
            var epred = e.GetPrediction(target);
            if (UseE && e.IsReady() && target.IsValidTarget(e.Range) && epred.Hitchance >= HitChance.High)
            {
                e.Cast(epred.CastPosition);
            }
        }
        private static void DoLaneclear()
        {
            var LaneclearQ = Menu["Clear"].GetValue<MenuBool>("laneclearQ");
            var LaneclearW = Menu["Clear"].GetValue<MenuBool>("laneclearW");
            var LaneclearE = Menu["Clear"].GetValue<MenuBool>("laneclearE");
            var JungleclearQ = Menu["Clear"].GetValue<MenuBool>("jungleclearQ");
            var JungleclearW = Menu["Clear"].GetValue<MenuBool>("jungleclearW");
            var JungleclearE = Menu["Clear"].GetValue<MenuBool>("jungleclearE");

            var allJgl = GameObjects.GetJungles(ObjectManager.Player.Position, e.Range, JungleType.All);
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, e.Range, MinionTypes.All);

            foreach (var minion in allMinions)
            {
                if (q.IsReady() && LaneclearQ && minion.IsValidTarget() && q.IsInRange(minion))
                {
                    q.Cast(minion);
                }
                if (w.IsReady() && LaneclearW && minion.IsValidTarget() && w.IsInRange(minion))
                {
                    w.Cast(minion);
                }
                if (e.IsReady() && LaneclearE && minion.IsValidTarget() && e.IsInRange(minion))
                {
                    e.Cast(minion);
                }
            }
            foreach (var jgl in allJgl)
            {
                if (q.IsReady() && jgl.IsValidTarget() && q.IsInRange(jgl) && JungleclearQ)
                {
                    q.Cast(jgl);
                }

                if (w.IsReady() && jgl.IsValidTarget() && w.IsInRange(jgl) && JungleclearW)
                {
                    w.Cast(jgl);
                }
                if (e.IsReady() && jgl.IsValidTarget() && e.IsInRange(jgl) && JungleclearE)
                {
                    e.Cast(jgl);
                }
            }
        }
        private static void Killsteal()
        {
            var target = TargetSelector.GetTarget(e.Range);
            var qdmg = Player.GetSpellDamage(target, SpellSlot.Q);
            var edmg = Player.GetSpellDamage(target, SpellSlot.E);
            var ksq = Menu["KillSteal"].GetValue<MenuBool>("ksQ");
            var ksE = Menu["KillSteal"].GetValue<MenuBool>("ksE");
            if (!target.IsValidTarget())
            {
                return;
            }
            if (q.IsInRange(target) && q.IsReady() && qdmg > target.Health + target.AllShield && ksq)
            {
                q.CastOnUnit(target);
            }
            if (q.IsInRange(target) && e.IsReady() && edmg > target.Health + target.AllShield && ksq && ksE)
            {
                e.Cast();
            }
        }
        private static void Flee()
        {
            Orbwalker.Move(Game.CursorPos);
            if (!r.IsReady() || !w.IsReady())
            {
                return;
            }
            if(w.IsReady())
            {
                w.Cast();
            }
            if (r.IsReady())
            {
                r.Cast(Game.CursorPos);
            }
        }
        public static void Rusage(AIBaseClient target)
        {
            var castPosition = r.GetPrediction(target).CastPosition;
            castPosition = Player.Position.Extend(castPosition, r.Range);
            r.Cast(castPosition);
        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "ShyvanaDoubleAttack" || args.SData.Name == "ShyvanaDoubleAttackDragon")
                Orbwalker.ResetAutoAttackTimer();
        }
    }
}
