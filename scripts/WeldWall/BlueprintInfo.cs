using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript {
    class BlueprintInfo {
        /// <summary>
        /// Dictionary containing key (definition of block), value (amount).
        /// </summary>
        public Dictionary<string, int> Blocks { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public Vector3I ProjectionOffset { get; set; }
        public Vector3I ProjectionRotation { get; set; }

        public void Save(MyIni ini) {
            ini.Set(ID, "Name", Name);
            ini.Set(ID, "ProjectionOffsetHorizontal", ProjectionOffset.X);
            ini.Set(ID, "ProjectionOffsetVertical", ProjectionOffset.Y);
            ini.Set(ID, "ProjectionOffsetForward", ProjectionOffset.Z);
            ini.Set(ID, "ProjectionPitch", ProjectionRotation.X * 90);
            ini.Set(ID, "ProjectionYaw", ProjectionRotation.Y * 90);
            ini.Set(ID, "ProjectionRoll", ProjectionRotation.Z * 90);
        }

        /// <summary>
        /// Loads an assembler's queue into the Blocks dictionary.
        /// </summary>
        /// <param name="assembler"></param>
        public void SetBlocksFromAssembler(IMyAssembler assembler) {
            var queue = new List<MyProductionItem>();
            assembler.GetQueue(queue);
            Blocks.Clear();

            queue.ForEach(item => {
                var blueprintId = item.BlueprintId.ToString();
                var amount = (int)item.Amount;
                Blocks.Add(blueprintId, amount);
            });
        }

        public static BlueprintInfo FromIni(MyIni ini, string id, Action<string> Echo) {
            var projectionOffset = new Vector3I(
                ini.Get(id, "ProjectionOffsetHorizontal").ToInt32(),
                ini.Get(id, "ProjectionOffsetVertical").ToInt32(),
                ini.Get(id, "ProjectionOffsetForward").ToInt32()
            );

            var projectionRotation = new Vector3I(
                ini.Get(id, "ProjectionPitch").ToInt32() / 90,
                ini.Get(id, "ProjectionYaw").ToInt32() / 90,
                ini.Get(id, "ProjectionRoll").ToInt32() / 90
            );

            return new BlueprintInfo() {
                Blocks = new Dictionary<string, int>(),
                ID = id,
                Name = ini.Get(id, "Name").ToString(),
                ProjectionOffset = projectionOffset,
                ProjectionRotation = projectionRotation,
            };
        }
    }
}
