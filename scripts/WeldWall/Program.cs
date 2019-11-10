using Sandbox.ModAPI.Ingame;
using SharedProject;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program : MyGridProgram {
        // [general]
        readonly string _calculatorAssemblerName = "WeldWallCalculatorAssembler";
        IMyAssembler _calculatorAssembler;

        readonly string _lcdBlueprintInfoName = "WeldWallBlueprintInfoLcd";
        IMyTextSurface _lcdBlueprintInfo;

        readonly string _lcdComponentStatusName = "WeldWallComponentStatus";
        IMyTextSurface _lcdComponentStatus;

        readonly string _pistonGroupName = "WeldWallPistons";
        List<IMyExtendedPistonBase> _pistons;

        readonly string _productionAssemblerName = "WeldWallProductionAssembler";
        IMyAssembler _productionAssembler;

        readonly string _projectorName = "WeldWallProjector";
        IMyProjector _projector;

        readonly double _retractVelocity = 2;

        readonly string _weldEndedTimerName = "WeldWallEndTimer";
        IMyTimerBlock _weldEndedTimer;

        readonly string _weldReadyTimerName = "WeldWallReadyTimer";
        IMyTimerBlock _weldReadyTimer;

        readonly double _weldVelocity = 0.5;

        readonly string _welderGroupName = "WeldWallWelders";
        List<IMyShipWelder> _welders;

        readonly MyIni _ini = new MyIni();

        Dictionary<string, BlueprintInfo> _blueprints;

        string _state = "CHECK_BLUEPRINT";

        public Program() {
            Main("PARSE");
        }

        public void Main(string argument) {
            if (argument == "PARSE") {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result)) {
                    Echo($"CustomData parsing error: \nLine {result.LineNo}");
                    return;
                }

                var calculatorAssemblerName = _ini.Get("general", "CalculatorAssembler").ToString(_calculatorAssemblerName);
                var lcdBlueprintInfoName = _ini.Get("general", "LcdBlueprintInfo").ToString(_lcdBlueprintInfoName);
                var lcdComponentStatusName = _ini.Get("general", "LcdComponentStatus").ToString(_lcdComponentStatusName);
                var pistonGroupName = _ini.Get("general", "PistonGroup").ToString(_pistonGroupName);
                var productionAssemblerName = _ini.Get("general", "ProductionAssembler").ToString(_productionAssemblerName);
                var projectorName = _ini.Get("general", "Projector").ToString(_projectorName);
                var retractVelocity = _ini.Get("general", "RetractVelocity").ToDouble(_retractVelocity);
                var weldEndedTimerName = _ini.Get("general", "WeldEndedTimer").ToString(_weldEndedTimerName);
                var weldReadyTimerName = _ini.Get("general", "WeldReadyTimer").ToString(_weldReadyTimerName);
                var weldVelocity = _ini.Get("general", "WeldVelocity").ToDouble(_weldVelocity);
                var welderGroupName = _ini.Get("general", "WelderGroup").ToString(_welderGroupName);

                var lookup = new LookupHelper {
                    GridTerminalSystem = GridTerminalSystem,
                };

                _calculatorAssembler = lookup.GetBlockWithName<IMyAssembler>(calculatorAssemblerName, true);
                _lcdBlueprintInfo = lookup.GetBlockWithName<IMyTextSurface>(lcdBlueprintInfoName, true);
                _lcdComponentStatus = lookup.GetBlockWithName<IMyTextSurface>(lcdComponentStatusName, true);
                _pistons = lookup.GetBlocksInGroup<IMyExtendedPistonBase>(pistonGroupName);
                _productionAssembler = lookup.GetBlockWithName<IMyAssembler>(productionAssemblerName, true);
                _projector = lookup.GetBlockWithName<IMyProjector>(projectorName, true);
                _weldEndedTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldEndedTimerName);
                _weldReadyTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldReadyTimerName);
                _welders = lookup.GetBlocksInGroup<IMyShipWelder>(welderGroupName);

                _blueprints = new Dictionary<string, BlueprintInfo>();
                var sections = new List<string>();
                _ini.GetSections(sections);
                sections.ForEach(section => {
                    var keys = new List<MyIniKey>();
                    _ini.GetKeys(section, keys);
                    var blocks = new Dictionary<string, int>();

                    keys.ForEach(key => {
                        if (key.Name.StartsWith("MyObject")) {
                            blocks.Add(key.Name, _ini.Get(section, key.Name).ToInt32());
                        }
                    });

                    _blueprints.Add(section, new BlueprintInfo() {
                        Blocks = blocks,
                        Name = _ini.Get(section, "Name").ToString(),
                    });
                });
            }

            var currentBpId = GetProjectorBlueprintId();
            var currentBpName = _blueprints.GetValueOrDefault(currentBpId, null)?.Name ?? "Unknown";
            _lcdBlueprintInfo.WriteText($@"Selected blueprint: {currentBpName}
Current blueprint ID: {currentBpId}");
            Echo($"Blueprint id: {GetProjectorBlueprintId()}");
            Echo($"Known blueprint IDS: {String.Join(", ", _blueprints.Keys)}");

            switch (argument) {
                case "CHECK":
                    if (_state == "CHECK_BLUEPRINT") {
                        Echo($"Blueprint id: {GetProjectorBlueprintId()}");
                    }
                    break;
            }

            /*
            var block = GridTerminalSystem.GetBlockWithName("Projector weld wall") as IMyProjector;
            
            */
        }

        string GetProjectorBlueprintId() {
            var remainingBlocks = new List<string>();
            var enumerator = _projector.RemainingBlocksPerType.GetEnumerator();
            while (enumerator.MoveNext()) {
                var item = enumerator.Current;
                remainingBlocks.Add($"{item.Key}={item.Value}");
            }

            remainingBlocks.Sort();
            var projectionInfo = String.Join(", ", remainingBlocks);

            return MurmurHash2.Hash(projectionInfo).ToString("X");
        }
    }
}
