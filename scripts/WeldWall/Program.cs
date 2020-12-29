using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
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

        IMyProjector _projector;

        string _projectorName;

        Dictionary<string, IMyProjector> _projectors;

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
            Main("PARSE");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argumentString = "CHECK") {
            var arguments = argumentString.Split(' ');
            var argument = arguments[0] == "" ? "CHECK" : arguments[0];
            Echo((executionCounter++).ToString());

            if (argument != "CHECK" || _state != _previousState) {
                // Keep previous output on check
                _output.Clear();
            }
            _previousState = _state;

            if (argument == "PARSE") {
                Runtime.UpdateFrequency = UpdateFrequency.None;
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
                var projectors = _ini.Get("general", "Projectors").ToString("Large:WeldWallLargeProjector");
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
                _weldEndedTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldEndedTimerName);
                _weldReadyTimer = lookup.GetBlockWithName<IMyTimerBlock>(weldReadyTimerName);
                _welders = lookup.GetBlocksInGroup<IMyShipWelder>(welderGroupName, true);

                _projectors = projectors
                    .Split(' ')
                    .Select(part => part.Split(':'))
                    .ToDictionary(split => split[0], split => lookup.GetBlockWithName<IMyProjector>(split[1], true));

                var firstProjectorEntry = _projectors.FirstOrDefault();
                _projector = firstProjectorEntry.Value;

                if (_projector == null) {
                    Echo("No projector found");
                    return;
                } else {
                    _projector.Enabled = true;
                    _projectorName = firstProjectorEntry.Key;
                }

                ExtractBlueprints();
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }

            if (_projector == null) {
                Echo("No projector found");
                return;
            }

            var currentBpId = _state == "Preparing" || _state == "Welding"
                ? _previousBlueprint.ID
                : GetProjectorBlueprintId(_projector);
            var currentBlueprint = _blueprints.GetValueOrDefault(currentBpId);

            if (currentBlueprint != _previousBlueprint && currentBlueprint != null) {
                _state = "CheckBlueprint";

                _projector.ProjectionOffset = currentBlueprint.ProjectionOffset;
                _projector.ProjectionRotation = currentBlueprint.ProjectionRotation;
                _projector.UpdateOffsetAndRotation();

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
                    AssembleMissing();
                    _output.Add("Assembly of missing items in progress");
                    _state = "Preparing";

                    break;
                case "SAVE_OFFSET":
                    if (currentBlueprint == null) {
                        currentBlueprint = new BlueprintInfo() {
                            ID = currentBpId,
                            Name = currentBpId,
                        };
                        UpdateComponentList(currentBlueprint);
                        _blueprints.Add(currentBpId, currentBlueprint);
                    }

                    currentBlueprint.ProjectionOffset = _projector.ProjectionOffset;
                    currentBlueprint.ProjectionRotation = _projector.ProjectionRotation;
                    currentBlueprint.Save(_ini);
                    Me.CustomData = _ini.ToString();

                    break;
                case "SET_ACTIVE":
                    IMyProjector newActiveProjector = _projectors.GetValueOrDefault(arguments[1]);

                    if (newActiveProjector == null) {
                        _output.Add($"Projector {arguments[1]} not found");
                    } else {
                        _projectors.Values.ToList().ForEach(projector => projector.Enabled = false);
                        _projector = newActiveProjector;
                        _projectorName = arguments[1];
                        _projector.Enabled = true;
                    }

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
Active projector: {_projectorName}

{String.Join("\n", _output.ToArray())}
".Trim());
        }

        void AssembleMissing() {
            _projector.CustomData = _productionAssemblerName;
            _projector.ApplyAction("AssembleMissing");
            _projector.CustomData = "";
        }

        void Stop() {
            _pistons.ForEach(piston => piston.Enabled = false);
            _welders.ForEach(welder => welder.Enabled = false);
            _state = "CheckBlueprint";
        }

        string GetProjectorBlueprintId(IMyProjector projector) {
            var remainingBlocks = new List<string>();

            foreach (var item in projector.RemainingBlocksPerType) {
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

        void UpdateBlueprintRequirements(BlueprintInfo blueprint) {
            _calculatorAssembler.ClearQueue();
            _projector.CustomData = _calculatorAssemblerName;
            _projector.ApplyAction("AssembleAll");
            blueprint.SetBlocksFromAssembler(_calculatorAssembler);
            _calculatorAssembler.ClearQueue();
            _projector.CustomData = "";
        }

        void UpdateComponentList(BlueprintInfo blueprint) {
            UpdateBlueprintRequirements(blueprint);

            var requirements = new List<string>();

            foreach (var block in blueprint.Blocks) {
                requirements.Add($"+{BlueprintIdToLcd2Name(block.Key)}:{block.Value}");
            }

            _lcdComponentStatus.CustomData = $"MissingList * {String.Join(" ", requirements)}";
        }
    }
}
