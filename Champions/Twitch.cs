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
    class Twitch
    {
        private static Menu Menu;
        private static Spell q, w, e;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        public static int mykills = 0 + Player.ChampionsKilled;
        private static void CreateMenu()
        {
            Menu = new Menu("Twitch", "Noob Twitch", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuBool("comboQ", "Use Q after a Kill"),
                new MenuBool("comboW", "Use W"),
                new MenuBool("comboE", "Use E")
            };
            Menu.Add(comboMenu);

            // Lane clear
            var laneclearMenu = new Menu("Clear", "Farming")
            {
                new MenuSeparator("Head1", "Lane Clear"),
                new MenuBool("laneclearW", "Use W"),
                new MenuBool("laneclearE", "Use E"),
                new MenuSeparator("Head2", "Jungle Clear"),
                new MenuBool("jungleclearW", "Use W"),
                new MenuBool("jungleclearE", "Use E")
            };
            Menu.Add(laneclearMenu);

            // Kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksE", "Use E")
            };
            Menu.Add(killstealMenu);

            // Misc
            var miscMenu = new Menu("Misc", "Misc")
            {
                new MenuKeyBind("MiscBack", "Stealth Recall", System.Windows.Forms.Keys.B, KeyBindType.Press)
            };
            Menu.Add(miscMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw")
            {
                new MenuBool("Wd", "Draw W"),
                new MenuBool("Ed", "Draw E")
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }

        public Twitch()
        {
            q = new Spell(SpellSlot.Q);
            w = new Spell(SpellSlot.W, 950f);
            w.SetSkillshot(0.25f, 100f, 1400f, false, false, SkillshotType.Circle);
            e = new Spell(SpellSlot.E, 1200f);

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

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoLaneclear();
                    break;
            }
            Stealthrecall();

            // E Usage

            Active();
        }
        private static void DoCombo()
        {
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var target = TargetSelector.GetTarget(w.Range);
            var wPred = w.GetPrediction(target);
            if (Player.IsDead) return;
            if (target == null) return;

            if (wPred.Hitchance >= HitChance.High && target.Health > GetRealEDamage(target) && GetEStackCount(target) < 6 && UseW)
            {
                w.Cast(wPred.CastPosition);
            }
            if (UseQ && Player.ChampionsKilled > mykills && target != null)
            {
                q.Cast();
            }
        }
        private static void Stealthrecall()
        {
            if (Player.IsDead) return;
            if (Menu["Misc"].GetValue<MenuKeyBind>("MiscBack").Active && q.IsReady())
            {
                q.Cast();
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.Recall);
            }
        }
        private static void DoLaneclear()
        {
            var LaneclearE = Menu["Clear"].GetValue<MenuBool>("laneclearE");
            var LaneclearW = Menu["Clear"].GetValue<MenuBool>("laneclearW");
            var JungleclearE = Menu["Clear"].GetValue<MenuBool>("jungleclearE");
            var JungleclearW = Menu["Clear"].GetValue<MenuBool>("jungleclearW");

            var laneW = GameObjects.GetMinions(ObjectManager.Player.Position, Player.GetRealAutoAttackRange() + 100);
            var laneWJ = GameObjects.GetJungles(ObjectManager.Player.Position, Player.GetRealAutoAttackRange() + 100);
            var Wfarmpos = w.GetLineFarmLocation(laneW, 100);
            var WfarmposJ = w.GetLineFarmLocation(laneWJ, 100);

            var allJgl = GameObjects.GetJungles(ObjectManager.Player.Position, q.Range, JungleType.All);
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, q.Range, MinionTypes.All);

            if (Player.IsDead)
            {
                return;
            }
            if (allJgl == null || allMinions == null)
            {
                return;
            }

            foreach (var minion in allMinions)
            {
                if (e.IsReady() && LaneclearE && minion.IsValidTarget() && e.IsInRange(minion) && minion.Health < GetRealEDamage(minion))
                {
                    e.Cast(minion);
                }
                if (w.IsReady() && LaneclearW && minion.IsValidTarget() && w.IsInRange(minion))
                {
                    if (minion.IsValidTarget(w.Range) && Wfarmpos.MinionsHit >= 3 && laneW.Count >= 3)
                    {
                        w.Cast(minion);
                    }
                }
            }
            foreach (var jgl in allJgl)
            {
                if (e.IsReady() && jgl.IsValidTarget() && e.IsInRange(jgl) && JungleclearE && jgl.Health < GetRealEDamage(jgl))
                {
                    e.Cast(jgl);
                }
                if (w.IsReady() && jgl.IsValidTarget() && w.IsInRange(jgl) && JungleclearW)
                {
                    if (jgl.IsValidTarget(w.Range) && WfarmposJ.MinionsHit >= 3 && laneWJ.Count >= 3)
                    {
                        w.Cast(jgl);
                    }
                }
            }
        }
        private static void Active()
        {
            if (Player.IsDead) return;

            var ksE = Menu["KillSteal"].GetValue<MenuBool>("ksE");
            var UseE = Menu["Combo"].GetValue<MenuBool>("comboE");
            var target = TargetSelector.GetTarget(1450);

            if (target != null)
            {
                if (!target.IsInvulnerable)
                {
                    if (e.IsReady() && ksE && target.IsValidTarget(e.Range))
                    {
                        if (target.Health < GetRealEDamage(target) - target.HPRegenRate)
                        {
                            e.Cast();
                        }
                    }
                    else
                    {
                        if(e.IsReady() && UseE && target.IsValidTarget(e.Range))
                        {
                            if (target.Health < GetRealEDamage(target) - target.HPRegenRate)
                            {
                                e.Cast();
                            }
                        }
                    }
                }
            }


        }
        private static double GetRealEDamage(AIBaseClient target)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.Time > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += e.IsReady() ? GetEDMGTwitch(target) : 0d;

                if (target.CharacterName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (Player.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }
        public static double GetEDMGTwitch(AIBaseClient target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0;
            }

            if (!target.HasBuff("twitchdeadlyvenom"))
            {
                return 0;
            }

            var eLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            if (eLevel <= 0)
            {
                return 0;
            }

            var buffCount = GetEStackCount(target);

            var baseDamage = new[] { 0, 20, 30, 40, 50, 60 }[eLevel];
            var extraDamage = new[] { 0, 15, 20, 25, 30, 35 }[eLevel] + 0.2f * ObjectManager.Player.TotalMagicalDamage +
                              0.35f * (ObjectManager.Player.TotalAttackDamage - ObjectManager.Player.BaseAttackDamage);
            var resultDamage =
                ObjectManager.Player.CalculateDamage(target, DamageType.Physical, baseDamage + extraDamage * buffCount);
            if (ObjectManager.Player.HasBuff("SummonerExhaust"))
            {
                resultDamage *= 0.6f;
            }

            return resultDamage;
        }
        public static int GetEStackCount(AIBaseClient target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget() ||
                target.Type != GameObjectType.AIMinionClient && target.Type != GameObjectType.AIHeroClient)
            {
                return 0;
            }

            return target.GetBuffCount("twitchdeadlyvenom");
        }
        private static void OnDraw(EventArgs args)
        {
            var drawW = Menu["Drawing"].GetValue<MenuBool>("Wd");
            var drawE = Menu["Drawing"].GetValue<MenuBool>("Ed");
            var p = Player.Position;

            if (drawW && w.IsReady())
            {
                Render.Circle.DrawCircle(p, w.Range, System.Drawing.Color.DarkCyan);
            }
            if (drawE)
            {
                Render.Circle.DrawCircle(p, e.Range, System.Drawing.Color.DarkCyan);
            }
        }
    }
}
