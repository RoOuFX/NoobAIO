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
    class Master_Yi
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        private static void CreateMenu()
        {
            Menu = new Menu("Master Yi", "Noob Master Yi", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuBool("comboQ", "Use Q to gap"),
                new MenuBool("comboW", "Use W to reset AA", false),
                new MenuBool("comboE", "Use E"),
                new MenuBool("comboR", "Use R"),
                new MenuSlider("MinR", "Min Enemies to use R", 2, 1, 5)
            };
            Menu.Add(comboMenu);

            // Lane clear
            var laneclearMenu = new Menu("Clear", "Farming")
            {
                new MenuSeparator("Head2", "Clear settings"),
                new MenuBool("jungleclearW", "Use W to reset AA", false),
                new MenuBool("jungleclearE", "Use E")
            };
            Menu.Add(laneclearMenu);

            // Kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksQ", "Use Q")
            };
            Menu.Add(killstealMenu);

            // Misc
            var miscMenu = new Menu("Misc", "Misc")
            {
                new MenuBool("Qss", "Use Qss on CC"),
                new MenuBool("Miscstuff", "Gapcloser")
            };
            Menu.Add(miscMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw")
            {
                new MenuBool("Qd", "Draw Q")
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }

        public Master_Yi()
        {
            q = new Spell(SpellSlot.Q, 600f);
            w = new Spell(SpellSlot.W);
            e = new Spell(SpellSlot.E, Player.GetRealAutoAttackRange());           
            r = new Spell(SpellSlot.R);

            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnBuffLose += OnBuffLose;
            AIBaseClient.OnBuffGain += OnBuffGain;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Orbwalker.OnAction += OnOrbwalkerAction;
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
            // KS
            Active();
        }
        private static void DoCombo()
        {
            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var UseE = Menu["Combo"].GetValue<MenuBool>("comboE");
            var UseR = Menu["Combo"].GetValue<MenuBool>("comboR");
            var MinR = Menu["Combo"].GetValue<MenuSlider>("MinR");
            var target = TargetSelector.GetTarget(q.Range);
            if (Player.IsDead) return;
            if (target != null)
            {
                if (target.IsValid)
                {
                    if (q.IsReady() && UseQ && target.IsValidTarget(q.Range))
                    {
                        if (target.IsDashing())
                        {
                            q.Cast(target);
                        }                       
                    }
                    if (e.IsReady() && UseE && target.IsValidTarget(e.Range))
                    {
                        e.Cast();
                    }
                    if (r.IsReady() && UseR && target.IsValidTarget(1000))
                    {
                        if (Player.Position.CountEnemyHeroesInRange(1000) >= MinR)
                        {
                            r.Cast();
                        }
                    }
                }
            }          
        }
        private static void DoLaneclear()
        {
            var jglE = Menu["Clear"].GetValue<MenuBool>("jungleclearE");
            var allJgl = GameObjects.GetJungles(ObjectManager.Player.Position, q.Range, JungleType.All);

            foreach (var jgl in allJgl)
            {
                if (e.IsReady() && jgl.IsValidTarget() && e.IsInRange(jgl) && jglE)
                {
                    e.Cast(jgl);
                }
            }
        }
        private static void Active()
        {        
            var target = TargetSelector.GetTarget(q.Range);
            var ksQ = Menu["KillSteal"].GetValue<MenuBool>("ksQ");
            var qdmg = Player.GetSpellDamage(target, SpellSlot.Q);

            if (Player.IsDead) return;
            if (target != null)
            {
                if (!target.IsInvulnerable)
                {
                    if (q.IsReady() && ksQ)
                    {
                        if (qdmg > target.Health)
                        {
                            q.Cast(target);
                        }
                    }
                }
            }
        }
        private static void OnOrbwalkerAction(object obj, OrbwalkerActionArgs args)
        {
            var jglW = Menu["Clear"].GetValue<MenuBool>("jungleclearW");
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var mobs = GameObjects.Jungle;

            if (args.Type == OrbwalkerType.AfterAttack)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (w.IsReady() && UseW && args.Target != null && args.Target.Type == GameObjectType.AIHeroClient)
                    {
                        w.Cast();
                        Orbwalker.ResetAutoAttackTimer();
                    }
                }
                else if(Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    if (mobs.Count() >= 1)
                    {
                        foreach (var mob in mobs)
                        {
                            if (!mob.IsValid) { return; }
                            if (jglW && w.IsReady())
                            {
                                w.Cast();
                                Orbwalker.ResetAutoAttackTimer();
                            }
                        }
                        return;
                    }
                }
            }
        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "Meditate")
            {
                Orbwalker.ResetAutoAttackTimer();
                //Orbwalker.AttackState = false;
                //Orbwalker.MovementState = false;
            }           
        }
        private static void OnBuffLose(AIBaseClient sender, AIBaseClientBuffLoseEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Buff.Name == "Meditate")
            {
                Orbwalker.ResetAutoAttackTimer();
                //Orbwalker.AttackState = true;
                //Orbwalker.MovementState = true;
            }
        }
        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Buff.Type == BuffType.Taunt ||
                args.Buff.Type == BuffType.Stun ||
                args.Buff.Type == BuffType.Snare ||
                args.Buff.Type == BuffType.Polymorph ||
                args.Buff.Type == BuffType.Blind ||
                args.Buff.Type == BuffType.Flee ||
                args.Buff.Type == BuffType.Charm ||
                args.Buff.Type == BuffType.Suppression ||
                args.Buff.Type == BuffType.Silence)
            {
                DoQss();
            }
        }
        private static void DoQss()
        {
            var useQss = Menu["Misc"].GetValue<MenuBool>("Qss");
            if (useQss)
            {
                if (Player.CanUseItem((int)ItemId.Quicksilver_Sash))
                {
                    Player.UseItem((int)ItemId.Quicksilver_Sash);
                }
                if (Player.CanUseItem((int)ItemId.Mercurial_Scimitar))
                {
                    Player.UseItem((int)ItemId.Mercurial_Scimitar);
                }
            }
        }
        private static void OnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("Qd");
            var p = Player.Position;

            if (drawQ && q.IsReady())
            {
                Render.Circle.DrawCircle(p, q.Range, System.Drawing.Color.DarkCyan);
            }
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (!Menu["Misc"].GetValue<MenuBool>("Miscstuff"))
            {
                return;
            }
            if (q.IsReady() && sender != null && sender.IsValidTarget(q.Range))
            {
                if (sender.IsMelee)
                {
                    if (sender.IsValidTarget(sender.AttackRange + sender.BoundingRadius + 100))
                    {
                        q.Cast();
                    }
                }

                if (sender.IsDashing())
                {
                    if (args.EndPosition.DistanceToPlayer() <= 250 ||
                        sender.PreviousPosition.DistanceToPlayer() <= 300)
                    {
                        q.Cast();
                    }
                }

                if (sender.IsCastingImporantSpell())
                {
                    if (sender.PreviousPosition.DistanceToPlayer() <= 300)
                    {
                        q.Cast();
                    }
                }
            }
        }       
    }
}
