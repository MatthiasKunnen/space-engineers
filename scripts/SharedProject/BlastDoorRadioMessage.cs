using System;
using System.Collections.Generic;

namespace IngameScript {
    class BlastDoorRadioMessage {
        public string AccessCode { get; set; }

        public string Command { get; set; }

        public static BlastDoorRadioMessage Deserialize(string from) {
            var config = new Config(from);
            return new BlastDoorRadioMessage() {
                AccessCode = config.Get("AccessCode"),
                Command = config.Get("Command"),
            };
        }

        public string Serialize() {
            var output = new List<string>();

            output.Add($"AccessCode={AccessCode}");
            output.Add($"Command={Command}");

            return String.Join("\n", output.ToArray());
        }

        public override string ToString() {
            return this.Serialize();
        }
    }
}
