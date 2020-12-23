using Sandbox.ModAPI.Ingame;
using System;
using System.Linq;
using System.Collections.Generic;

namespace IngameScript {
    public class LookupHelper {
        public string NamePrefix { get; set; } = "";
        public IMyGridTerminalSystem GridTerminalSystem { get; set; }

        /// <summary>
        /// Returns the blocks of the first group which matches the given names. Errors if no groups are found.
        /// </summary>
        public List<T> GetBlocksInFirstGroup<T>(List<string> groupNames) where T : class {
            foreach (var name in groupNames) {
                var blocks = GetBlocksInGroup<T>(name);

                if (blocks != null) {
                    return blocks;
                }
            }

            throw new Exception($"No groups named [{String.Join(", ", groupNames.Select(name => $"\"{name}\""))}] found");
        }

        public List<T> GetBlocksInGroup<T>(string groupName, bool errorIfNotExists = false) where T : class {
            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            for (int i = 0; i < groups.Count; i++) {
                if (groups[i].Name == $"{NamePrefix}{groupName}") {
                    var groupBlocks = new List<IMyTerminalBlock>();
                    var result = new List<T>();

                    groups[i].GetBlocks(groupBlocks);
                    for (int t = 0; t < groupBlocks.Count; t++) {
                        var block = groupBlocks[t] as T;

                        if (block == null) {
                            continue;
                        }

                        result.Add(block);
                    }

                    return result;
                }
            }

            if (errorIfNotExists) {
                throw new Exception($"No group named {groupName} found");
            } else {
                return null;
            }
        }

        public List<T> GetBlocksOfType<T>() where T : class {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(blocks);

            for (var i = 0; i < blocks.Count; i++) {
                result.Add((T)blocks[i]);
            }

            return result;
        }


        public T GetBlockWithName<T>(string name, bool errorIfNotExists = false) where T : class {
            var block = GridTerminalSystem.GetBlockWithName($"{NamePrefix}{name}") as T;

            if (block == null && errorIfNotExists) {
                throw new Exception($"Block with name {name} not found");
            }

            return block;
        }

        /// <summary>
        /// Returns the first block which matches the given names. Errors if no block is found.
        /// </summary>
        public T GetFirstBlockWithName<T>(List<string> blockNames) where T : class {
            foreach (var name in blockNames) {
                var block = GetBlockWithName<T>(name);

                if (block != null) {
                    return block;
                }
            }

            throw new Exception($"No block named [{String.Join(", ", blockNames.Select(name => $"\"{name}\""))}] found");
        }

        public List<T> SearchBlocksWithName<T>(string name) where T : class {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName($"{NamePrefix}{name}", blocks);

            for (var i = 0; i < blocks.Count; i++) {
                result.Add((T)blocks[i]);
            }

            return result;
        }
    }
}
