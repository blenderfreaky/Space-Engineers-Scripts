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
    public partial class Program
    {
        private struct StepReturn
        {
            public StepState CurrentState;
        }

        private enum StepState
        {
            IndexedCargoItems,
            IndexedProductionQueue,
            IndexedUnstockpiledItems,
            QueuedItems,
        }

        private IEnumerator<StepReturn> Step()
        {
            var queue = new List<MyProductionItem>();
            var items = new List<MyInventoryItem>();

            while (true)
            {
                _cargoItemCount.Clear();
                for (int i = 0; i < _containers.Length; i++)
                {
                    var inventory = _containers[i].GetInventory();
                    items.Clear();
                    inventory.GetItems(items);

                    foreach (var item in items)
                    {
                        AddToValue(_cargoItemCount, item.Type, item.Amount);
                    }
                }

                yield return new StepReturn
                {
                    CurrentState = StepState.IndexedCargoItems,
                };

                _productionQueueCount.Clear();
                for (int i = 0; i < _assemblers.Length; i++)
                {
                    queue.Clear();
                    _assemblers[i].GetQueue(queue);

                    foreach (var queueItem in queue)
                    {
                        AddToValue(_cargoItemCount, queueItem.BlueprintId, queueItem.Amount);
                    }
                }

                yield return new StepReturn
                {
                    CurrentState = StepState.IndexedProductionQueue,
                };

                _unqueuedStockpileCount.Clear();
                foreach (var item in _stockpileCount)
                {
                    _unqueuedStockpileCount.Add(item.Key, item.Value * _factor);
                }

                foreach (var item in _cargoItemCount.Concat(_productionQueueCount))
                {
                    if (_unqueuedStockpileCount.ContainsKey(item.Key))
                    {
                        _unqueuedStockpileCount[item.Key] -= item.Value;
                    }
                }

                yield return new StepReturn
                {
                    CurrentState = StepState.IndexedUnstockpiledItems,
                };

                var toProduce = _unqueuedStockpileCount
                    .Where(x => x.Value > 0)
                    .Select(x => new { Type = x.Key, Amount = MyFixedPoint.Ceiling(x.Value * (1f / _assemblers.Length)) })
                    .ToArray();

                for (int i = 0; i < _assemblers.Length; i++)
                {
                    var assembler = _assemblers[i];

                    for (int j = 0; j < toProduce.Length; j++)
                    {
                        assembler.AddQueueItem(toProduce[j].Type, toProduce[j].Amount);
                    }
                }

                yield return new StepReturn
                {
                    CurrentState = StepState.QueuedItems,
                };
            }
        }

        public void AddToValue<T>(Dictionary<T, MyFixedPoint> dictionary, T key, MyFixedPoint toAdd)
        {
            MyFixedPoint amount;
            if (dictionary.TryGetValue(key, out amount)) dictionary[key] = amount + toAdd;
            else dictionary.Add(key, toAdd);
        }
    }
}
