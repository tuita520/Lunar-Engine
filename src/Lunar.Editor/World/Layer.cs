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
using System.Linq;
using Lunar.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lunar.Core.Utilities.Logic;
using Lunar.Core.World.Structure;
using Lunar.Editor.Utilities;

namespace Lunar.Editor.World
{
    public class Layer
    {
        private LayerDescriptor _descriptor;
        private Tile[,] _tiles;
        private List<MapObject> _mapObjects;


        public LayerDescriptor Descriptor => _descriptor;

        public bool Visible { get; set; }

        public List<MapObject> MapObjects
        {
            get => _mapObjects;
        }

        public Layer(Vector2 dimensions, string name, int layerIndex)
        {
            _mapObjects = new List<MapObject>();
            _tiles = new Tile[(int)dimensions.X, (int)dimensions.Y];

            _descriptor = new LayerDescriptor(dimensions, name, layerIndex);
            _descriptor.DescriptorChanged += _descriptor_DescriptorChanged;

            this.Visible = true;
        }

        private void _descriptor_DescriptorChanged(object sender, EventArgs e)
        {
            foreach (var tile in _tiles)
            {
                if (tile != null && tile.Sprite != null)
                    tile.Sprite.LayerDepth = _descriptor.ZIndex;
            }
        }

        public Layer(LayerDescriptor layerDescriptor)
        {
            _descriptor = layerDescriptor;
            _descriptor.DescriptorChanged += _descriptor_DescriptorChanged;
            _mapObjects = new List<MapObject>();
            _tiles = new Tile[layerDescriptor.Tiles.GetLength(0), layerDescriptor.Tiles.GetLength(1)];
        }

        public void Resize(Vector2 dimensions)
        {
            _tiles = HelperFunctions.ResizeArray<Tile>(_tiles, (int)dimensions.X, (int)dimensions.Y);
        }

        public MapObject TryGetMapObject(Vector2 position)
        {
            return this.MapObjects.FirstOrDefault(x => x.Contains(position));
        }

        public void Update(GameTime gameTime)
        {
            for (int x = 0; x < _tiles.GetLength(0); x++)
            {
                for (int y = 0; y < _tiles.GetLength(1); y++)
                {
                    if (_tiles[x, y] != null)
                    {
                        _tiles[x, y].Update(gameTime);
                    }
                }
            }

            foreach (var mapObject in this.MapObjects)
                mapObject.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            if (!this.Visible)
                return;

            int left = (int)MathHelper.Clamp(camera.Position.X / 32, 0, float.MaxValue);
            int width = (camera.Bounds.Width / 32) + 2;
            int top = (int)MathHelper.Clamp(camera.Position.Y / 32, 0, float.MaxValue);
            int height = (camera.Bounds.Height / 32) + 2;

            for (int x = left; x < (left + width); x++)
            {
                for (int y = top; y < (top + height); y++)
                {
                    if (x < _tiles.GetLength(0) && y < _tiles.GetLength(1))
                    {
                        if (_tiles[x, y] != null)
                            _tiles[x, y].Draw(spriteBatch);
                    }
                }
            }

            foreach (var mapObject in this.MapObjects)
                mapObject.Draw(spriteBatch);
        }

        public Tile GetTile(int x, int y)
        {
            if (x >= _tiles.GetLength(0) || x < 0 || y >= _tiles.GetLength(1) || y < 0)
                throw new Exception("Fatal error: attempted to access an invalid tile!");

            return _tiles[x, y];
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (x >= _tiles.GetLength(0) || x < 0 || y >= _tiles.GetLength(1) || y < 0)
                throw new Exception("Fatal error: attempted to access an invalid tile!");

            _tiles[x, y] = tile;
            _descriptor.Tiles[x, y] = tile.Descriptor;
        }

     
    }
}