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
    class Twisted_Fate
    {
        public static int PickTick = 0;
        private static Menu Menu;
        private static Spell q, w, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        #region Menu
        private static void CreateMenu()
        {
            Menu = new Menu("TwistedFate", "Noob Twisted Fate", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.Add(new MenuBool("comboQ", "Use Q"));
            comboMenu.Add(new MenuBool("comboQStun", "Only use Q when stunned"));
            comboMenu.Add(new MenuSeparator("Head1", "W Usage"));
            comboMenu.Add(new MenuBool("comboW", "Use W"));
            comboMenu.Add(new MenuSlider("comboWBlue", "Use Bluecard if Mana is under X %", 35, 0, 100));
            Menu.Add(comboMenu);

            // Pick a card
            var pickacardMenu = new Menu("Cardpick", "Pick a card")
            {
                new MenuKeyBind("SelectBlue", "Blue Card", System.Windows.Forms.Keys.E, KeyBindType.Press),
                new MenuKeyBind("SelectRed", "Red Card", System.Windows.Forms.Keys.T, KeyBindType.Press),
                new MenuKeyBind("SelectYellow", "Gold Card", System.Windows.Forms.Keys.W, KeyBindType.Press)
            };
            Menu.Add(pickacardMenu);

            // lane clear
            var laneclearMenu = new Menu("Clear", "Farming")
            {
                new MenuSeparator("Head1", "Lane Clear"),
                new MenuBool("laneclearQ", "Use Q"),
                new MenuBool("laneclearW", "Use W"),
                new MenuSeparator("Head2", "Jungle Clear"),
                new MenuBool("jungleclearQ", "Use Q"),
                new MenuBool("jungleclearW", "Use W"),
            };
            Menu.Add(laneclearMenu);

            // kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksQ", "Use Q"),
            };
            Menu.Add(killstealMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw")
            {
                new MenuBool("Qd", "Draw Q"),
                new MenuBool("Rd", "Draw R")
            };
            Menu.Add(drawMenu);
            Menu.Attach();
        }
        #endregion
        public Twisted_Fate()
        {
            q = new Spell(SpellSlot.Q, 1450);
            q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.Line);
            w = new Spell(SpellSlot.W, Player.GetRealAutoAttackRange());
            r = new Spell(SpellSlot.R, 5500);

            CreateMenu();
            Game.OnUpdate += GameOnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
            {
                TFCardSelector.CardSelector.StartSelecting(Cards.Yellow);
            }
        }
        private static void OnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("Qd");
            var drawR = Menu["Drawing"].GetValue<MenuBool>("Rd");
            var p = Player.Position;

            if (drawQ && q.IsReady())
            {
                Render.Circle.DrawCircle(p, q.Range, System.Drawing.Color.Aqua);
            }
            if (drawR && r.IsReady())
            {
                Render.Circle.DrawCircle(p, r.Range, System.Drawing.Color.Aqua);
            }
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

            Active();

            //Select cards.
            if (Menu["Cardpick"].GetValue<MenuKeyBind>("SelectYellow").Active)
            {
                SelectACard(Cards.Yellow);
            }

            if (Menu["Cardpick"].GetValue<MenuKeyBind>("SelectBlue").Active)
            {
                SelectACard(Cards.Blue);
            }

            if (Menu["Cardpick"].GetValue<MenuKeyBind>("SelectRed").Active)
            {
                SelectACard(Cards.Red);
            }

        }
        static void SelectACard(Cards aCard)
        {

            TFCardSelector.CardSelector.StartSelecting(aCard);
        }
        public static void Active()
        {
            if (Player.IsDead) return;

            var target = TargetSelector.GetTarget(1450);
            var qdmg = Player.GetSpellDamage(target, SpellSlot.Q);

            if (target != null)
            {
                if (!target.IsInvulnerable)
                {
                    var UseQStun = Menu["Combo"].GetValue<MenuBool>("comboQStun");
                    var killsteal = Menu["KillSteal"].GetValue<MenuBool>("ksQ");
                    if (killsteal)
                    {
                        if (q.IsReady() && q.IsInRange(target))
                        {
                            var Qpredicticon = q.GetPrediction(target);

                            if (qdmg > target.Health && Qpredicticon.Hitchance >= HitChance.High)
                            {
                                q.Cast(Qpredicticon.CastPosition);
                            }
                        }
                    }
                    // Auto Q on CC
                    if (q.IsReady() && q.IsInRange(target) && UseQStun)
                    {
                        if (target.HasBuffOfType(BuffType.Stun) || 
                            target.HasBuffOfType(BuffType.Snare) || 
                            target.HasBuffOfType(BuffType.Knockup) ||
                            target.HasBuffOfType(BuffType.Suppression) || 
                            target.HasBuffOfType(BuffType.Charm) || 
                            target.IsRecalling())
                        {
                            q.Cast(target);
                        }
                    }
                }
            }
        }
        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(q.Range);
            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var UseQStun = Menu["Combo"].GetValue<MenuBool>("comboQStun");
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var UseWBlue = Menu["Combo"].GetValue<MenuSlider>("comboWBlue");
            if (Player.IsDead)
            {
                return;
            }

            if (target != null)
            {
                if (UseW && target.IsValidTarget(700) && w.IsReady())
                {
                    if (Player.ManaPercent < UseWBlue)
                    {
                        TFCardSelector.CardSelector.StartSelecting(Cards.Blue);
                    }
                    else
                    {
                        TFCardSelector.CardSelector.StartSelecting(Cards.Yellow);
                    }
                }
                if (UseQ && q.IsReady() && q.IsInRange(target))
                {
                    if (UseQStun)
                    {
                        if (target.HasBuffOfType(BuffType.Stun) ||
                                target.HasBuffOfType(BuffType.Snare) ||
                                target.HasBuffOfType(BuffType.Knockup) ||
                                target.HasBuffOfType(BuffType.Suppression) ||
                                target.IsRecalling())
                        {
                            q.Cast(target);
                        }
                    }
                    else
                    {
                        var Qprediction = q.GetPrediction(target);

                        if (Qprediction.Hitchance >= HitChance.High)
                        {
                            q.Cast(Qprediction.CastPosition);
                        }
                    }
                }
            }
        }
        private static void DoLaneclear()
        {
            var LaneclearQ = Menu["Clear"].GetValue<MenuBool>("laneclearQ");
            var LaneclearW = Menu["Clear"].GetValue<MenuBool>("laneclearW");
            var JungleclearQ = Menu["Clear"].GetValue<MenuBool>("jungleclearQ");
            var JungleclearW = Menu["Clear"].GetValue<MenuBool>("jungleclearW");

            var allJgl = GameObjects.GetJungles(ObjectManager.Player.Position, q.Range, JungleType.All);
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, q.Range, MinionTypes.All);

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
            }
        }
    }
}
