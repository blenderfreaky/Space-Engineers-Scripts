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

namespace IngameScript
{
    internal partial class Beans : MyGridProgram
    {  // This is the name of the assembler you want to control.          
        private const string _assemblerName = "Assembler";

        // This is the name of the LCD that will show what the assembler is producing.          

        private const string _outputLCD = "LCD Assembler";

        /************************************          
        Instructions:          
        This script uses the Argument box in the Programmable Block,          
        You set components to be built by putting          
        a ( in front of the component to be made then add a / followed by number to be made then end with )          
        Example:          
        (SteelPlate/10)          
        will build 10 steel plates.          

        You can put as many as you like in the argument box.          
        Example:          
        (RadioCommunication/100) (Display/4) (Medical/20)          
        This will build 100 Radios, 4 Displays, and 20 Medical Components.          

        This script is case insensitive, which means you don't have to worry about typing the component right.          
        Example:          
        (mOtOR/1) (MOTOR/1)          
        These will both work.          

        To clear the assembler queue type the word clear in the argument box. This is also case Insensitive.        

        I've also included short names for components.          
        For example, BulletProofGlass can be typed in as Bulletproof or just Bullet.          
        All the components are in the Component List Below.          


        Component List          
        ==============          
        You can type these any way shown.          
        ==============          

        BulletproofGlass / Bulletproof / Bullet            
        Computer            
        Construction            
        Detector            
        Display            
        Explosives            
        Girder            
        GravityGenerator / Gravity          
        InteriorPlate  / Interior          
        LargeTube / Large          
        Medical            
        MetalGrid / Metal          
        Motor            
        PowerCell / Power          
        RadioCommunication / Radio          
        Reactor            
        SmallTube / Small          
        SolarCell / Solar          
        SteelPlate / Steel          
        Thrust            
        Superconductor / Super         
        Canvas       
        Mag / Magazine  (Rifle Ammo)
        Box / Turret         (Turret Ammo)
        Missle

        ************************************/

        private IMyProductionBlock _assembler;
        private readonly StringBuilder _dynamicContent = new StringBuilder();
        private readonly StringBuilder _asmworking = new StringBuilder();

        public Beans()
        {
            InitialiseAssembler();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private int _runAdvancer;

        private readonly string[] _runStatus = new[]
        {
            "Program Running [|---]",
            "Program Running [-|--]",
            "Program Running [--|-]",
            "Program Running [---|]",
            "Program Running [--|-]",
            "Program Running [-|--]"
        };

        public bool InitialiseAssembler()
        {
            _assembler = GridTerminalSystem.GetBlockWithName(_assemblerName) as IMyProductionBlock;

            return _assembler != null;
        }

        private readonly IReadOnlyDictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        public void Main(string argument)
        {
            if (_assembler == null && !InitialiseAssembler())
            {
                WriteToTextPanel(_assemblerName + " not installed");
                return;
            }
            string arg = argument.ToLower();

            List<string> groups = new List<string>();

            for (int start = 0; (start = arg.IndexOf('(', start)) != -1; start++)
            {
                int end = (start >= 0) ? arg.IndexOf(')', start) : -1;
                string result = (end >= 0) ? arg.Substring(start + 1, end - start - 1) : "";
                groups.Add(result);
            }

            const string working = "Assembler is working.\n";
            const string missing = "Missing material for Queue.\n";

            if (!_assembler.IsProducing && !_assembler.IsQueueEmpty)
            {
                _asmworking.Replace(working, missing);
            }
            if (_assembler.IsProducing)
            {
                _asmworking.Clear();
                _asmworking.AppendLine(working);
            }
            if (!_assembler.IsProducing && _assembler.IsQueueEmpty)
            {
                _asmworking.Clear();
                _dynamicContent.Clear();
                _asmworking.AppendLine("Assembler is Idle.\n");
            }
            //dynamicContent.Clear();  

            Echo(_runStatus[_runAdvancer]);
            _runAdvancer = (_runAdvancer + 1) % _runStatus.Length;

            foreach (var selectedGroup in groups)
            {
                List<string> args = new List<string>(selectedGroup.Split('/'));

                if (_values.ContainsKey(args[0]))
                {
                    string item = _values[args[0]];
                    string count = args[1];

                    if (count?.Length == 0 || Convert.ToDouble(count) <= 0)
                    {
                        Echo("Item: " + item + "\nAmount: Wrong or No number input\n");
                        return;
                    }
                    MyDefinitionId objectIdToAdd = new MyDefinitionId();
                    if (MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + item, out objectIdToAdd))
                    {
                        Echo("Item: " + item + "\nAmount: " + Convert.ToDouble(count) + "\n");
                        _assembler.AddQueueItem(objectIdToAdd, Convert.ToDouble(count));
                        _dynamicContent.Append("Item: ").Append(item).Append("\nAmount: ").Append(Convert.ToDouble(count)).AppendLine("\n");
                    }
                }
                else
                {
                    Echo("Argument Missing: Check Name \n");
                }
            }
            if (arg == "clear")
            {
                _dynamicContent.Clear();
                _assembler.ClearQueue();
                Echo("Cleared Assembler Queue.");
                _dynamicContent.AppendLine("Cleared Assembler Queue.");
            }

            WriteToTextPanel(_asmworking.ToString() + _dynamicContent.ToString());
            string asmstatus = ("\nAssembler is producing: " + _assembler.IsProducing.ToString());
            //string asmstatus = assembler.NextItemId.ToString();        
            //WriteToTextPanel(asmstatus);       
        }

        public void WriteToTextPanel(string input)
        {
            List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(_outputLCD, lcds, b => b.CubeGrid == Me.CubeGrid);

            if (lcds.Count == 0)
            {
                Echo("\nNo LCD with name '" + _outputLCD + "' found.\nCheck the name.");
            }
            foreach (IMyTextPanel lcd in lcds)
            {
                lcd.WriteText(input);
            }
        }
    }
}
