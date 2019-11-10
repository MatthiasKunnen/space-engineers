using Sandbox.ModAPI.Ingame;
using SharedProject;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {
        string closeConnectorName = "Blast door closed connector"; // Name of connector on the closed side
        string grabberGroupName = "Blast door grabbers";
        string grinderGroupName = "Blast door grinders";
        string namePrefix = ""; // Prefix is used for all name lookups
        string openConnectorName = "Blast door open connector"; // Name of connector on the open side
        string rotorGroupName = "Blast door rotors";
        string welderGroupName = "Blast door welders";

        float grabberVelocity = 6.0f;
        float rotorVelocity = 10.0f;

        string state = null;

        IMyShipConnector closeConnector;
        IMyShipConnector openConnector;

        List<IMyShipGrinder> grinders;
        List<IMyShipWelder> welders;

        RotorController rotorController;
        RotorController grabberController;

        List<string> accessCodes = new List<string>();
        IMyBroadcastListener broadcastListener;

        // States
        // - OPEN
        // - OPENING
        // - CLOSED
        // - CLOSING
        // - UNKNOWN -> stopped in non-closed/open state, or moving to unknown state
        // - null -> default, requires init


        // Inputs
        // - CLOSE
        // - OPEN
        // - OBSERVE -> default, observes states, locks when available and change state accordingly
        // - RESTART -> reruns init

        public Program() {
            var config = new Config(Me.CustomData);
            var blashDoorChannel = config.Get("blastDoorChannel", "BlastDoor");
            broadcastListener = IGC.RegisterBroadcastListener(blashDoorChannel);
            broadcastListener.SetMessageCallback("radioCommand");
        }

        public void Main(string input, UpdateType updateSource) {
            input = input == "" ? "OBSERVE" : input;

            if (input == "RESTART" || state == null) {
                var config = new Config(Me.CustomData);

                config.Set(ref closeConnectorName, "closeConnectorName");
                config.Set(ref grabberGroupName, "grabberGroupName");
                config.Set(ref grabberVelocity, "grabberVelocity");
                config.Set(ref grinderGroupName, "grinderGroupName");
                config.Set(ref namePrefix, "namePrefix");
                config.Set(ref openConnectorName, "openConnectorName");
                config.Set(ref rotorGroupName, "rotorGroupName");
                config.Set(ref rotorVelocity, "rotorVelocity");
                config.Set(ref welderGroupName, "welderGroupName");

                var lookup = new LookupHelper {
                    GridTerminalSystem = GridTerminalSystem,
                    NamePrefix = namePrefix,
                };

                // init
                accessCodes = new List<string>(config.Get("accessCodes", "").Split(','));

                closeConnector = lookup.GetBlockWithName<IMyShipConnector>(closeConnectorName);
                closeConnector.Enabled = true;

                openConnector = lookup.GetBlockWithName<IMyShipConnector>(openConnectorName);
                openConnector.Enabled = true;

                grinders = lookup.GetBlocksInGroup<IMyShipGrinder>(grinderGroupName);
                grinders.ForEach(grinder => grinder.Enabled = false);
                welders = lookup.GetBlocksInGroup<IMyShipWelder>(welderGroupName);
                welders.ForEach(welder => welder.Enabled = false);

                rotorController = new RotorController(lookup.GetBlocksInGroup<IMyMotorStator>(rotorGroupName), rotorVelocity);
                rotorController.Stop();
                grabberController = new RotorController(lookup.GetBlocksInGroup<IMyMotorStator>(grabberGroupName), grabberVelocity);
                grabberController.Go("BACKWARD"); // Open grabber

                state = "UNKNOWN";
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                Observe();
            }

            if (input == "radioCommand" && broadcastListener.HasPendingMessage) {
                var message = broadcastListener.AcceptMessage();

                while (broadcastListener.HasPendingMessage) {
                    message = broadcastListener.AcceptMessage();
                }

                var data = BlastDoorRadioMessage.Deserialize(message.Data as string);

                if (!accessCodes.Contains(data.AccessCode)) {
                    return;
                }

                input = data.Command;
            }

            if (input == "OBSERVE") {
                Observe();
            } else if (input == "CLOSE" && state != "CLOSING" && state != "CLOSED") {
                openConnector.Enabled = false;
                closeConnector.Enabled = true;
                grinders.ForEach(grinder => grinder.Enabled = false);
                welders.ForEach(welder => welder.Enabled = true);
                rotorController.Go("FORWARD");
                grabberController.Go("FORWARD"); // Grab
                state = "CLOSING";
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            } else if (input == "OPEN" && state != "OPENING" && state != "OPEN") {
                openConnector.Enabled = true;
                closeConnector.Enabled = false;
                welders.ForEach(welder => welder.Enabled = false);
                grinders.ForEach(grinder => grinder.Enabled = true);
                rotorController.Go("BACKWARD");
                grabberController.Go("FORWARD"); // Grab
                state = "OPENING";
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }

        void Observe() {
            switch (state) {
                case "CLOSED":
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    rotorController.Stop();
                    grabberController.Go("BACKWARD"); // Open grabber by default
                    welders.ForEach(welder => welder.Enabled = false);
                    grinders.ForEach(grinder => grinder.Enabled = false);

                    break;
                case "OPEN":
                    goto case "CLOSED";
                case "CLOSING":
                    CheckClosed();
                    break;
                case "OPENING":
                    CheckOpen();
                    break;
                case "UNKNOWN":
                    CheckClosed();
                    CheckOpen();
                    break;
            }
        }

        void CheckClosed() {
            closeConnector.Connect();
            if (closeConnector.Status == MyShipConnectorStatus.Connected) {
                state = "CLOSED";
            }
        }

        void CheckOpen() {
            openConnector.Connect();
            if (openConnector.Status == MyShipConnectorStatus.Connected) {
                state = "OPEN";
            }
        }
    }
}
