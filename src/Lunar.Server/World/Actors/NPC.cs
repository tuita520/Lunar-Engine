﻿using System;
using Lunar.Server.Net;
using Lidgren.Network;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Lunar.Core.Net;
using Lunar.Core.Utilities;
using Lunar.Core.Utilities.Data;
using Lunar.Core.Utilities.Logic;
using Lunar.Core.World;
using Lunar.Core.World.Actor;
using Lunar.Server.Content.Graphics;
using Lunar.Server.Utilities;
using Lunar.Server.World.Structure;
using Lunar.Server.Utilities.Scripting;
using Lunar.Server.World.BehaviorDefinition;

namespace Lunar.Server.World.Actors
{
    public class NPC : IActor
    {
        private Map _map;
        private Vector _frameSize;
        private List<Vector> _targetPath;
        private long _nextAttackTime;
        private Random _random;
        private long _nextMoveTime;

        public string Name { get; }

        public Sprite Sprite { get; set; }

        public float Speed { get; set; }

        public int Level { get; set; }

        public int Health { get; set; }

        public int MaximumHealth { get; set; }

        public int AggresiveRange { get; set; }

        public Vector MaxRoam { get; }

        public int AttackRange { get; }

        public Layer Layer { get; set; }

        public long UniqueID { get; }

        public Vector Position { get; private set; }

        public Rect CollisionBounds { get; set; }

        public Direction Direction { get; set; }

        public ActorBehaviorDefinition BehaviorDefinition { get; set; }

        public bool Aggrevated { get; set; }

        public ActorStates State { get; set; }

        public Map Map => _map;

        public IActor Target { get; set; }

        public bool Attackable => true;

        public event EventHandler<SubjectEventArgs> EventOccured;

        public NPC(NPCDescriptor descriptor, Map map)
        {
            Name = descriptor.Name;
            _map = map;

            this.Sprite = descriptor.Sprite;
            this.Speed = descriptor.Speed;
            this.Level = descriptor.Level;
            this.Health = descriptor.MaximumHealth;
            this.MaximumHealth = descriptor.MaximumHealth;
            this.AggresiveRange = descriptor.AggresiveRange;
            this.CollisionBounds = descriptor.CollisionBounds;
            this.BehaviorDefinition = descriptor.BehaviorDefinition;
            this.Layer = map.Layers.ElementAt(0);

            _frameSize = descriptor.FrameSize;
            AttackRange = descriptor.AttackRange;

            MaxRoam = descriptor.MaxRoam;
            
            _random = new Random();

            UniqueID = this.GetHashCode() + Environment.TickCount;
            _targetPath = new List<Vector>();

            _map.AddActor(this);

            var npcDataPacket = new Packet(PacketType.NPC_DATA);
            npcDataPacket.Message.Write(this.Pack());
            _map.SendPacket(npcDataPacket, NetDeliveryMethod.ReliableOrdered, ChannelType.UNASSIGNED);

            this.BehaviorDefinition.OnCreated.Invoke(new ScriptActionArgs(this));

            descriptor.DefinitionChanged += Descriptor_DefinitionChanged;
        }

        public void OnAttacked(IActor attacker, int damageDelt)
        {
            
        }

        private void Descriptor_DefinitionChanged(object sender, EventArgs e)
        {
            this.BehaviorDefinition = ((NPCDescriptor) sender).BehaviorDefinition;
        }

        public void Update(GameTime gameTime)
        {
            if (this.Target != null && _targetPath.Count == 0)
            {
                this.GoTo(this.Target);
            }

            this.ProcessMovement(gameTime);
            this.ProcessCombat(gameTime);

            this.BehaviorDefinition.Update.Invoke(new ScriptActionArgs(this, new object[] { gameTime }));
        }

        private void ProcessCombat(GameTime gameTime)
        {
            if (_nextAttackTime > gameTime.TotalElapsedTime || this.Target == null || !this.Aggrevated || !this.Target.Attackable) return;

            Vector posDiff = this.Position - this.Target.Position;

            posDiff = new Vector(Math.Abs(posDiff.X), Math.Abs(posDiff.Y));

            if (posDiff.X.IsWithin(0, this.AttackRange)
                && posDiff.Y.IsWithin(0, this.AttackRange))
            {

                if (this.Target is Player player)
                {
                    if (player.InLoadingScreen)
                        return;
                }

                var damageDelt = this.BehaviorDefinition?.Attack?.Invoke(new ScriptActionArgs(this, this.Target));
                this.Target.OnAttacked(this, Convert.ToInt32(damageDelt));
                

                _nextAttackTime = gameTime.TotalElapsedTime + 1000;
            }
        }

        public bool HasTarget()
        {
            return (this.Target != null);
        }

        public void GoTo(Vector targetDest)
        {
            _targetPath = this.Map.GetPathfinder(this.Layer).FindPath(this.Position, targetDest);
            _targetPath.Reverse();

            this.State = ActorStates.Moving;

            if (_targetPath.Count > 0)
            {
                this.SendMovementPacket(_targetPath);
            }
        }

        public void GoTo(IActor target)
        {
            float targetX = target.Position.X;
            float targetY = target.Position.Y;

            if (targetX < this.Position.X)
            {
                targetX -= this.CollisionBounds.Width;
            }
            else
            {
                targetX += this.CollisionBounds.Width;
            }

            if (targetY < this.Position.Y)
            {
                targetY -= this.CollisionBounds.Height;
            }
            else
            {
                targetY += this.CollisionBounds.Height;
            }

            // Don't mother moving if we're within range of the target
            if (!this.WithinRangeOf(target))
                this.GoTo(new Vector(targetX, targetY));
        }

        public bool WithinRangeOf(IActor actor)
        {
            Rect collisionBoundsRight = new Rect(this.Position.X + this.CollisionBounds.Left + Constants.TILE_SIZE, this.Position.Y + this.CollisionBounds.Top,
                this.CollisionBounds.Width, this.CollisionBounds.Height);

            Rect collisionBoundsLeft = new Rect(this.Position.X + this.CollisionBounds.Left - Constants.TILE_SIZE, this.Position.Y + this.CollisionBounds.Top,
                this.CollisionBounds.Width, this.CollisionBounds.Height);

            return (collisionBoundsRight.Intersects(actor.CollisionBounds) || collisionBoundsLeft.Intersects(actor.CollisionBounds));
        }

        private void ProcessMovement(GameTime gameTime)
        {
            if (_targetPath.Count > 0)
            {
                var targetDest = _targetPath[_targetPath.Count - 1];

                if (targetDest.X < this.Position.X)
                {
                    this.UpdateMovement(Direction.Left, gameTime);

                    if (targetDest.X >= this.Position.X)
                    {

                        this.Position = new Vector(targetDest.X, this.Position.Y);

                        // Check to make sure that our target path wasn't cleared due to a blocked path whilst moving.
                        // The reason that we have to check for this AGAIN is due to the fact that the method Move can clear the target path
                        // if it encounters a blocked tile in its way.
                        if (_targetPath.Count > 0)
                            _targetPath.RemoveAt(_targetPath.Count - 1);

                    }
                }
                else if (targetDest.X > this.Position.X)
                {
                    this.UpdateMovement(Direction.Right, gameTime);

                    if (targetDest.X <= this.Position.X)
                    {
                        this.Position = new Vector(targetDest.X, this.Position.Y);

                        // Check to make sure that our target path wasn't cleared due to a blocked path whilst moving.
                        // The reason that we have to check for this AGAIN is due to the fact that the method Move can clear the target path
                        // if it encounters a blocked tile in its way.
                        if (_targetPath.Count > 0)
                            _targetPath.RemoveAt(_targetPath.Count - 1);

                    }
                }
                else if (targetDest.Y < this.Position.Y)
                {
                    this.UpdateMovement(Direction.Up, gameTime);

                    if (targetDest.Y >= this.Position.Y)
                    {
                        this.Position = new Vector(this.Position.X, targetDest.Y);

                        // Check to make sure that our target path wasn't cleared due to a blocked path whilst moving.
                        // The reason that we have to check for this AGAIN is due to the fact that the method Move can clear the target path
                        // if it encounters a blocked tile in its way.
                        if (_targetPath.Count > 0)
                            _targetPath.RemoveAt(_targetPath.Count - 1);

                    }
                }
                else if (targetDest.Y > this.Position.Y)
                {
                    this.UpdateMovement(Direction.Down, gameTime);

                    if (targetDest.Y <= this.Position.X)
                    {
                        this.Position = new Vector(this.Position.X, targetDest.Y);

                        // Check to make sure that our target path wasn't cleared due to a blocked path whilst moving.
                        // The reason that we have to check for this AGAIN is due to the fact that the method Move can clear the target path
                        // if it encounters a blocked tile in its way.
                        if (_targetPath.Count > 0)
                            _targetPath.RemoveAt(_targetPath.Count - 1);

                    }
                }
                else
                {
                    // Check to make sure that our target path wasn't cleared due to a blocked path whilst moving.
                    // The reason that we have to check for this AGAIN is due to the fact that the method Move can clear the target path
                    // if it encounters a blocked tile in its way.
                    if (_targetPath.Count > 0)
                        _targetPath.RemoveAt(_targetPath.Count - 1);
                }

                if (_targetPath.Count == 0 && _nextMoveTime <= gameTime.TotalElapsedTime)
                {
                    _nextMoveTime = gameTime.TotalElapsedTime + Constants.NPC_REST_PERIOD;
                    this.State = ActorStates.Idle;
                    this.SendMovementPacket();
                }
            }
        }

        private void UpdateMovement(Direction direction, GameTime gameTime)
        {
            this.Direction = direction;
            
            float dX = 0, dY = 0;

            switch (this.Direction)
            {
                case Direction.Right:
                    dX = this.Speed * gameTime.UpdateTimeInMilliseconds;
                    break;

                case Direction.Left:
                    dX = -this.Speed * gameTime.UpdateTimeInMilliseconds;
                    break;

                case Direction.Up:
                    dY = -this.Speed * gameTime.UpdateTimeInMilliseconds;
                    break;

                case Direction.Down:
                    dY = this.Speed * gameTime.UpdateTimeInMilliseconds;
                    break;
            }

            this.Position = new Vector(this.Position.X + dX, this.Position.Y + dY);
        }

        public T FindTarget<T>() where T : IActor
        {
            foreach (var actor in _map.GetActors<T>())
            {
                if (!actor.Attackable)
                {
                    continue;
                }

                if (actor.Position.X >= this.Position.X - this.AggresiveRange * Constants.TILE_SIZE && actor.Position.X <= this.Position.X + this.AggresiveRange * Constants.TILE_SIZE &&
                    actor.Position.Y >= this.Position.Y - this.AggresiveRange* Constants.TILE_SIZE && actor.Position.Y <= this.Position.Y + this.AggresiveRange * Constants.TILE_SIZE)
                {
                    return actor;
                }
            }

            return default(T);
        }

        public Player FindPlayerTarget()
        {
            return this.FindTarget<Player>();
        }

        public NPC FindNPCTarget()
        {
            return this.FindTarget<NPC>();
        }
     

        private void SendMovementPacket(List<Vector> targetPath)
        {
            var packet = new Packet(PacketType.NPC_MOVING);
            packet.Message.Write(this.UniqueID);
            packet.Message.Write(true);
            packet.Message.Write((int)this.Direction);

            packet.Message.Write(targetPath.Count);
            foreach (var pos in targetPath)
            {
                packet.Message.Write(pos);
            }

            _map.SendPacket(packet, NetDeliveryMethod.ReliableOrdered, ChannelType.UNASSIGNED);
        }
        
        /// <summary>
        /// Sends a packet telling clients that the NPC is no longer moving.
        /// </summary>
        private void SendMovementPacket()
        {
            var packet = new Packet(PacketType.NPC_MOVING);
            packet.Message.Write(this.UniqueID);
            packet.Message.Write(false);
            packet.Message.Write((int)this.Direction);
            packet.Message.Write(this.Position);

            _map.SendPacket(packet, NetDeliveryMethod.ReliableOrdered, ChannelType.UNASSIGNED);
        }

        public void WarpTo(Vector position)
        {
            this.Position = position;
            var npcDataPacket = new Packet(PacketType.NPC_DATA);
            npcDataPacket.Message.Write(this.Pack());
            _map.SendPacket(npcDataPacket, NetDeliveryMethod.ReliableOrdered, ChannelType.UNASSIGNED);

            this.EventOccured?.Invoke(this, new SubjectEventArgs("moved", null));
        }

        public NetBuffer Pack()
        {
            var netBuffer = new NetBuffer();

            netBuffer.Write(this.UniqueID);
            netBuffer.Write(this.Name);
            netBuffer.Write(this.Sprite.TextureName);
            netBuffer.Write(this.Speed);
            netBuffer.Write(this.Health);
            netBuffer.Write(this.MaximumHealth);
            netBuffer.Write(this.Level);
            netBuffer.Write(this.Position.X);
            netBuffer.Write(this.Position.Y);
            netBuffer.Write(_frameSize);
            netBuffer.Write(this.CollisionBounds);
            netBuffer.Write(this.Layer.Name);

            return netBuffer;
        }
    }
}