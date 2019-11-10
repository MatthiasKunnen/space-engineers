using System.Collections.Generic;

namespace IngameScript {
    class BlueprintInfo {
        /// <summary>
        /// Dictionary containing key (definition of block), value (amount).
        /// </summary>
        public Dictionary<string, int> Blocks { get; set; }
        public string Name { get; set; }
    }
}
