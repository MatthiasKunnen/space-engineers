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

        BlueprintInfo _previousBlueprint = null;

        string _previousBlueprintId = null;

        string _state = "CHECK_BLUEPRINT";

        int i = 1;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Main("PARSE");
        }

        public void Main(string argument = "CHECK") {
            Echo((i++).ToString());
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
                _pistons = lookup.GetBlocksInGroup<IMyExtendedPistonBase>(pistonGroupName, true);
                _productionAssembler = lookup.GetBlockWithName<IMyAssembler>(productionAssemblerName, true);
                _projector = lookup.GetBlockWithName<IMyProjector>(projectorName, true);
                _weldEndedTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldEndedTimerName);
                _weldReadyTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldReadyTimerName);
                _welders = lookup.GetBlocksInGroup<IMyShipWelder>(welderGroupName, true);

                ExtractBlueprints();
            }

            var currentBpId = GetProjectorBlueprintId();
            var currentBlueprint = _blueprints.GetValueOrDefault(currentBpId);

            if (currentBlueprint != _previousBlueprint && currentBlueprint != null) {
                _state = "CHECK_BLUEPRINT";
               
                _projector.ProjectionOffset = currentBlueprint.ProjectionOffset;
                _projector.ProjectionRotation = currentBlueprint.ProjectionRotation;
                _projector.UpdateOffsetAndRotation();
            }

            _previousBlueprint = currentBlueprint;

            switch (argument) {
                case "CHECK":
                    if (currentBlueprint == null) break;
                    if (_state == "CHECK_BLUEPRINT") {

                    } else if (_state == "PREPARING") {

                    }

                    break;
                case "SAVE_OFFSET":
                    if (currentBlueprint == null) break;
                    currentBlueprint.ProjectionOffset = _projector.ProjectionOffset;
                    currentBlueprint.ProjectionRotation = _projector.ProjectionRotation;
                    currentBlueprint.Save(_ini);
                    Me.CustomData = _ini.ToString();

                    break;
                case "SAVE_REQUIREMENTS":
                    if (currentBlueprint == null) break;
                    currentBlueprint.SetBlocksFromAssembler(_calculatorAssembler);
                    currentBlueprint.Save(_ini);
                    Me.CustomData = _ini.ToString();

                    break;
                case "STOP":
                    Stop();
                    break;
            }
          

            _lcdBlueprintInfo.WriteText($@"Selected blueprint: {currentBlueprint?.Name ?? "Unknown"}
Current blueprint ID: {currentBpId}
State: {_state}
Projection Offset {_projector.ProjectionOffset}
Rotation {_projector.ProjectionRotation}
BP Offset {currentBlueprint?.ProjectionOffset}
BP Rotation {currentBlueprint?.ProjectionRotation}");
        }

        void Stop() {
            _pistons.ForEach(piston => piston.Enabled = false);
            _welders.ForEach(welder => welder.Enabled = false);
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

        void ExtractBlueprints() {
            _blueprints = new Dictionary<string, BlueprintInfo>();
            var sections = new List<string>();
            _ini.GetSections(sections);
            sections.ForEach(section => {
                if (section.ToLower() == "general") {
                    return;
                }

                _blueprints.Add(section, BlueprintInfo.FromIni(_ini, section, Echo));
            });
        }
    }
}
