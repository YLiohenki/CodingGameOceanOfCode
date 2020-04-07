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
        public void Shouldnt_init_in_island()
        {
            var output = this.ProcessGame(@"...............
...............
...............
..........xx...
..........xx.xx
.......xx.xx.xx
.......xx.xx...
...............
...............
..........xx...
..........xx...
...............
...............
.............xx
.............xx", new List<string> {
            @"2 0 6 6 3 -1 -1 -1 -1
NA
NA"}, 0);
            Assert.IsFalse(program.map.islands[program.me.x, program.me.y]);

            Assert.IsTrue(output.Any(o => o.Contains("MOVE")));
        }

        [TestMethod]
        public void Shouldnt_go_into_an_island()
        {
            var output = this.ProcessGame(@".xx..xx.xxx....
...........xxx.
.......xx..xxxx
.......xx..xxxx
...............
..........xxxx.
..........xxxx.
..........xxx..
...............
...............
..xx..xx.......
..xx..xx.......
...........xxx.
...........xxx.
...........xxx.", new List<string> {
            @"5 3 6 6 3 -1 -1 -1
NA
NA",
            @"5 2 6 6 2 -1 -1 -1
NA
MOVE S", @"5 1 6 6 1 -1 -1 -1
NA
MOVE S"}, 0);
            Assert.IsFalse(program.map.islands[program.me.x, program.me.y]);

            Assert.IsTrue(output.Any(o => o.Contains("MOVE")));
            Assert.IsTrue(program.map.islands[5, 0]);
        }

        [TestMethod]
        public void Shouldnt_charge_torpedo_in_darkness()
        {
            var output = this.ProcessGame(@"......xx.......
......xx.xxx...
.........xxx...
.xx......xxx...
.xx............
...............
...............
......xx.......
......xx.......
...............
.......xx.....x
.......xx.....x
..xx...........
..xx..........x
..............x", new List<string> {
            @"12 10 6 6 3 -1 -1 -1
NA
NA",
            @"12 9 6 6 2 -1 -1 -1
NA
MOVE E", @"12 8 6 6 1 -1 -1 -1
NA
MOVE S", @"12 7 6 6 0 -1 -1 -1
NA
MOVE S"}, 0);
            Assert.IsFalse(program.map.islands[program.me.x, program.me.y]);

            Assert.IsTrue(output.Any(o => o.Contains("MOVE")));

            Assert.IsTrue(output.Any(o => o.Contains("TORPEDO")));
        }

        [TestMethod]
        public void Shouldnt_never_attack()
        {
            var output = this.ProcessGame(@".....xx.......x
...............
...............
...............
...............
.....xx........
.xxx.xx........
.xxx...........
.......xxx..xx.
.......xxx.xxx.
.......xxx.xxx.
.......xxx.xxx.
.......xxx.xx..
.......xx......
...............", new List<string> { @"10 7 6 6 3 -1 -1 -1
NA
NA", @"10 6 6 6 2 -1 -1 -1
NA
MOVE E", @"10 5 6 6 1 -1 -1 -1
NA
MOVE S", @"10 4 6 6 0 -1 -1 -1
NA
MOVE S" }, 1570830210);
            Assert.IsTrue(program.map.enemyPossibility.total < 180 && program.map.enemyPossibility.total > 0);
        }

        [TestMethod]
        public void Shouldnt_fire_in_the_beginning()
        {
            var output = this.ProcessGame(@"...............
...............
...............
...............
...............
...............
...............
...............
...............
.....xx........
xx...xx.....xx.
xx...xx..xx.xx.
.....xx..xx.xx.
............xx.
...............", new List<string> {
            @"1 2 6 6 3 4 6 -1
NA
NA", @"1 1 6 5 2 4 6 -1
NA
SURFACE 7", @"0 10 6 6 1 -1 -1 -1
NA
MOVE S", @"1 0 6 5 1 4 6 -1
NA
MOVE E", @"2 0 6 5 0 4 6 -1
NA
MOVE E"}, 114122187);

            Assert.IsTrue(!output.Last().StartsWith("TORPEDO"));

        }

        [TestMethod]
        public void Should_fire_if_possible()
        {

            var output = this.ProcessGame(@"...............
...............
..xx...........
..xxxx.........
..xxxx.........
..xx...........
...............
.........xxx...
.........xxx...
.........xxx...
...............
.....xx........
.....xx........
...............
...............", new List<string> {@"0 12 6 6 3 -1 -1 -1
NA
NA", @"0 11 6 6 2 -1 -1 -1
NA
MOVE E", @"0 10 6 6 1 -1 -1 -1
NA
MOVE S", @"0 9 6 6 0 -1 -1 -1
NA
MOVE S", @"0 8 6 6 0 -1 -1 -1
NA
TORPEDO 3 0|MOVE W", @"0 7 6 6 0 -1 -1 -1
NA
MOVE S", @"0 6 6 6 0 -1 -1 -1
NA
MOVE S", @"0 5 6 6 0 -1 -1 -1
NA
MOVE E"
          }, 968730904);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }

        [TestMethod]
        public void Shouldnt_fire_in_the_beginning2()
        {
            var output = this.ProcessGame(@".............xx
..............x
..............x
..xx..xx.......
..xx..xx.......
......xxx......
......xxx...xx.
......xx....xx.
...............
xx.......xx....
xx.......xx..xx
.............xx
..............x
..............x
...............", new List<string> {@"4 4 6 6 3 4 6 -1
NA
NA", @"5 4 6 5 2 4 6 -1
NA
SURFACE 7", @"5 5 6 5 1 4 6 -1
NA
MOVE E", @"5 6 6 5 0 4 6 -1
NA
MOVE E", @"5 7 6 5 0 4 6 -1
NA
MOVE N"
          }, 533308942);

            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(!output.Any(o => o.StartsWith("TORPEDO")));
        }

        [TestMethod]
        public void Should_fire_eventually()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame1.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }

        [TestMethod]
        public void Should_fire_eventually2()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame2.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }

        /*[TestMethod]
        public void Should_fire_eventually3()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame3.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }

        [TestMethod]
        public void Should_fire_eventually4()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame4.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }*/

        [TestMethod]
        public void Should_fire_eventually5()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame5.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("TORPEDO")));
        }

        [TestMethod]
        public void Should_use_silence_eventually5()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame6.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(output.Any(o => o.StartsWith("SILENCE")));
        }

        [TestMethod]
        public void Shouldnt_walk_back_into_visited()
        {
            string text = System.IO.File.ReadAllText(@"./../../../FullGame7.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(!output.Last().StartsWith("MOVE W"));
        }

        [TestMethod]
        public void Shouldnt_lock_itself()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame1.txt");
            var output = ProcessInputsBulk(text);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(!output.Last().StartsWith("MOVE S"));
        }

        [TestMethod]
        public void Shouldnt_lock_itself2()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame2.txt");
            var output = ProcessInputsBulk(text);
            //Assert.IsTrue(program.map.enemyPossibility.total > 0);
            Assert.IsTrue(!output.Last().StartsWith("MOVE N"));
        }

        [TestMethod]
        public void Should_have_positive_enemy_probability()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame3.txt");
            var output = ProcessInputsBulk(text, 1);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
        }

        [TestMethod]
        public void Should_reduce_probability_on_enemy_fire()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame4.txt");
            var output = ProcessInputsBulk(text, 1);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
        }

        [TestMethod]
        public void Should_have_positive_enemy_probability2()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame5.txt");
            var output = ProcessInputsBulk(text, 1);
            Assert.IsTrue(program.map.enemyPossibility.total > 0);
        }

        [TestMethod]
        public void Should_have_positive_my_probability3()
        {
            string text = System.IO.File.ReadAllText(@"./../../../HalfGame6.txt");
            var output = ProcessInputsBulk(text, 1);
            Assert.IsTrue(program.map.mePossibility.total > 0);
        }
    }
}
