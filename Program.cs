using EnsoulSharp;
using EnsoulSharp.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoobAIO.Champions;
using NoobAIO.Misc;

namespace NoobAIO
{
    class Program
    {
        private static AIHeroClient Player => ObjectManager.Player;
        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoadGame;
        }
        private static void OnLoadGame()
        {
            switch (Player.CharacterName)
            {
                case "Jax":
                    new Jax();
                    break;
                case "TwistedFate":
                    new Twisted_Fate();
                    break;
                case "Shyvana":
                    new Shyvana();
                    break;
            }
            new Rundown();

        }
    }
}