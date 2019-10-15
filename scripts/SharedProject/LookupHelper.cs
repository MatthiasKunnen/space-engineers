using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedProject
{
    public class LookupHelper
    {
        public string NamePrefix { get; set; } = "";
        public IMyGridTerminalSystem GridTerminalSystem { get; set; }

        public List<T> GetBlocksInGroup<T>(string groupName) where T : class
        {
            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Name == $"{NamePrefix}{groupName}")
                {
                    var groupBlocks = new List<IMyTerminalBlock>();
                    var result = new List<T>();

                    groups[i].GetBlocks(groupBlocks);
                    for (int t = 0; t < groupBlocks.Count; t++)
                    {
                        result.Add(groupBlocks[t] as T);
                    }

                    return result;
                }
            }

            return null;
        }

        public List<T> GetBlocksOfType<T>() where T : class
        {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(blocks);

            for (var i = 0; i < blocks.Count; i++)
            {
                result.Add((T)blocks[i]);
            }

            return result;
        }

        public T GetBlockWithName<T>(string name) where T : class
        {
            return GridTerminalSystem.GetBlockWithName($"{NamePrefix}{name}") as T;
        }

        public List<T> SearchBlocksWithName<T>(string name) where T : class
        {
            var result = new List<T>();
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName($"{NamePrefix}{name}", blocks);

            for (var i = 0; i < blocks.Count; i++)
            {
                result.Add((T)blocks[i]);
            }

            return result;
        }
    }
}
