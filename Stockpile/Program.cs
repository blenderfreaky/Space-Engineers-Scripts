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
        private readonly MyIni _ini = new MyIni();
        private readonly IMyEntity[] _containers;
        private readonly IMyAssembler[] _assemblers;
        private readonly Dictionary<MyDefinitionId, MyFixedPoint> _cargoItemCount, _productionQueueCount, _unqueuedStockpileCount, _stockpileCount;
        private readonly IMyTextPanel _cargoItemPanel, _productionQueuePanel, _unqueuedStockpilePanel, _stockpilePanel;
        private readonly IEnumerator<StepReturn> _enumerator;

        private const string _sectionSettings = "StockpileSettings";
        private const string _sectionStockpile = "Stockpile";
        private const string _blueprintDef = "MyObjectBuilder_BlueprintDefinition/";

        private MyFixedPoint _factor = 1;

        private readonly IReadOnlyDictionary<string, string> _blueprintNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["computer"] = "ComputerComponent",
            ["motor"] = "MotorComponent",
            ["steelplate"] = "SteelPlate",
            ["steel"] = "SteelPlate",
            ["construction"] = "ConstructionComponent",
            ["girder"] = "GirderComponent",
            ["metalgrid"] = "MetalGrid",
            ["metal"] = "MetalGrid",
            ["interiorplate"] = "InteriorPlate",
            ["interior"] = "InteriorPlate",
            ["smalltube"] = "SmallTube",
            ["small"] = "SmallTube",
            ["largetube"] = "LargeTube",
            ["large"] = "LargeTube",
            ["display"] = "Display",
            ["bulletproofglass"] = "BulletproofGlass",
            ["bulletproof"] = "BulletproofGlass",
            ["bullet"] = "BulletproofGlass",
            ["reactor"] = "ReactorComponent",
            ["thrust"] = "ThrustComponent",
            ["gravitygenerator"] = "GravityGeneratorComponent",
            ["gravity"] = "GravityGeneratorComponent",
            ["medical"] = "MedicalComponent",
            ["radiocommunication"] = "RadioCommunicationComponent",
            ["radio"] = "RadioCommunicationComponent",
            ["detector"] = "DetectorComponent",
            ["explosives"] = "ExplosivesComponent",
            ["solarcell"] = "SolarCell",
            ["solar"] = "SolarCell",
            ["powercell"] = "PowerCell",
            ["power"] = "PowerCell",
            ["superconductor"] = "Superconductor",
            ["super"] = "Superconductor",
            ["canvas"] = "Canvas",
            ["mag"] = "NATO_5p56x45mmMagazine",
            ["magazine"] = "NATO_5p56x45mmMagazine",
            ["box"] = "NATO_25x184mmMagazine",
            ["turret"] = "NATO_25x184mmMagazine",
            ["missle"] = "Missile200mm"
        };

        private readonly string[] _runStati = new[]
        {
            "Program Running [|---]",
            "Program Running [-|--]",
            "Program Running [--|-]",
            "Program Running [---|]",
            "Program Running [--|-]",
            "Program Running [-|--]"
        };
        private int _runStatus;

        public Program()
        {
            var containers = new List<IMyCargoContainer>();
            var assemblers = new List<IMyAssembler>();

            _ini.TryParse(Me.CustomData);
            if (_ini.Get(_sectionSettings, "WriteStockpileDefaults").ToBoolean())
            {
                var names = new Dictionary<string, MyFixedPoint>
                {
                    ["SteelPlate"] = 5000,
                    ["SmallTube"] = 5000,
                    ["Construction"] = 4000,
                    ["InteriorPlate"] = 4000,
                    ["Motor"] = 3000,
                    ["LargeTube"] = 2500,
                    ["Computer"] = 2500,
                    ["MetalGrid"] = 2500,
                    ["Display"] = 2000,
                    ["BulletproofGlass"] = 2000,
                    ["PowerCell"] = 1500,
                    ["SolarCell"] = 750,
                    ["Detector"] = 750,
                    ["RadioCommunication"] = 750,
                    ["Explosives"] = 100,
                };
                _stockpileCount = names.ToDictionary(x => GetBlueprint(x.Key), x => x.Value);

                foreach (var item in names)
                {
                    _ini.Set(_sectionStockpile, item.Key, item.Value.ToString());
                }

                _ini.Delete(_sectionSettings, "WriteStockpileDefaults");
                Me.CustomData = _ini.ToString();
            }
            else
            {
                _stockpileCount = new Dictionary<MyDefinitionId, MyFixedPoint>();
                var keys = new List<MyIniKey>();
                _ini.GetKeys(_sectionStockpile, keys);

                foreach (var item in keys)
                {
                    MyDefinitionId id;
                    if (TryGetBlueprint(item.Name, out id))
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
            _productionQueueCount = new Dictionary<MyDefinitionId, MyFixedPoint>();
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

            _cargoItemPanel = _ini.ContainsKey(_sectionSettings, "CargoItemPanel")
                ? GridTerminalSystem.GetBlockWithName(_ini.Get(_sectionSettings, "CargoItemPanel").ToString()) as IMyTextPanel
                : null;
            _productionQueuePanel = _ini.ContainsKey(_sectionSettings, "ProductionQueuePanel")
                ? GridTerminalSystem.GetBlockWithName(_ini.Get(_sectionSettings, "ProductionQueuePanel").ToString()) as IMyTextPanel
                : null;
            _unqueuedStockpilePanel = _ini.ContainsKey(_sectionSettings, "UnqueuedStockpilePanel")
                ? GridTerminalSystem.GetBlockWithName(_ini.Get(_sectionSettings, "UnqueuedStockpilePanel").ToString()) as IMyTextPanel
                : null;
            _stockpilePanel = _ini.ContainsKey(_sectionSettings, "StockpilePanel")
                ? GridTerminalSystem.GetBlockWithName(_ini.Get(_sectionSettings, "StockpilePanel").ToString()) as IMyTextPanel
                : null;

            DisplayStockpile(EchoTo(_stockpilePanel));

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

        private Action<string> EchoTo(IMyTextPanel textPanel)
        {
            if (textPanel == null) return _ => { };
            textPanel.WriteText(string.Empty, false);
            return text => textPanel.WriteText(text, true);
        }

        private MyDefinitionId GetBlueprint(string shortName) => MyDefinitionId.Parse(_blueprintDef + _blueprintNames[shortName]);
        private bool TryGetBlueprint(string shortName, out MyDefinitionId id) => MyDefinitionId.TryParse(_blueprintDef + _blueprintNames[shortName], out id);

        public void Save()
        {
        }

        public void Main(string argument)
        {
            if (argument == "SetInventoryAsEmptyStockpile")
            {
                foreach (var item in _cargoItemCount
                    .Where(x => x.Key.ToString().StartsWith("MyObjectBuilder_Component/")))
                {
                    _ini.Set(_sectionStockpile, item.Key.ToString().Substring("MyObjectBuilder_Component/".Length), 0);
                    if (!_stockpileCount.ContainsKey(item.Key)) _stockpileCount.Add(item.Key, 0);
                }
                Me.CustomData = _ini.ToString();
                return;
            }

            Echo(_runStati[_runStatus = (_runStatus + 1) % _runStati.Length]);

            for (int i = 0; i < 4; i++)
            {
                if (!_enumerator.MoveNext()) throw new Exception("Terminated early");

                var result = _enumerator.Current;

                switch (result.CurrentState)
                {
                    case StepState.IndexedCargoItems:
                        DisplayCargo(EchoTo(_cargoItemPanel));
                        break;
                    case StepState.IndexedProductionQueue:
                        DisplayQueue(EchoTo(_productionQueuePanel));
                        break;
                    case StepState.IndexedUnstockpiledItems:
                        DisplayUnstockpiled(EchoTo(_unqueuedStockpilePanel));
                        break;
                    case StepState.QueuedItems:
                        DisplayQueue(EchoTo(_productionQueuePanel));
                        break;
                }
            }
        }

        private void DisplayTo(Action<string> echo, string header, IEnumerable<KeyValuePair<MyDefinitionId, MyFixedPoint>> items)
        {
            var strings = items.Select(x => new { Type = x.Key.ToString(), Amount = $"{x.Value:0.00}" }).ToArray();
            var length = strings.Length > 0 ? strings.Max(x => x.Amount.Length) : 0;

            echo(header);

            foreach (var item in strings.OrderBy(x => x.Type))
            {
                echo($"{item.Amount}{new string(' ', length-item.Amount.Length)} : {item.Type.Substring("MyObjectBuilder_".Length)}\n");
            }
        }

        private void DisplayUnstockpiled(Action<string> echo)
        {
            DisplayTo(echo, "Missing Stockpile\n", _unqueuedStockpileCount);
        }

        private void DisplayQueue(Action<string> echo)
        {
            DisplayTo(echo, "Production Queue\n", _productionQueueCount);
        }

        private void DisplayCargo(Action<string> echo)
        {
            DisplayTo(echo, "Cargo\n", _cargoItemCount);
        }

        private void DisplayStockpile(Action<string> echo)
        {
            DisplayTo(echo, $"Stockpile\nFactor: {_factor}\n", _stockpileCount);
        }
    }
}
