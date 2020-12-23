using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript {

    partial class Program : MyGridProgram {

        readonly string _lcdInfoName = "Spinner Driller Info LCD"; // Overwrite: General.LcdInfo
        IMyTextPanel _lcdInfo;

        private float _drillSpeedMetersPerSecond = 0.2f;  // Overwrite: General.DrillSpeedMeterPerSecond
        readonly MyIni _ini = new MyIni();
        private bool _isFast = false;
        readonly List<string> _output = new List<string>();
        private float _pistonExtendFast = 5f;
        private float _pistonExtendSlow = 2.5f;
        private float _pistonLowerSpeed = 0.3f; // Overwrite: General.PistonLowerSpeed
        private Dictionary<string, SpinnerInfo> _spinners;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Main("PARSE");
        }

        public void Main(string argumentString) {
            _output.Clear();
            var arguments = argumentString.ToUpper().Split(';');
            _output.Add($"Mode: {(_isFast ? "Fast" : "Slow")}");

            switch (arguments[0]) {
                case "":
                    foreach (var entry in _spinners) {
                        var spinner = entry.Value;
                        var cycleStatus = spinner.CalculateCycleStatus();
                        var maxPosition = spinner.Pistons.Sum(piston => piston.HighestPosition);
                        var currentPosition = spinner.Pistons.Sum(piston => piston.CurrentPosition);

                        _output.Add($"{entry.Key}:");
                        _output.Add($"  {Math.Round(currentPosition, 2)} m/{Math.Round(maxPosition, 2)} m");
                        _output.Add($"  Change in angle: {Math.Round(cycleStatus.ChangeInAngle)}°");

                        if (!cycleStatus.CycleFinished) {
                            continue;
                        }

                        if (spinner.Pistons.All(piston => piston.CurrentPosition == piston.HighestPosition)) {
                            spinner.Rotor.Enabled = false;
                            spinner.Drills.ForEach(drill => drill.Enabled = false);
                        }

                        var extendTotalBy = _isFast ? _pistonExtendFast : _pistonExtendSlow;

                        foreach (var piston in spinner.Pistons) {
                            var extendBy = Math.Min(piston.HighestPosition - piston.CurrentPosition, extendTotalBy);
                            extendTotalBy -= extendBy;
                            piston.MaxLimit = piston.CurrentPosition + extendBy;
                        }
                    };
                    break;
                case "LOWER":
                    foreach (var spinner in _spinners.Values) {
                        spinner.Rotor.RotorLock = true;
                        spinner.Pistons.ForEach(piston => {
                            piston.MaxLimit = piston.HighestPosition;
                            piston.Velocity = _pistonLowerSpeed / spinner.Pistons.Count();
                        });
                    }

                    break;
                case "PARSE":
                    MyIniParseResult result;
                    if (!_ini.TryParse(Me.CustomData, out result)) {
                        _output.Add($"CustomData parsing error: \nLine {result.LineNo}\nError: {result.Error}");
                        return;
                    }

                    var lookup = new LookupHelper {
                        GridTerminalSystem = GridTerminalSystem,
                    };

                    _pistonLowerSpeed = (float)_ini
                        .Get("general", "PistonLowerSpeed")
                        .ToDouble(_pistonLowerSpeed);
                    _drillSpeedMetersPerSecond = (float)_ini
                        .Get("general", "DrillSpeedMeterPerSecond")
                        .ToDouble(_drillSpeedMetersPerSecond);

                    var lcdInfoName = _ini.Get("general", "LcdInfo").ToString(_lcdInfoName);
                    _lcdInfo = lookup.GetBlockWithName<IMyTextPanel>(lcdInfoName);

                    _spinners = new Dictionary<string, SpinnerInfo>();
                    var sections = new List<string>();
                    _ini.GetSections(sections);
                    sections.ForEach(section => {
                        if (section.ToLower() == "general") {
                            return;
                        }

                        var spinnerInfo = SpinnerInfo.FromIni(_ini, section, lookup);
                        _spinners.Add(section, spinnerInfo);
                    });

                    break;
                case "PERSIST_PISTON_POSITION":
                    foreach (var entry in _spinners) {
                        entry.Value.Pistons.ForEach(piston => piston.MaxLimit = piston.CurrentPosition);
                    }

                    break;

                case "RETRACT":
                    foreach (var spinner in _spinners.Values) {
                        spinner.Pistons.ForEach(piston => piston.Reverse());
                    }

                    break;
                case "START":
                    foreach (var spinner in _spinners.Values) {
                        spinner.Drills.ForEach(drill => drill.Enabled = true);
                        spinner.Pistons.ForEach(piston => {
                            piston.MaxLimit = piston.CurrentPosition;
                            piston.Enabled = true;
                            piston.Velocity = 0.5f;
                        });
                        spinner.Rotor.RotorLock = false;
                        spinner.Rotor.Enabled = true;
                        spinner.Rotor.TargetVelocityRad = spinner.GetTargetVelocityRotorRad(_isFast, _drillSpeedMetersPerSecond);
                    }

                    Runtime.UpdateFrequency = UpdateFrequency.Update100;

                    break;
                case "STOP":
                    foreach (var spinner in _spinners.Values) {
                        spinner.Rotor.Enabled = false;
                        spinner.Drills.ForEach(drill => drill.Enabled = false);
                    }

                    Runtime.UpdateFrequency = UpdateFrequency.None;

                    break;
                case "STOP_EMERGENCY":
                    foreach (var spinner in _spinners.Values) {
                        spinner.Rotor.Enabled = false;
                        spinner.Drills.ForEach(drill => drill.Enabled = false);
                        spinner.Pistons.ForEach(piston => piston.Enabled = false);
                    }

                    Runtime.UpdateFrequency = UpdateFrequency.None;

                    break;
                case "TOGGLE_SPEED":
                    _isFast = !_isFast;

                    foreach (var spinner in _spinners.Values) {
                        spinner.Drills.ForEach(drill => drill.Enabled = !_isFast);
                        spinner.Rotor.TargetVelocityRad = spinner.GetTargetVelocityRotorRad(_isFast, _drillSpeedMetersPerSecond);
                    }

                    break;
            }

            _output.ForEach(line => Echo(line));
            _lcdInfo?.WriteText(String.Join("\n", _output.ToArray()));
        }
    }
}
