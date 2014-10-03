using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.Components;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace TeleportMod
{
    class TeleporterManager
    {

        private Dictionary<string, IMyFunctionalBlock> TeleporterList = new Dictionary<string, IMyFunctionalBlock>();

        private static TeleporterManager instance;

        private TeleporterManager() { }

        public static TeleporterManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TeleporterManager();
                }
                return instance;
            }
        }

        public bool AddTeleporter(string name, IMyFunctionalBlock obj)
        {
            if (name == null || name.Equals("") || obj == null)
                return false;
            try
            {
                TeleporterList.Add(name, obj);
            }
            catch (ArgumentException)
            {
                TeleporterList.Remove(name);
                return AddTeleporter(name, obj);
            }

            return true;
        }

        public bool RemoveTeleporter(string name)
        {
            if (!TeleporterList.ContainsKey(name))
                return false;
            TeleporterList.Remove(name);
            return true;
        }

        public IMyFunctionalBlock GetTeleporterByName(string name)
        {
            if (TeleporterList.ContainsKey(name))
            {
                return TeleporterList[name];
            }
            else
            {
                return null;
            }
        }
    }
}
