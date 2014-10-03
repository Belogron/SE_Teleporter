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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    public class TeleportBeacon : MyGameLogicComponent
    {
        public bool IsTeleporter { get; private set; }

        public string TeleporterName { get; private set; }
        public string TeleporterDestination { get; private set; }

        public const float TELEPORTATION_DISTANCE_TRIGGER = 4.5f;
        public const float TELEPORTATION_DISTANCE_ARRIVAL = 6f;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            IsTeleporter = false;
            TeleporterName = "";
            TeleporterDestination = "";

            //(Entity as IMyTerminalBlock).CustomNameChanged += TeleportBeacon_CustomNameChanged;

        }

        /*public void TeleportBeacon_CustomNameChanged(IMyTerminalBlock obj) //Not or not yet working
        {

            MyAPIGateway.Utilities.ShowNotification("Name changed", 1000, MyFontEnum.Green);

        }*/


        public override void Close()
        {

        }

        public override void MarkForClose()
        {
            if (IsTeleporter)
            {
                TeleporterManager.Instance.RemoveTeleporter(TeleporterName); //Remove the beacon from the teleporter system
            }
        }

        public override void UpdateAfterSimulation() { }
        public override void UpdateAfterSimulation10() { }

        public override void UpdateAfterSimulation100() { }

        public override void UpdateBeforeSimulation() { }

        public override void UpdateBeforeSimulation10() { }

        public override void UpdateBeforeSimulation100()
        {
            //check if all game components are already available
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null || MyAPIGateway.Session.Player.PlayerCharacter == null)
                return;

            //Checking for the name and trying to register the beacon as a teleporter
            TryRegisterTeleporter();

            if (!IsTeleporter)
                return;

            // is the beacon enabled, fucntional(not damaged) and working(has power)
            if (!(Entity as IMyFunctionalBlock).Enabled || !(Entity as IMyFunctionalBlock).IsFunctional || !(Entity as IMyFunctionalBlock).IsWorking) //block disabled
                return;

            
            // is the player in teleportation range of the beacon?
            if ((MyAPIGateway.Session.Player.PlayerCharacter.Entity.GetPosition() - Entity.GetPosition()).Length() < TELEPORTATION_DISTANCE_TRIGGER)
            {

                MyRelationsBetweenPlayerAndBlock rel = (Entity as IMyFunctionalBlock).GetUserRelationToOwner(MyAPIGateway.Session.Player.PlayerId);

                if (rel == MyRelationsBetweenPlayerAndBlock.Enemies)
                { //no teleport for enemies
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Missing access to teleporter \"{0}\"", TeleporterName), 1000, MyFontEnum.Red);
                    return;
                }
                
                //Get the target beacon out of the teleporter system
                IMyFunctionalBlock target = TeleporterManager.Instance.GetTeleporterByName(TeleporterDestination);

                // if the target exists (the given target name is valid)
                if (target != null)
                {
                    // check if target is enabled/functional/working
                    if (target.Enabled && target.IsWorking && target.IsFunctional)
                    {
                        MyRelationsBetweenPlayerAndBlock relTarget = target.GetUserRelationToOwner(MyAPIGateway.Session.Player.PlayerId);
                        if (relTarget == MyRelationsBetweenPlayerAndBlock.Enemies)
                        { //no teleport for enemies
                            MyAPIGateway.Utilities.ShowNotification(string.Format("Missing access to target teleporter \"{0}\"", TeleporterDestination), 1000, MyFontEnum.Red);
                            return;
                        } 

                        //get position of target and move target by arrival distance in front of the beacon
                        //simple vector math :)
                        VRageMath.Vector3 targetPos = target.GetPosition();
                        targetPos += (target.WorldMatrix.Forward * TELEPORTATION_DISTANCE_ARRIVAL);

                        MyAPIGateway.Session.Player.PlayerCharacter.Entity.WorldMatrix = target.WorldMatrix; //first set the player orientation
                        MyAPIGateway.Session.Player.PlayerCharacter.Entity.SetPosition(targetPos); //and now finally the position


                        MyAPIGateway.Utilities.ShowNotification(string.Format("Teleported you from \"{0}\" to \"{1}\". ", TeleporterName, TeleporterDestination), 1000, MyFontEnum.Blue);
                    }
                    else 
                    {
                        MyAPIGateway.Utilities.ShowNotification(string.Format("Could not teleport you to \"{0}\" because it is not functional.", TeleporterDestination), 1000, MyFontEnum.Red);
                    }
                    
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Could not teleport you to \"{0}\" because it does not exist.", TeleporterDestination), 1000, MyFontEnum.Red);
                }
            }

        }

        public override void UpdateOnceBeforeFrame() { }

        public void TryRegisterTeleporter()
        {
            if ((Entity as IMyTerminalBlock).CustomName.StartsWith("TP(") && (Entity as IMyTerminalBlock).CustomName.EndsWith(")")) //does the block have teleporter syntax name
            {
                

                string[] separators = { " : ", ":", "(", ")" };
                string[] parameters = (Entity as IMyTerminalBlock).CustomName.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                if (parameters.Length < 3)
                    return;
                if (parameters[1] == null || parameters[1].Equals("") || parameters[2] == null || parameters[2].Equals(""))
                    return;
                if (TeleporterName.Equals(parameters[1])) //if name did not change, return
                {
                    return;
                }
                HandlePossibleNameChange(parameters);

                //if(IsNameInUse(parameters[1]))
                  //  return;

                //Everything fine, start creating teleporter

                //setting own properties
                TeleporterName = parameters[1];
                TeleporterDestination = parameters[2];


                if (TeleporterManager.Instance.AddTeleporter(TeleporterName, Entity as IMyFunctionalBlock))
                {
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Succesfully created teleporter \"{0}\"", TeleporterName), 1000, MyFontEnum.Green);
                    IsTeleporter = true;
                }
                else 
                {
                    MyAPIGateway.Utilities.ShowNotification(string.Format("Creation of teleporter \"{0}\" failed", TeleporterName), 1000, MyFontEnum.Red);
                }

            }
            else if (IsTeleporter)
            {
                // remove tepelorter if the name differs teleporter name syntax
                TeleporterManager.Instance.RemoveTeleporter(TeleporterName);
                IsTeleporter = false;
            }
        }

        private void HandlePossibleNameChange(string[] parameters)
        {
            if (IsTeleporter && !parameters[1].Equals(TeleporterName)) //if it currently is a teleporter and the name did change
            {
                TeleporterManager.Instance.RemoveTeleporter(TeleporterName); // delete old one, so it can be added again soon
                MyAPIGateway.Utilities.ShowNotification("Renamed Teleporter");
            }
        }

        private bool IsNameInUse(string name) {
            if (TeleporterManager.Instance.GetTeleporterByName(name) != null)
            { //does this name already exist in the system
                //then abort
                MyAPIGateway.Utilities.ShowNotification(string.Format("Another teleporter already has the name \"{0}\"", name), 1000, MyFontEnum.Red);
                IsTeleporter = false;
                return true;
            }
            return false;
        }
    }
}
