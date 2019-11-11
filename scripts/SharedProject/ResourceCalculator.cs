using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public class ResourceCalculator {

            public static List<MyProductionItem> CalculateResources(IMyAssembler assembler, Dictionary<string, int> components, Action<string> echo) {
                assembler.ClearQueue();

                var enumerator = components.GetEnumerator();

                while(enumerator.MoveNext()) {
                    var requirement = enumerator.Current;
                    var definition = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/Drill");
                    var amount = Convert.ToDecimal(requirement.Value);
                    echo($"Definition {definition}");
                    echo($"Assembler: {assembler}");
                    echo($"amount: {amount}");

                    assembler.AddQueueItem(definition, amount);
                }

                var productionItems = new List<MyProductionItem>();

                assembler.GetQueue(productionItems);

                return productionItems;
            }
        }
    }
}
