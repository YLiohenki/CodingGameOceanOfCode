using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitTestProject
{
    [TestClass]
    public class Tests
    {
        private Program program;
        [TestInitialize]
        public void Init()
        {
            this.program = new Program();
        }

        public List<string> ProcessInputsBulk(string input, int version = 0)
        {
            var lines = input.Replace("\r\n", "\n").Split('\n');
            int seed = int.Parse(lines[0]);
            var islandMap = String.Join('\n', lines.Skip(2).Take(15));
            var divider = version > 0 ? 4 : 3;
            var cycleInput = lines.Skip(17).Select((value, index) => new { PairNum = index / divider, value })
   .GroupBy(pair => pair.PairNum)
   .Select(grp => String.Join('\n', grp.Take(3).Select(g => g.value))).ToList();
            return ProcessGame(islandMap, cycleInput, seed);
        }

        public List<string> ProcessGame(string islandMap, List<string> cycleInput, int seed)
        {
            var initInput = new List<string> { "15 15 0" };
            initInput.AddRange(islandMap.Split('\n'));
            var result = new List<string>();
            result.Add(this.program.InitGame(initInput, seed));
            cycleInput.ForEach(ci => result.Add(this.program.GameCycle(ci.Split('\n').ToList())));
            return result;
        }

        [TestMethod]
        public void Should_have_positive_my_probability3()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame1.txt");
            var output = ProcessInputsBulk(text, 1);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
        }
    }
}
