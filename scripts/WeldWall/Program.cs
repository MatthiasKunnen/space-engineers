using Sandbox.ModAPI.Ingame;
using SharedProject;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {
    partial class Program : MyGridProgram {

        readonly string _calculatorAssemblerName = "WeldWallCalculatorAssembler";
        IMyAssembler _calculatorAssembler;

        readonly string _lcdBlueprintInfoName = "WeldWallBlueprintInfoLcd";
        IMyTextPanel _lcdBlueprintInfo;

        readonly string _lcdComponentStatusName = "WeldWallComponentStatus [LCD]";
        IMyTextPanel _lcdComponentStatus;

        readonly string _pistonGroupName = "WeldWallPistons";
        List<IMyExtendedPistonBase> _pistons;

        readonly string _productionAssemblerName = "WeldWallProductionAssembler";
        IMyAssembler _productionAssembler;

        readonly string _projectorLargeName = "WeldWallProjectorLarge";
        IMyProjector _projectorLarge;

        readonly string _projectorSmallName = "WeldWallProjectorSmall";
        IMyProjector _projectorSmall;

        readonly double _retractVelocity = 4;

        readonly string _weldEndedTimerName = "WeldWallEndedTimer";
        IMyTimerBlock _weldEndedTimer;

        readonly string _weldReadyTimerName = "WeldWallReadyTimer";
        IMyTimerBlock _weldReadyTimer;

        readonly double _weldVelocity = 1;

        readonly string _welderGroupName = "WeldWallWelders";
        List<IMyShipWelder> _welders;

        readonly MyIni _ini = new MyIni();

        Dictionary<string, BlueprintInfo> _blueprints;

        BlueprintInfo _previousBlueprint = null;

        string _previousState = "CheckBlueprint";

        string _state = "CheckBlueprint";

        int executionCounter = 1;

        readonly List<string> _output = new List<string>();


        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Main("PARSE");
        }

        public void Main(string argument = "CHECK") {
            argument = argument == "" ? "CHECK" : argument;
            Echo((executionCounter++).ToString());

            if (argument != "CHECK" || _state != _previousState) {
                // Keep previous output on check
                _output.Clear();
            }
            _previousState = _state;

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
                var projectorLargeName = _ini.Get("general", "ProjectorLarge").ToString(_projectorLargeName);
                var projectorSmallName = _ini.Get("general", "ProjectorSmall").ToString(_projectorSmallName);
                var retractVelocity = _ini.Get("general", "RetractVelocity").ToDouble(_retractVelocity);
                var weldEndedTimerName = _ini.Get("general", "WeldEndedTimer").ToString(_weldEndedTimerName);
                var weldReadyTimerName = _ini.Get("general", "WeldReadyTimer").ToString(_weldReadyTimerName);
                var weldVelocity = _ini.Get("general", "WeldVelocity").ToDouble(_weldVelocity);
                var welderGroupName = _ini.Get("general", "WelderGroup").ToString(_welderGroupName);

                var lookup = new LookupHelper {
                    GridTerminalSystem = GridTerminalSystem,
                };

                _calculatorAssembler = lookup.GetBlockWithName<IMyAssembler>(calculatorAssemblerName, true);
                _lcdBlueprintInfo = lookup.GetBlockWithName<IMyTextPanel>(lcdBlueprintInfoName, true);
                _lcdComponentStatus = lookup.GetBlockWithName<IMyTextPanel>(lcdComponentStatusName, true);
                _pistons = lookup.GetBlocksInGroup<IMyExtendedPistonBase>(pistonGroupName, true);
                _productionAssembler = lookup.GetBlockWithName<IMyAssembler>(productionAssemblerName, true);
                _projectorLarge = lookup.GetBlockWithName<IMyProjector>(projectorLargeName, true);
                _projectorSmall = lookup.GetBlockWithName<IMyProjector>(projectorSmallName);
                _weldEndedTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldEndedTimerName);
                _weldReadyTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldReadyTimerName);
                _welders = lookup.GetBlocksInGroup<IMyShipWelder>(welderGroupName, true);

                ExtractBlueprints();
            }

            var projector = GetActiveProjector();
            var currentBpId = _state == "Preparing" || _state == "Welding"
                ? _previousBlueprint.ID
                : GetProjectorBlueprintId(projector);
            var currentBlueprint = _blueprints.GetValueOrDefault(currentBpId);

            if (currentBlueprint != _previousBlueprint && currentBlueprint != null) {
                _state = "CheckBlueprint";

                projector.ProjectionOffset = currentBlueprint.ProjectionOffset;
                projector.ProjectionRotation = currentBlueprint.ProjectionRotation;
                projector.UpdateOffsetAndRotation();

                UpdateComponentList(currentBlueprint);
            }

            _previousBlueprint = currentBlueprint;

            switch (argument) {
                case "CHECK":
                    if (currentBlueprint == null) break;
                    if (_state == "Preparing") {
                        if (_lcdComponentStatus.GetText().Trim() == "") {
                            if (_weldReadyTimer == null) {
                                Main("Weld");
                            } else {
                                _weldReadyTimer.ApplyAction("TriggerNow");
                            }
                        }
                    } else if (_state == "Welding") {
                        if (_pistons.TrueForAll(p => p.MaxLimit == p.CurrentPosition)) {
                            _welders.ForEach(w => w.Enabled = false);
                            _weldEndedTimer?.ApplyAction("TriggerNow");
                            _state = "CheckBlueprint";
                        }
                    }

                    break;
                case "PREPARE":
                    if (currentBlueprint == null) {
                        _output.Add("Can't prepare when no blueprint is loaded");
                        break;
                    }

                    UpdateComponentList(currentBlueprint);
                    _output.Add("Send missing items to assemblers manually");
                    _state = "Preparing";

                    break;
                case "SAVE_OFFSET":
                    if (currentBlueprint == null) {
                        _output.Add("Can't save offset when no blueprint is loaded");
                        break;
                    }
                    currentBlueprint.ProjectionOffset = projector.ProjectionOffset;
                    currentBlueprint.ProjectionRotation = projector.ProjectionRotation;
                    currentBlueprint.Save(_ini);
                    Me.CustomData = _ini.ToString();

                    break;
                case "SAVE_REQUIREMENTS":
                    if (currentBlueprint == null) {
                        _output.Add("Can't save requirements when no blueprint is loaded");
                        break;
                    }
                    currentBlueprint.SetBlocksFromAssembler(_calculatorAssembler);
                    currentBlueprint.Save(_ini);
                    Me.CustomData = _ini.ToString();

                    break;
                case "STOP":
                    Stop();
                    break;
                case "RETRACT":
                    _pistons.ForEach(p => p.Enabled = true);
                    DistributePistonVelocity(-_retractVelocity);
                    break;
                case "WELD":
                    if (currentBlueprint == null) {
                        _output.Add("Can't start welding when no blueprint is loaded");
                        break;
                    }

                    _welders.ForEach(w => w.Enabled = true);
                    _pistons.ForEach(p => p.Enabled = true);
                    DistributePistonVelocity(_weldVelocity);

                    _state = "Welding";
                    break;
            }


            _lcdBlueprintInfo.WriteText($@"
{(currentBlueprint == null ? "Unknown blueprint" : (currentBlueprint.Name ?? "Unnamed blueprint"))}
Blueprint ID: {currentBpId}
State: {_state}

{String.Join("\n", _output.ToArray())}
".Trim());
        }

        void Stop() {
            _pistons.ForEach(piston => piston.Enabled = false);
            _welders.ForEach(welder => welder.Enabled = false);
            _state = "CheckBlueprint";
        }

        string GetProjectorBlueprintId(IMyProjector projector) {
            var remainingBlocks = new List<string>();

            var enumerator = projector.RemainingBlocksPerType.GetEnumerator();
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

        /// <summary>
        /// Converts a blueprint ID (e.g. MyObjectBuilder_BlueprintDefinition/ConstructionComponent)
        /// to the name used in MMaster's LCD 2 (e.g. construction)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string BlueprintIdToLcd2Name(string name) {
            var mapper = new Dictionary<string, string>() {
                {"BulletproofGlass", "bpglass"},
                {"GravityGenerator", "gravgen"},
                {"RadioCommunication", "radio"},
                {"Thrust", "thruster"},
            };

            name = name.Substring(name.IndexOf('/') + 1); // Remove prefix MyObjectBuilder_BlueprintDefinition/
            name = name.Replace("Component", ""); // Remove component in name

            return (name ?? mapper.GetValueOrDefault(name)).ToLower();
        }

        void DistributePistonVelocity(double velocity) {
            float perPiston = (float)(velocity / _pistons.Count);
            _pistons.ForEach(p => p.Velocity = perPiston);
        }

        IMyProjector GetActiveProjector() {
            if (_projectorLarge.Enabled && _projectorSmall.Enabled) {
                Echo($"Warning: Both projectors enabled, large projector received preference");
            }

            return !_projectorLarge.Enabled && _projectorSmall.Enabled ? _projectorSmall : _projectorLarge;
        }

        void UpdateComponentList(BlueprintInfo blueprint) {
            var requirements = new List<string>();

            foreach (var block in blueprint.Blocks) {
                requirements.Add($"+{BlueprintIdToLcd2Name(block.Key)}:{block.Value}");
            }

            _lcdComponentStatus.CustomData = $"MissingList * {String.Join(" ", requirements)}";
        }
    }
}
