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
using VRage.ObjectBuilders;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private MyIni _ini = new MyIni();
        private IMyEntity[] _containers;
        private IMyAssembler[] _assemblers;
        private Dictionary<MyDefinitionId, MyFixedPoint> _cargoItemCount;
        private Dictionary<MyDefinitionId, MyFixedPoint> _productionQueue;
        private Dictionary<MyDefinitionId, MyFixedPoint> _unqueuedStockpileCount;
        private Dictionary<MyDefinitionId, MyFixedPoint> _stockpileCount;
        private IEnumerator<StepReturn> _enumerator;

        private const string _sectionSettings = "StockpileSettings";
        private const string _sectionStockpile = "Stockpile";

        public Program()
        {
            var containers = new List<IMyCargoContainer>();
            var assemblers = new List<IMyAssembler>();

            _ini.TryParse(Me.CustomData);
            if (_ini.Get(_sectionSettings, "WriteStockpileDefaults").ToBoolean())
            {
                _stockpileCount = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    // TODO
                    [MyDefinitionId.Parse("MyObjectBuilder_Ore/Iron")] = 1,
                };

                foreach (var item in _stockpileCount)
                {
                    _ini.Set(_sectionStockpile, item.Key.ToString(), item.Value.ToString());
                }

                _ini.Delete(_sectionSettings, "WriteStockpileDefaults");
            }
            else
            {
                _stockpileCount = new Dictionary<MyDefinitionId, MyFixedPoint>();
                var keys = new List<MyIniKey>();
                _ini.GetKeys(_sectionStockpile, keys);

                foreach (var item in keys)
                {
                    MyDefinitionId id;
                    if (MyDefinitionId.TryParse(item.Name, out id))
                    {
                        var amount = MyFixedPoint.DeserializeStringSafe(_ini.Get(item).ToString());
                        _stockpileCount.Add(id, amount);
                    }
                    else
                    {
                        Echo($"Unknown item type {item.Name}");
                    }
                }
            }

            _cargoItemCount = new Dictionary<MyDefinitionId, MyFixedPoint>();
            _productionQueue = new Dictionary<MyDefinitionId, MyFixedPoint>();
            _unqueuedStockpileCount = new Dictionary<MyDefinitionId, MyFixedPoint>();

            if (_ini.ContainsKey(_sectionSettings, "ContainerGroup"))
            {
                GridTerminalSystem
                    .GetBlockGroupWithName(_ini
                        .Get(_sectionSettings, "ContainerGroup")
                        .ToString())
                    .GetBlocksOfType(containers);
            }
            else
            {
                GridTerminalSystem
                    .GetBlocksOfType(containers);
            }

            GridTerminalSystem
                .GetBlockGroupWithName(_ini
                    .Get(_sectionSettings, "AssemblerGroup")
                    .ToString("Assemblers"))
                .GetBlocksOfType(assemblers);

            _containers = containers.ToArray();
            _assemblers = assemblers.ToArray();

            _enumerator = Step();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!_enumerator.MoveNext()) throw new Exception("Terminated early");

                var result = _enumerator.Current;

                switch (result.CurrentState)
                {
                    case StepState.IndexedCargoItems:
                        DisplayCargo();
                        break;
                    case StepState.IndexedProductionQueue:
                        DisplayQueue();
                        break;
                    case StepState.IndexedUnstockpiledItems:
                        DisplayUnstockpiled();
                        break;
                    case StepState.QueuedItems:
                        DisplayQueue();
                        break;
                }
            }
        }

        private void DisplayUnstockpiled()
        {
            Echo("Cargo");

            foreach (var item in _unqueuedStockpileCount)
            {
                Echo($"  {item.Key} : {item.Value}");
            }
        }

        private void DisplayQueue()
        {
            Echo("Cargo");

            foreach (var item in _productionQueue)
            {
                Echo($"  {item.Key} : {item.Value}");
            }
        }

        private void DisplayCargo()
        {
            Echo("Cargo");

            foreach(var item in _cargoItemCount)
            {
                Echo($"  {item.Key} : {item.Value}");
            }
        }
    }
}
