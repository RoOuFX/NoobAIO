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


namespace NoobAIO.Champions
{
    class Jax
    {
        private static Menu Menu;
        private static Spell q, w, e, r;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        public bool Wj;
        public static bool Eactive
        {
            get { return Player.HasBuff("JaxCounterStrike"); }
        }
        #region Menu
        private static void CreateMenu()
        {
            Menu = new Menu("Jax", "Noob Jax", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuBool("comboQ", "Use Q"),
                new MenuBool("comboW", "Use W"),
                new MenuBool("comboE", "Use E"),
                new MenuBool("comboR", "Use R"),
                new MenuSlider("MinR", "Min Enemies to use R", 2, 1, 5)
            };
            Menu.Add(comboMenu);

            // lane clear
            var laneclearMenu = new Menu("clear", "Farming")
            {
                new MenuSeparator("Head1", "Lane Clear"),
                new MenuBool("laneclearW", "Use W"),
                new MenuSeparator("Head2", "Jungle Clear"),
                new MenuBool("jungleclearQ", "Use Q"),
                new MenuBool("jungleclearW", "Use W"),
                new MenuBool("jungleclearE", "Use E"),
            };
            Menu.Add(laneclearMenu);

            // kill steal
            var killstealMenu = new Menu("KillSteal", "Kill Steal")
            {
                new MenuBool("ksQ", "Use Q"),
                new MenuBool("ksW", "^ with W")
            };
            Menu.Add(killstealMenu);

            // Misc
            var miscMenu = new Menu("Misc", "Misc")
            {
                new MenuKeyBind("jumpkey", "Wardjump Key", System.Windows.Forms.Keys.Z, KeyBindType.Press)
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
        #endregion Menu
        
        public Jax()
        {
            q = new Spell(SpellSlot.Q, 680f);
            w = new Spell(SpellSlot.W, Player.GetRealAutoAttackRange());
            e = new Spell(SpellSlot.E);
            r = new Spell(SpellSlot.R);
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

            Killsteal();

            if (Menu["Misc"].GetValue<MenuKeyBind>("jumpkey").Active)
            {
                WardJump();
            }
        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {

            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "JaxEmpowerTwo")
                Orbwalker.ResetAutoAttackTimer();
        }
        private static void OnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("Qd");
            var p = Player.Position;

            if (Player.IsDead || Player.IsRecalling())
                return;

            if (drawQ && q.IsReady())
            {
                Render.Circle.DrawCircle(p, q.Range, System.Drawing.Color.Aqua);
            }
        }
        private static void Killsteal()
        {
            var target = TargetSelector.GetTarget(q.Range);
            var qdmg = Player.GetSpellDamage(target, SpellSlot.Q);
            var wdmg = Player.GetSpellDamage(target, SpellSlot.W);
            var ksq = Menu["KillSteal"].GetValue<MenuBool>("ksQ");
            var ksw = Menu["KillSteal"].GetValue<MenuBool>("ksW");
            if (!target.IsValidTarget())
            {
                return;
            }
            if (q.IsInRange(target) && q.IsReady() && qdmg > target.Health + target.AllShield && ksq)
            {
                q.CastOnUnit(target);
            }
            if (q.IsInRange(target) && w.IsReady() && qdmg + wdmg > target.Health + target.AllShield && ksq && ksw)
            {
                w.Cast();
                q.CastOnUnit(target);
            }
        }

        private static void DoCombo()
        {
            var targetQ = TargetSelector.GetTarget(q.Range);
            var targetW = TargetSelector.GetTarget(Player.GetRealAutoAttackRange());

            var UseQ = Menu["Combo"].GetValue<MenuBool>("comboQ");
            var UseE = Menu["Combo"].GetValue<MenuBool>("comboE");
            var UseW = Menu["Combo"].GetValue<MenuBool>("comboW");
            var UseR = Menu["Combo"].GetValue<MenuBool>("comboR");
            var MinR = Menu["Combo"].GetValue<MenuSlider>("MinR");

            if (!targetQ.IsValidTarget())
            {
                return;
            }
            // Q usage
            if (UseQ && q.IsReady() && targetQ.IsValidTarget() && 
              (targetQ.Distance(Player) > Player.GetRealAutoAttackRange() || Player.HealthPercent <= 25))
            {
                if(Eactive)
                {
                    q.CastOnUnit(targetQ);
                }
                else
                {
                    if (!e.IsReady())
                    {
                        q.CastOnUnit(targetQ);
                    }
                }
            }
            // E usage
            if (UseE && e.IsReady())
            {
                if (!Eactive && q.IsReady() && targetQ.IsValidTarget() && !targetQ.IsValidTarget(e.Range))
                {
                    e.Cast();
                }

                if (!Eactive && targetQ.IsValidTarget(e.Range))
                {
                    e.Cast();
                }

                if (Eactive && targetQ.IsValidTarget(e.Range))
                {
                    e.Cast();
                }
            }
            // W usage
            if (UseW && w.IsReady() && w.IsInRange(targetW))
            {
                w.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, targetW);
            }

            if (r.IsReady() && UseR)
            {
                if (Player.Position.CountEnemyHeroesInRange(q.Range) >= MinR)
                {
                    r.Cast();
                }
            }

        }
        private static void DoLaneclear()
        {
            var LaneclearW = Menu["clear"].GetValue<MenuBool>("laneclearW");
            var JungleclearQ = Menu["clear"].GetValue<MenuBool>("jungleclearQ");
            var JungleclearW = Menu["clear"].GetValue<MenuBool>("jungleclearW");
            var JungleclearE = Menu["clear"].GetValue<MenuBool>("jungleclearE");
            var allJgl = GameObjects.GetJungles(ObjectManager.Player.Position, q.Range, JungleType.All);
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, q.Range, MinionTypes.All);
            if (LaneclearW.Enabled)
            {
                foreach (var minion in allMinions)
                {
                    var totalWDamage = w.GetDamage(minion); if (w.IsReady() &&
                        minion.IsValidTarget(w.Range + Player.BoundingRadius + minion.BoundingRadius) &&
                        w.Range + Player.BoundingRadius + minion.BoundingRadius > Player.Distance(minion.Position) &&
                        minion.Health < totalWDamage + Player.TotalAttackDamage)
                    {
                        w.Cast();
                    }
                }
            }
            foreach (var jgl in allJgl)
            {
                if (q.IsReady() && jgl.IsValidTarget() && q.IsInRange(jgl) && JungleclearQ)
                {
                    q.Cast(jgl);
                }
                if (w.IsReady() && jgl.IsValidTarget(w.Range + Player.BoundingRadius + jgl.BoundingRadius) && 
                    w.Range + Player.BoundingRadius + jgl.BoundingRadius > Player.Distance(jgl.Position) && JungleclearW)
                {
                    w.CastOnUnit(jgl);
                }
                if (e.IsReady() && jgl.IsValidTarget() && e.IsInRange(jgl) && !Eactive && JungleclearE)
                {
                    e.Cast(jgl);
                }
            }
        }
        private void WardJump()
        {
            Orbwalker.Move(Game.CursorPos);
            if (!q.IsReady())
            {
                return;
            }
            var wardSlot = Player.GetWardSlot();
            var pos = Game.CursorPos;
            if (pos.Distance(Player.Position) > 600)
            {
                pos = Player.Position.Extend(pos, 600);
            }

            var jumpObj = GetJumpObj(pos);
            if (jumpObj != null)
            {
                q.CastOnUnit(jumpObj);
            }
            else
            {
                if (wardSlot != null && Player.CanUseItem(wardSlot.ItemID) &&
                    (Player.Spellbook.CanUseSpell(wardSlot.SpellSlot) == SpellState.Ready || wardSlot.CountInSlot != 0) &&
                    !Wj)
                {
                    Wj = true;
                    Utility.DelayAction.Add(new Random().Next(1000, 1500), () => { Wj = false; });
                    Player.Spellbook.CastSpell(wardSlot.SpellSlot, pos);
                    Utility.DelayAction.Add(
                        150, () =>
                        {
                            var predWard = GetJumpObj(pos);
                            if (predWard != null && q.IsReady())
                            {
                                q.CastOnUnit(predWard);
                            }
                        });
                }
            }
        }
        public AIBaseClient GetJumpObj(Vector3 pos)
        {
            return
                ObjectManager.Get<AIBaseClient>()
                    .Where(
                        obj =>
                            obj.IsValidTarget(600, false) && pos.Distance(obj.Position) <= 100 &&
                            (obj is AIMinionClient || obj is AIHeroClient))
                    .OrderBy(obj => obj.Distance(pos))
                    .FirstOrDefault();
        }
    }
}