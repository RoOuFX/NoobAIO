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
using NoobAIO.Champions;

namespace NoobAIO.Misc
{
    public enum Cards
        {
            Red,
            Yellow,
            Blue,
            None,
        }
        public enum SelectStatus
        {
            Ready,
            Selecting,
            Selected,
            Cooldown,
        }
    class TFCardSelector
    {
        public static class CardSelector
        {
            public static Cards SelectedCard;
            public static int LastW;
            public static SelectStatus Status { get; set; }

            static CardSelector()
            {
                Game.OnUpdate += GameOnGameUpdate;
                AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            }
            public static void SelectCard(AIBaseClient t, Cards selectedCard)
            {
                if (t == null)
                {
                    return;
                }

                if (selectedCard == Cards.Red)
                {
                    StartSelecting(Cards.Red);
                }
                else if (selectedCard == Cards.Yellow)
                {
                    StartSelecting(Cards.Yellow);
                }
                else if (selectedCard == Cards.Blue)
                {
                    StartSelecting(Cards.Blue);
                }
            }
            public static void StartSelecting(Cards card)
            {
                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "PickACard" && Status == SelectStatus.Ready)
                {
                    SelectedCard = card;
                    if (Environment.TickCount - LastW > 170 + Game.Ping / 2)
                    {
                        ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player);
                        LastW = Environment.TickCount;
                    }
                }
            }
            private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (!sender.IsMe)
                {
                    return;
                }

                if (args.SData.Name == "PickACard")
                {
                    Status = SelectStatus.Selecting;
                }

                if (args.SData.Name.ToLower() == "goldcardlock" || 
                    args.SData.Name.ToLower() == "bluecardlock" ||
                    args.SData.Name.ToLower() == "redcardlock")
                {
                    Status = SelectStatus.Selected;
                    SelectedCard = Cards.None;
                }
            }
            private static void GameOnGameUpdate(EventArgs args)
            {
                var wName = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name;
                var wState = ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.W);

                if ((wState == SpellState.Ready &&
                     wName == "PickACard" &&
                     (Status != SelectStatus.Selecting || Environment.TickCount - LastW > 500)) ||
                    ObjectManager.Player.IsDead)
                {
                    Status = SelectStatus.Ready;
                }
                else if (wState == SpellState.Cooldown &&
                         wName == "PickACard")
                {
                    SelectedCard = Cards.None;
                    Status = SelectStatus.Cooldown;
                }
                else if (wState == SpellState.Disabled &&
                         !ObjectManager.Player.IsDead)
                {
                    Status = SelectStatus.Selected;
                }

                if (SelectedCard == Cards.Blue && wName.ToLower() == "bluecardlock" && Environment.TickCount > LastW)
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
                else if (SelectedCard == Cards.Yellow && wName.ToLower() == "goldcardlock" && Environment.TickCount > LastW)
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
                else if (SelectedCard == Cards.Red && wName.ToLower() == "redcardlock" && Environment.TickCount > LastW)
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, false);
                }
            }
        }
    }
}
