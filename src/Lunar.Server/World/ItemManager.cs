﻿/** Copyright 2018 John Lamontagne https://www.rpgorigin.com

	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.IO;
using Lunar.Core;
using Lunar.Core.Utilities;
using Lunar.Core.World;
using Lunar.Server.Utilities;

namespace Lunar.Server.World
{
    public class ItemManager : IService
    {
        private Dictionary<string, ItemDescriptor> _items;

        public ItemManager()
        {
            _items = new Dictionary<string, ItemDescriptor>();

            this.LoadItems();
        }

        private void LoadItems()
        {
            Console.WriteLine("Loading Items...");

            var directoryInfo = new DirectoryInfo(EngineConstants.FILEPATH_ITEMS);
            FileInfo[] files = directoryInfo.GetFiles("*.litm");

            foreach (var file in files)
            {
                ItemDescriptor itemDescriptor = ItemDescriptor.Load(EngineConstants.FILEPATH_ITEMS + file.Name);
                _items.Add(itemDescriptor.Name, itemDescriptor);
            }

            Console.WriteLine($"Loaded {files.Length} items.");
        }

        public ItemDescriptor GetItem(string itemName)
        {
            if (!_items.ContainsKey(itemName))
            {
                Logger.LogEvent($"Item {itemName} does not exist", LogTypes.ERROR, Environment.StackTrace);
                return null;
            }

            return _items[itemName];
        }

        public void Initalize()
        {
        }
    }
}
