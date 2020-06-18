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
    class Katarina
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static void CreateMenu()
        {
            Menu = new Menu("Katarina", "Noob Katarina", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuList("comboMode", "Combo mode:", new [] { "Q - E", "E - Q" }) { Index = 1 },
                new MenuBool("comboQ", "Use Q"),
                new MenuBool("comboW", "Use W"),
                new MenuSeparator("Head 1", "E usage"),
                new MenuBool("comboE", "Use E"),
                new MenuBool("eTurret", "Don't E under Turret", false),
                new MenuBool("eDagger", "Only E if Dagger", false),
                new MenuList("comboEMode", "E logic:", new [] { "Infront", "Behind" }) { Index = 0 },
                new MenuSeparator("Head 2", "R usage"),
                new MenuList("comboRMode", "R logic:", new [] { "If target has X% health", "If killable", "Never" }) { Index = 1 },
                new MenuSlider("MinRHealth", "^ if target has x % health use R", 40, 0, 100),
                new MenuBool("noR", "Dont channel R if no enemies are around")
            };
            Menu.Add(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "Harass")
            {
                new MenuBool("harassQ", "Auto Q harass", false)
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
                new MenuBool("ksE", "Use E"),
                new MenuBool("ksEd", "Use E to ks with daggers"),
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
                new MenuBool("Ed", "Draw E"),
                new MenuBool("Rd", "Draw R"),
                new MenuBool("Dd", "Draw Daggers"),
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }

        public Katarina()
        {
            q = new Spell(SpellSlot.Q, 625f);
            e = new Spell(SpellSlot.E, 725f);
            w = new Spell(SpellSlot.W, 350f);
            r = new Spell(SpellSlot.R, 550f);

            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }
        private static void GameOnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (Player.HasBuff("katarinarsound"))
            {
                Orbwalker.MovementState = false;
                Orbwalker.AttackState = false;
            }
            else
            {
                Orbwalker.MovementState = true;
                Orbwalker.AttackState = true;
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
            // KS - Harass
            Active();
        }
        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(e.Range);
            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var UseE = Menu["Combo"].GetValue<MenuBool>("comboE");
            var UseETurret = Menu["Combo"].GetValue<MenuBool>("eTurret");
            var SaveE = Menu["Combo"].GetValue<MenuBool>("eDagger");
            var noR = Menu["Combo"].GetValue<MenuBool>("noR");
            var eMode = Menu["Combo"].GetValue<MenuList>("comboEMode");
            var rMode = Menu["Combo"].GetValue<MenuList>("comboRMode");
            var minRHealth = Menu["Combo"].GetValue <MenuSlider>("MinRHealth");

            if (!target.IsValidTarget())
            {
                return;
            }

            if (Player.HasBuff("katarinarsound"))
            {
                if (noR)
                {
                    if (Player.CountEnemyHeroesInRange(r.Range) == 0)
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    }
                }
                if (target != null && UseQ && UseE && Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.E) >= target.Health)
                {
                    foreach (var daggers in GameObjects.AllGameObjects)
                    {
                        if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid && e.IsReady())
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            if (target.Distance(daggers) < 450 && target.IsValidTarget(e.Range) && e.IsReady())
                            {
                                e.Cast(daggers.Position.Extend(target.Position, 200));
                            }

                            if (daggers.Distance(Player) > e.Range)
                            {
                                e.Cast(target.Position.Extend(Player.Position, -50));
                            }
                            if (daggers.Distance(target) > 450)
                            {

                                e.Cast(target.Position.Extend(Player.Position, -50));
                            }
                        }
                        if (!Daggers.Any() && e.IsReady())
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            e.Cast(target.Position.Extend(Player.Position, -50));
                        }
                        if (target.IsValidTarget(q.Range) && q.IsReady())
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            q.CastOnUnit(target);
                        }
                    }
                }
            }
            
            switch (Menu["Combo"].GetValue<MenuList>("comboMode").Index)
            {
                case 0:

                    if (q.IsReady() && UseQ && target.IsValidTarget(q.Range))
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            q.CastOnUnit(target);
                        }
                    }

                    if (e.IsReady() && UseE && target.IsValidTarget(e.Range) &&
                        !q.IsReady())
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            if (UseETurret && target.IsUnderEnemyTurret())
                            {
                                return;
                            }
                            foreach (var daggers in GameObjects.AllGameObjects)
                            {
                                if (!SaveE)
                                {
                                    if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                                    {
                                        if (target.Distance(daggers) < 450 && target.IsValidTarget(e.Range))
                                        {
                                            e.Cast(daggers.Position.Extend(target.Position, 200));
                                        }
                                        else
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                        if (daggers.Distance(Player) > e.Range)
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                        if (daggers.Distance(target) > 450)
                                        {

                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                    }
                                    if (!Daggers.Any())
                                    {
                                        if (eMode.Index == 0)
                                        {
                                            e.Cast(target.Position.Extend(Player.Position, -50));
                                        }
                                        else if (eMode.Index == 1)
                                        {
                                            e.Cast(target.Position.Extend(Player.Position, 50));
                                        }
                                    }
                                }
                                if (SaveE)
                                {
                                    if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                                    {
                                        if (target.Distance(daggers) < 450 && target.IsValidTarget(e.Range))
                                        {
                                            e.Cast(daggers.Position.Extend(target.Position, 200));
                                        }
                                        else
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (w.IsReady() && UseW && target.IsValidTarget(w.Range))
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            w.Cast();
                        }
                    }

                    if (r.IsReady() && rMode.Index != 2)
                    {
                        if (rMode.Index == 0)
                        {
                            if (target.IsValidTarget(r.Range - 150))
                            {
                                if (target != null && target.HealthPercent <= minRHealth.Value)
                                {
                                    r.Cast();
                                }
                            }
                        }
                        if (rMode.Index == 1)
                        {
                            if (target.IsValidTarget(r.Range - 150))
                            {
                                if (target != null && target.Health <= Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.E) + Daggersdmg(target) + Rdmg(target) * 10)
                                {
                                    if (!q.IsReady())
                                    {
                                        r.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;

                case 1:

                    if (e.IsReady() && UseE && target.IsValidTarget(e.Range))
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            if (UseETurret && target.IsUnderEnemyTurret())
                            {
                                return;
                            }
                            foreach (var daggers in GameObjects.AllGameObjects)
                            {
                                if (!SaveE)
                                {
                                    if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                                    {
                                        if (target.Distance(daggers) < 450 && target.IsValidTarget(e.Range))
                                        {
                                            e.Cast(daggers.Position.Extend(target.Position, 200));
                                        }
                                        else
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                        if (daggers.Distance(Player) > e.Range)
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                        if (daggers.Distance(target) > 450)
                                        {

                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                    }
                                    if (!Daggers.Any())
                                    {
                                        if (eMode.Index == 0)
                                        {
                                            e.Cast(target.Position.Extend(Player.Position, -50));
                                        }
                                        else if (eMode.Index == 1)
                                        {
                                            e.Cast(target.Position.Extend(Player.Position, 50));
                                        }
                                    }
                                }
                                if (SaveE)
                                {
                                    if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                                    {
                                        if (target.Distance(daggers) < 450 && target.IsValidTarget(e.Range))
                                        {
                                            e.Cast(daggers.Position.Extend(target.Position, 200));
                                        }
                                        else
                                        {
                                            if (eMode.Index == 0)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, -50));
                                            }
                                            else if (eMode.Index == 1)
                                            {
                                                e.Cast(target.Position.Extend(Player.Position, 50));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (w.IsReady() && UseW && target.IsValidTarget(w.Range))
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            w.Cast();
                        }
                    }
                    if (q.IsReady() && UseQ && target.IsValidTarget(q.Range))
                    {
                        if (target != null)
                        {
                            if (Player.HasBuff("katarinarsound")) return;
                            q.CastOnUnit(target);
                        }
                    }
                    if (r.IsReady() && rMode.Index != 2)
                    {
                        if (rMode.Index == 0)
                        {
                            if (target.IsValidTarget(r.Range - 150))
                            {
                                if (target != null && target.HealthPercent <= minRHealth.GetValue<MenuSlider>().Value)
                                {
                                    r.Cast();
                                }
                            }
                        }
                        if (rMode.Index == 1)
                        {
                            if (target.IsValidTarget(r.Range - 150))
                            {
                                if (target != null && target.Health <= Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.E) + Daggersdmg(target) + Rdmg(target) * 10)
                                {
                                    if (!q.IsReady())
                                    {
                                        r.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }
        private static void DoLaneclear()
        {

        }
        private static void Active()
        {            
            var autoQ = Menu["Harass"].GetValue<MenuBool>("harassQ");
            var ksQ = Menu["KillSteal"].GetValue<MenuBool>("ksQ");
            var ksE = Menu["KillSteal"].GetValue<MenuBool>("ksE");
            var ksED = Menu["KillSteal"].GetValue<MenuBool>("ksEd");
            var target = TargetSelector.GetTarget(e.Range);

            if (target != null)
            {
                if (!target.IsInvulnerable)
                {
                    // harass Q
                    if (autoQ && q.IsReady() && target.IsValidTarget(q.Range))
                    {
                        q.CastOnUnit(target);
                    }
                    // normal ks E
                    if (e.IsReady() && ksE && target.IsValidTarget(e.Range))
                    {
                        if (Player.GetSpellDamage(target, SpellSlot.E) >= target.Health)
                        {
                            if (Player.HasBuff("katarinarsound"))
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            }
                            e.Cast(target.Position.Extend(Player.Position, -50));
                        }                      
                    }
                    // ks E with daggers
                    if (ksED && e.IsReady() && target.IsValidTarget(e.Range))
                    {
                        foreach (var daggers in GameObjects.AllGameObjects)
                        {
                            if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                            {
                                if (target.Distance(daggers) < 450 && Daggersdmg(target) >= target.Health)
                                {
                                    if (Player.HasBuff("katarinarsound"))
                                    {
                                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                    }
                                    e.Cast(target.Position.Extend(daggers.Position, 200));
                                }
                            }
                        }
                    }
                    // ks with Q
                    if (ksQ && q.IsReady() && target.IsValidTarget(q.Range))
                    {
                        if (Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health)
                        {
                            if (Player.HasBuff("katarinarsound"))
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                            }
                            q.CastOnUnit(target);
                        }
                    }
                }
            }
        }
        private static void OnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("Qd");
            var drawE = Menu["Drawing"].GetValue<MenuBool>("Ed");
            var drawD = Menu["Drawing"].GetValue<MenuBool>("Dd");
            var drawR = Menu["Drawing"].GetValue<MenuBool>("Rd");
            var p = Player.Position;

            if (drawQ && q.IsReady())
            {
                Render.Circle.DrawCircle(p, q.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawE && e.IsReady())
            {
                Render.Circle.DrawCircle(p, e.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawR && r.IsReady())
            {
                Render.Circle.DrawCircle(p, r.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawD)
            {
                foreach (var daggers in GameObjects.AllGameObjects)
                {
                    if (daggers.Name == "HiddenMinion" && daggers.IsValid && !daggers.IsDead && Player.IsVisibleOnScreen)
                    {
                        Render.Circle.DrawCircle(daggers.Position, 450, System.Drawing.Color.DarkCyan);
                        Render.Circle.DrawCircle(daggers.Position, 150, System.Drawing.Color.DarkCyan);
                    }
                }
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
        private static readonly IEnumerable<AIBaseClient> Daggers = ObjectManager.Get<AIBaseClient>().Where(x => x.Name == "HiddenMinion" && x.IsValid && !x.IsDead);
        public static double Daggersdmg(AIBaseClient target)
        {
            double dmg = 0;
            double returnDmg = 0;
            foreach (var daggers in GameObjects.AllGameObjects)
            {
                if (daggers.Name == "HiddenMinion" && !daggers.IsDead && daggers.IsValid)
                {
                    if (daggers.Distance(target) < 450)
                    {
                        if (Player.Level >= 1 && Player.Level < 6)
                        {
                            dmg = 0.55;
                        }
                        if (Player.Level >= 6 && Player.Level < 11)
                        {
                            dmg = 0.7;
                        }
                        if (Player.Level >= 11 && Player.Level < 16)
                        {
                            dmg = 0.85;
                        }
                        if (Player.Level >= 16)
                        {
                            dmg = 1;
                        }
                        var damage = Player.CalculateDamage(target, DamageType.Magical, ((Player.TotalMagicalDamage * dmg) + (35 + (Player.Level * 12)) + (Player.TotalAttackDamage - Player.BaseAttackDamage)));
                        returnDmg = damage;
                    }

                }
            }
            return returnDmg;
        }
        private static double Rdmg(AIBaseClient target)
        {
            double dmg = 0;
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 1)
            {
                dmg = 25;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 2)
            {
                dmg = 37.5;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.R).Level == 3)
            {
                dmg = 50;
            }
            var damage = Player.CalculateDamage(target, DamageType.Magical, ((Player.TotalMagicalDamage * 0.19) + ((Player.TotalAttackDamage - Player.BaseAttackDamage) * 0.22) + dmg));
            return damage;

        }
    }
}
