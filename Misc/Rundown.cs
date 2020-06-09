using EnsoulSharp;
using System;
using System.Linq;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using SharpDX;

namespace NoobAIO.Misc
{
    class Rundown
    {
        private static Vector3 RedTeam = new Vector3(604f, 612f, 183.5748f);
        private static Vector3 BlueTeam = new Vector3(14102f, 14194f, 171.9777f);
        private static Vector3 runitdown;
        private static AIHeroClient Player { get { return ObjectManager.Player; } }
        public static Menu Menu;
        #region Menu
        private static void CreateMenu()
        {
            Menu = new Menu("rundownTab", "Run it down?", true)
            {
                new MenuBool("rundown", "L9").SetValue(false)
            };
            Menu.Attach();
        }
        #endregion Menu


        public Rundown()
        {
            CreateMenu();
            Game.OnUpdate += GameOnUpdate;
            Inting();
                   
        }
        private static void Inting()
        {
            if (Player.Position.Distance(RedTeam) < Player.Position.Distance(BlueTeam))
            {
                runitdown = BlueTeam;
            }
            else
            {
                runitdown = RedTeam;
            }
        }

        private static void GameOnUpdate(EventArgs args)
        {
            if (Menu.GetValue<MenuBool>("rundown"))
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, runitdown);
            }
        }

    }

}